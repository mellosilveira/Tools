using MelloSilveiraTools.Core.Pipelines.Models;
using MelloSilveiraTools.Core.Pipelines.Telemetry;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks.Dataflow;

namespace MelloSilveiraTools.Core.Pipelines.Dataflow;

/// <summary>
/// A strongly-typed fluent builder for orchestrating TPL Dataflow topologies.
/// </summary>
/// <remarks>
/// Technical Decision: Encapsulates the complexity of block instantiation, telemetry wrapping, and graph linkage. 
/// It dynamically alters the internal topology depending on whether a Dead-Letter Queue (DLQ) is enabled, seamlessly 
/// injecting bifurcated routing nodes (via <see cref="SafeResult{TIn, TOut}"/>) without exposing this graph complexity to the consumer.
/// Limitation: The builder assumes a linear or singular-convergence topology. While it supports branching (forking), 
/// those branches must reconcile back to a single primary data type to proceed to the next step.
/// </remarks>
internal class DataflowPipelineBuilder<THead, TTail>(
    ILogger logger,
    ITargetBlock<THead> headBlock,
    ISourceBlock<TTail> tailBlock,
    ITargetBlock<FailedPayload>? deadLetterQueueBlock,
    RetryOptions? retryOptions,
    CancellationToken pipelineCancellationToken,
    List<Task>? branchCompletionTasks = null)
    : IDataflowPipelineBuilder<THead, TTail>
{
    private readonly bool _deadLetterQueueEnabled = deadLetterQueueBlock is not null;
    private readonly List<Task> _branchCompletionTasks = branchCompletionTasks ?? [];
    private const string DeadLetterQueueTelemetryName = "Pipeline.DeadLetterQueue";
    private const string DataMappingTelemetryName = "Pipeline.DataMapping";

    /// <inheritdoc/>
    /// <remarks>
    /// Technical Decision: Creates a new builder instance holding the DLQ reference, propagating it downstream.
    /// Limitation: Replaces any previously configured DLQ for subsequent steps in the builder chain.
    /// </remarks>
    public IDataflowPipelineBuilder<THead, TTail> WithDeadLetterQueue(ITargetBlock<FailedPayload> deadLetterQueueSink)
        => new DataflowPipelineBuilder<THead, TTail>(logger, headBlock, tailBlock, deadLetterQueueSink, retryOptions, pipelineCancellationToken, _branchCompletionTasks);

    /// <inheritdoc/>
    /// <remarks>
    /// Technical Decision: Automatically provisions an <see cref="ActionBlock{T}"/> to wrap the synchronous error handler, allowing it to hook directly into the TPL Dataflow graph.
    /// Limitation: Blocking I/O inside the synchronous action will stall the underlying ThreadPool thread assigned to this terminal block.
    /// </remarks>
    public IDataflowPipelineBuilder<THead, TTail> WithDeadLetterQueue(Action<FailedPayload> errorHandler, PipelineStepOptions options = default)
    {
        ActionBlock<FailedPayload> actionBlock = new(
            TelemetryExtensions.HandleExecution(logger, DeadLetterQueueTelemetryName, errorHandler, pipelineCancellationToken),
            options.ToDataflowOptions(pipelineCancellationToken));

        return WithDeadLetterQueue(actionBlock);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Technical Decision: Provides optimal DLQ routing by executing error-handling logic asynchronously (e.g., writing to a remote queue) while honoring retry constraints if configured.
    /// </remarks>
    public IDataflowPipelineBuilder<THead, TTail> WithDeadLetterQueue(Func<FailedPayload, CancellationToken, Task> errorHandler, PipelineStepOptions options = default)
    {
        ActionBlock<FailedPayload> actionBlock = new(
            TelemetryExtensions.HandleExecution(logger, DeadLetterQueueTelemetryName, errorHandler, retryOptions, pipelineCancellationToken),
            options.ToDataflowOptions(pipelineCancellationToken));

        return WithDeadLetterQueue(actionBlock);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Technical Decision: Serves as a fallback for pipelines that require fault tolerance (to prevent cascading block failures) but do not require complex error recovery.
    /// Limitation: Failed payloads are strictly serialized to logs and discarded from memory.
    /// </remarks>
    public IDataflowPipelineBuilder<THead, TTail> WithLoggingErrors(PipelineStepOptions options = default)
    {
        ActionBlock<FailedPayload> actionBlock = new(
            failedPayload => logger.LogError("Failed to execute step. Failed payload: {@FailedPayload}", failedPayload),
            options.ToDataflowOptions(pipelineCancellationToken));

        return WithDeadLetterQueue(actionBlock);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Technical Decision: Dynamically shifts the block type to <c>TransformBlock&lt;TTail, SafeResult&lt;TTail, TNextOut&gt;&gt;</c> if a DLQ is active. 
    /// This allows the pipeline to catch the exception internally and route it to the DLQ rather than faulting the current block.
    /// Limitation: Purely synchronous execution. Retry policies are physically bypassed here to prevent thread exhaustion.
    /// </remarks>
    public IDataflowPipelineBuilder<THead, TNextOut> AddDataMapping<TNextOut>(Func<TTail, TNextOut> mapFunc, PipelineStepOptions options = default)
    {
        ExecutionDataflowBlockOptions dataFlowOptions = options.ToDataflowOptions(pipelineCancellationToken);

        if (_deadLetterQueueEnabled)
        {
            TransformBlock<TTail, SafeResult<TTail, TNextOut>> safeBlock = new(TelemetryExtensions.HandleSafeExecution(logger, DataMappingTelemetryName, mapFunc, pipelineCancellationToken), dataFlowOptions);
            return AddSafeStep(safeBlock, dataFlowOptions);
        }

        TransformBlock<TTail, TNextOut> nextBlock = new(TelemetryExtensions.HandleExecution(logger, DataMappingTelemetryName, mapFunc, pipelineCancellationToken), dataFlowOptions);
        return LinkAndContinue(nextBlock);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Technical Decision: Leverages asynchronous execution, enabling both OpenTelemetry span lifecycle tracking and exponential backoff loops on transient faults.
    /// </remarks>
    public IDataflowPipelineBuilder<THead, TNextOut> AddDataMapping<TNextOut>(Func<TTail, CancellationToken, Task<TNextOut>> mapFunc, PipelineStepOptions options = default)
    {
        ExecutionDataflowBlockOptions dataFlowOptions = options.ToDataflowOptions(pipelineCancellationToken);

        if (_deadLetterQueueEnabled)
        {
            TransformBlock<TTail, SafeResult<TTail, TNextOut>> safeBlock = new(TelemetryExtensions.HandleSafeExecution(logger, DataMappingTelemetryName, mapFunc, retryOptions, pipelineCancellationToken), dataFlowOptions);
            return AddSafeStep(safeBlock, dataFlowOptions);
        }

        TransformBlock<TTail, TNextOut> nextBlock = new(TelemetryExtensions.HandleExecution(logger, DataMappingTelemetryName, mapFunc, retryOptions, pipelineCancellationToken)!, dataFlowOptions);
        return LinkAndContinue(nextBlock);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Technical Decision: Requires explicit naming for granular telemetry tracking. Like mapping, automatically supports topology bifurcation for DLQ routing.
    /// Limitation: Strict 1:1 input/output cardinality.
    /// </remarks>
    public IDataflowPipelineBuilder<THead, TNextOut> AddStep<TNextOut>(string stepName, Func<TTail, CancellationToken, Task<TNextOut>> stepFunc, PipelineStepOptions options = default)
    {
        ExecutionDataflowBlockOptions dataFlowOptions = options.ToDataflowOptions(pipelineCancellationToken);

        if (_deadLetterQueueEnabled)
        {
            TransformBlock<TTail, SafeResult<TTail, TNextOut>> safeBlock = new(TelemetryExtensions.HandleSafeExecution(logger, GetTelemetryName(stepName), stepFunc, retryOptions, pipelineCancellationToken), dataFlowOptions);
            return AddSafeStep(safeBlock, dataFlowOptions);
        }

        TransformBlock<TTail, TNextOut> nextBlock = new(TelemetryExtensions.HandleExecution(logger, GetTelemetryName(stepName), stepFunc, retryOptions, pipelineCancellationToken)!, dataFlowOptions);
        return LinkAndContinue(nextBlock);
    }

    /// <inheritdoc/>
    public IDataflowPipelineBuilder<THead, TNextOut> AddStep<TNextOut>(string stepName, Func<TTail, TNextOut> stepFunc, PipelineStepOptions options = default)
    {
        ExecutionDataflowBlockOptions dataFlowOptions = options.ToDataflowOptions(pipelineCancellationToken);

        if (_deadLetterQueueEnabled)
        {
            TransformBlock<TTail, SafeResult<TTail, TNextOut>> safeBlock = new(TelemetryExtensions.HandleSafeExecution(logger, GetTelemetryName(stepName), stepFunc, pipelineCancellationToken), dataFlowOptions);
            return AddSafeStep(safeBlock, dataFlowOptions);
        }

        TransformBlock<TTail, TNextOut> nextBlock = new(TelemetryExtensions.HandleExecution(logger, GetTelemetryName(stepName), stepFunc, pipelineCancellationToken), dataFlowOptions);
        return LinkAndContinue(nextBlock);
    }

    /// <inheritdoc/>
    public IDataflowPipelineBuilder<THead, TNextOut> AddStep<TNextOut>(string stepName, Func<TTail, CancellationToken, IAsyncEnumerable<TNextOut>> stepFunc, PipelineStepOptions options = default)
    {
        ExecutionDataflowBlockOptions dataFlowOptions = options.ToDataflowOptions(pipelineCancellationToken);
        Func<TTail, IAsyncEnumerable<TNextOut>> telemetryStreamFunc = TelemetryExtensions.HandleExecution(logger, GetTelemetryName(stepName), stepFunc, pipelineCancellationToken);

        BufferBlock<TNextOut> source = new(dataFlowOptions);

        ActionBlock<TTail> target = new(async item =>
        {
            try
            {
                await foreach (TNextOut outItem in telemetryStreamFunc(item).WithCancellation(pipelineCancellationToken).ConfigureAwait(false))
                {
                    await source.SendAsync(outItem, pipelineCancellationToken).ConfigureAwait(false);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException && _deadLetterQueueEnabled)
            {
                await deadLetterQueueBlock!.SendAsync(new FailedPayload(GetTelemetryName(stepName), item, ex), pipelineCancellationToken).ConfigureAwait(false);
            }
        }, dataFlowOptions);

        target.Completion.ContinueWith(t =>
        {
            if (t.IsFaulted && t.Exception is not null)
                ((IDataflowBlock)source).Fault(t.Exception);
            else
                source.Complete();
        }, TaskContinuationOptions.ExecuteSynchronously);

        IPropagatorBlock<TTail, TNextOut> streamBlock = DataflowBlock.Encapsulate(target, source);
        return LinkAndContinue(streamBlock);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Technical Decision: Evaluates the <paramref name="fallbackCondition"/> against the input before executing the primary logic, saving I/O if the bypass condition is met.
    /// Limitation: The output payload type of the fallback must strictly match the primary branch to maintain downstream type invariance.
    /// </remarks>
    public IDataflowPipelineBuilder<THead, TNextOut> AddForkingStep<TNextOut>(
        string stepName,
        string fallbackStepName,
        Func<TTail, CancellationToken, Task<TNextOut>> stepFunc,
        Func<TTail, bool> fallbackCondition,
        Func<TTail, CancellationToken, Task<TNextOut>> fallbackStep,
        PipelineStepOptions options = default)
    {
        ExecutionDataflowBlockOptions dataFlowOptions = options.ToDataflowOptions(pipelineCancellationToken);

        if (_deadLetterQueueEnabled)
        {
            TransformBlock<TTail, SafeResult<TTail, TNextOut>> safeBlock = new(
                TelemetryExtensions.HandleSafeExecution(logger, GetTelemetryName(stepName), GetTelemetryName(fallbackStepName), stepFunc, fallbackCondition, fallbackStep, retryOptions, pipelineCancellationToken),
                dataFlowOptions);
            return AddSafeStep(safeBlock, dataFlowOptions);
        }

        TransformBlock<TTail, TNextOut> nextBlock = new(TelemetryExtensions.HandleExecution(logger, GetTelemetryName(stepName), GetTelemetryName(fallbackStepName), stepFunc, fallbackCondition, fallbackStep, retryOptions, pipelineCancellationToken)!, dataFlowOptions);
        return LinkAndContinue(nextBlock);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Technical Decision: Evaluates the condition post-execution. If the pipeline has a DLQ enabled, the condition is evaluated dynamically inside the <c>SafeResult</c> wrapper to ensure it only applies to successful executions.
    /// Limitation: Does not rollback side-effects incurred by the primary step if the fallback evaluates to true.
    /// </remarks>
    public IDataflowPipelineBuilder<THead, TNextOut> AddForkingStep<TNextOut>(
        string stepName,
        string fallbackStepName,
        Func<TTail, CancellationToken, Task<TNextOut>> stepFunc,
        Func<TNextOut, bool> fallbackCondition,
        Func<TTail, CancellationToken, Task<TNextOut>> fallbackStep,
        PipelineStepOptions options = default)
    {
        ExecutionDataflowBlockOptions dataFlowOptions = options.ToDataflowOptions(pipelineCancellationToken);

        if (_deadLetterQueueEnabled)
        {
            TransformBlock<TTail, SafeResult<TTail, TNextOut>> safeBlock = new(
                TelemetryExtensions.HandleSafeExecution(
                    logger,
                    GetTelemetryName(stepName),
                    GetTelemetryName(fallbackStepName),
                    stepFunc,
                    safeResult => safeResult.Success && safeResult.Output is not null && fallbackCondition(safeResult.Output),
                    fallbackStep,
                    retryOptions,
                    pipelineCancellationToken),
                dataFlowOptions);
            return AddSafeStep(safeBlock, dataFlowOptions);
        }

        TransformBlock<TTail, TNextOut> nextBlock = new(TelemetryExtensions.HandleExecution(logger, GetTelemetryName(stepName), GetTelemetryName(fallbackStepName), stepFunc, fallbackCondition, fallbackStep, retryOptions, pipelineCancellationToken), dataFlowOptions);
        return LinkAndContinue(nextBlock);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Technical Decision: Configures a <see cref="BatchBlock{T}"/> using <see cref="GroupingDataflowBlockOptions"/> to respect bounded capacities.
    /// Limitation: Messages remain buffered until the exact <paramref name="batchSize"/> is met. A manual <see cref="IDataflowPipeline{TIn}.Complete"/> call is required to force flush a partial batch at the end of the stream.
    /// </remarks>
    public IDataflowPipelineBuilder<THead, TTail[]> AddBatchStep(int batchSize, PipelineStepOptions options = default)
    {
        BatchBlock<TTail> nextBlock = new(batchSize, new GroupingDataflowBlockOptions { BoundedCapacity = options.MaxBufferSize, CancellationToken = pipelineCancellationToken });
        return LinkAndContinue(nextBlock);
    }

    /// <inheritdoc/>
    public IDataflowPipelineBuilder<THead, TTail> AddFilterStep(Predicate<TTail> predicate, PipelineStepOptions options = default)
    {
        // Technical Decision: A TransformManyBlock yielding 0 or 1 item is the safest TPL-native way to drop messages.
        // It consumes the message entirely, preventing it from getting permanently stuck in the upstream source buffer.
        TransformManyBlock<TTail, TTail> filterBlock = new(item => predicate(item) ? [item] : Array.Empty<TTail>(), options.ToDataflowOptions(pipelineCancellationToken));
        return LinkAndContinue(filterBlock);
    }

    /// <inheritdoc/>
    public IDataflowPipelineBuilder<THead, TTail[]> AddGroupWhileStep(Func<TTail, TTail, bool> condition, PipelineStepOptions options = default)
    {
        ExecutionDataflowBlockOptions dataFlowOptions = options.ToDataflowOptions(pipelineCancellationToken);

        // Must be strictly sequential to evaluate the condition against the previous item accurately.
        dataFlowOptions.MaxDegreeOfParallelism = 1;

        List<TTail> buffer = [];
        BufferBlock<TTail[]> source = new(dataFlowOptions);

        ActionBlock<TTail> target = new(async item =>
        {
            if (buffer.Count > 0 && !condition(buffer[^1], item))
            {
                await source.SendAsync([.. buffer]).ConfigureAwait(false);
                buffer.Clear();
            }
            buffer.Add(item);
        }, dataFlowOptions);

        // Technical Decision: When the pipeline invokes Complete()[cite: 2], we must force-flush the final 
        // partial batch trapped in the buffer before propagating the completion state downstream.
        target.Completion.ContinueWith(async t =>
        {
            if (buffer.Count > 0)
            {
                await source.SendAsync([.. buffer]).ConfigureAwait(false);
                buffer.Clear();
            }

            if (t.IsFaulted && t.Exception is not null)
                ((IDataflowBlock)source).Fault(t.Exception);
            else
                source.Complete();

        }, TaskContinuationOptions.ExecuteSynchronously);

        // Encapsulate combines the receiving ActionBlock and emitting BufferBlock into a single logical Propagator node.
        IPropagatorBlock<TTail, TTail[]> groupBlock = DataflowBlock.Encapsulate(target, source);
        return LinkAndContinue(groupBlock);
    }

    /// <inheritdoc/>
    public IDataflowPipelineBuilder<THead, TTail> AddBroadcastBlock(ITargetBlock<TTail> branchTarget, Func<TTail, TTail>? cloneFunc = null, PipelineStepOptions options = default)
    {
        return AddBroadcastTarget(branchTarget, branchTarget.Completion, cloneFunc, options.ToDataflowOptions(pipelineCancellationToken));
    }

    /// <inheritdoc/>
    public IDataflowPipelineBuilder<THead, TTail> AddBroadcastBlock(IEnumerable<ITargetBlock<TTail>> branchTargets, Func<TTail, TTail>? cloneFunc = null, PipelineStepOptions options = default)
    {
        ExecutionDataflowBlockOptions dataFlowOptions = options.ToDataflowOptions(pipelineCancellationToken);
        return AddBroadcastTargets(branchTargets.Select(target => (target, target.Completion)), cloneFunc, dataFlowOptions);
    }

    /// <inheritdoc/>
    public IDataflowPipelineBuilder<THead, TTail> AddBroadcastBlock(
        string stepName,
        Func<TTail, CancellationToken, Task> branchAction,
        Func<TTail, TTail>? cloneFunc = null,
        PipelineStepOptions options = default)
    {
        ExecutionDataflowBlockOptions dataFlowOptions = options.ToDataflowOptions(pipelineCancellationToken);
        (ITargetBlock<TTail> target, Task completion) = CreateConsumer(stepName, branchAction, dataFlowOptions);
        return AddBroadcastTarget(target, completion, cloneFunc, dataFlowOptions);
    }

    /// <inheritdoc/>
    public IDataflowPipelineBuilder<THead, TTail> AddBroadcastBlock(
        string stepName,
        Action<TTail> branchAction,
        Func<TTail, TTail>? cloneFunc = null,
        PipelineStepOptions options = default)
    {
        ExecutionDataflowBlockOptions dataFlowOptions = options.ToDataflowOptions(pipelineCancellationToken);
        (ITargetBlock<TTail> target, Task completion) = CreateConsumer(stepName, branchAction, dataFlowOptions);
        return AddBroadcastTarget(target, completion, cloneFunc, dataFlowOptions);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Technical Decision: Caps the underlying graph by resolving to a final <see cref="ActionBlock{T}"/>, returning a sealed interface that prevents further linkage.
    /// </remarks>
    public IDataflowPipeline<THead> BuildTerminal(string stepName, Action<TTail> terminalAction, PipelineStepOptions options = default)
    {
        (ITargetBlock<TTail> consumerBlock, Task completionTask) = CreateConsumer(stepName, terminalAction, options.ToDataflowOptions(pipelineCancellationToken));
        return BuildTerminalFromConsumer(consumerBlock, completionTask);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Technical Decision: Caps the underlying graph by resolving to a final asynchronous <see cref="ActionBlock{T}"/>, returning a sealed interface that prevents further linkage.
    /// </remarks>
    public IDataflowPipeline<THead> BuildTerminal(string stepName, Func<TTail, CancellationToken, Task> terminalAction, PipelineStepOptions options = default)
    {
        (ITargetBlock<TTail> consumerBlock, Task completionTask) = CreateConsumer(stepName, terminalAction, options.ToDataflowOptions(pipelineCancellationToken));
        return BuildTerminalFromConsumer(consumerBlock, completionTask);
    }

    /// <summary>
    /// Constructs a bifurcated routing topology to handle <see cref="SafeResult{TIn, TOut}"/> payloads.
    /// </summary>
    /// <remarks>
    /// Technical Decision: Automatically splits a single logical pipeline step into three distinct TPL blocks: 
    /// the primary execution wrapper, a success router, and an asynchronous failure router linked directly to the DLQ. 
    /// This keeps the consumer's fluent configuration clean while satisfying complex TPL routing constraints.
    /// </remarks>
    private DataflowPipelineBuilder<THead, TNextOut> AddSafeStep<TNextOut>(TransformBlock<TTail, SafeResult<TTail, TNextOut>> safeBlock, ExecutionDataflowBlockOptions dataFlowOptions)
    {
        tailBlock.LinkTo(safeBlock);

        TransformBlock<SafeResult<TTail, TNextOut>, TNextOut> successBlock = new(safeResult => safeResult.Output!, dataFlowOptions);
        safeBlock.LinkTo(successBlock, safeResult => safeResult.Success);

        ActionBlock<SafeResult<TTail, TNextOut>> failedBlock = new(async safeResult => await deadLetterQueueBlock!.SendAsync(safeResult.FailedPayload, pipelineCancellationToken).ConfigureAwait(false), dataFlowOptions);
        safeBlock.LinkTo(failedBlock, safeResult => !safeResult.Success);

        return new DataflowPipelineBuilder<THead, TNextOut>(logger, headBlock, successBlock, deadLetterQueueBlock, retryOptions, pipelineCancellationToken, _branchCompletionTasks);
    }

    private (ITargetBlock<TTail> TargetBlock, Task CompletionTask) CreateConsumer(
        string stepName,
        Action<TTail> action,
        ExecutionDataflowBlockOptions dataFlowOptions)
    {
        if (_deadLetterQueueEnabled)
        {
            TransformBlock<TTail, SafeResult<TTail>> safeBlock = new(
                TelemetryExtensions.HandleSafeExecution(logger, GetTelemetryName(stepName), action, pipelineCancellationToken),
                dataFlowOptions);
            ActionBlock<SafeResult<TTail>> failedBlock = CreateDeadLetterSink(dataFlowOptions);
            safeBlock.LinkTo(failedBlock, safeResult => !safeResult.Success);
            return (safeBlock, Task.WhenAll(safeBlock.Completion, failedBlock.Completion));
        }

        ActionBlock<TTail> terminalBlock = new(
            TelemetryExtensions.HandleExecution(logger, GetTelemetryName(stepName), action, pipelineCancellationToken),
            dataFlowOptions);
        return (terminalBlock, terminalBlock.Completion);
    }

    private (ITargetBlock<TTail> TargetBlock, Task CompletionTask) CreateConsumer(
        string stepName,
        Func<TTail, CancellationToken, Task> action,
        ExecutionDataflowBlockOptions dataFlowOptions)
    {
        if (_deadLetterQueueEnabled)
        {
            TransformBlock<TTail, SafeResult<TTail>> safeBlock = new(
                TelemetryExtensions.HandleSafeExecution(logger, GetTelemetryName(stepName), action, retryOptions, pipelineCancellationToken),
                dataFlowOptions);
            ActionBlock<SafeResult<TTail>> failedBlock = CreateDeadLetterSink(dataFlowOptions);
            safeBlock.LinkTo(failedBlock, safeResult => !safeResult.Success);
            return (safeBlock, Task.WhenAll(safeBlock.Completion, failedBlock.Completion));
        }

        ActionBlock<TTail> terminalBlock = new(
            TelemetryExtensions.HandleExecution(logger, GetTelemetryName(stepName), action, retryOptions, pipelineCancellationToken)!,
            dataFlowOptions);
        return (terminalBlock, terminalBlock.Completion);
    }

    private ActionBlock<SafeResult<TTail>> CreateDeadLetterSink(ExecutionDataflowBlockOptions dataFlowOptions)
    {
        return new(async safeResult => await deadLetterQueueBlock!.SendAsync(safeResult.FailedPayload, pipelineCancellationToken).ConfigureAwait(false), dataFlowOptions);
    }

    private DataflowPipelineBuilder<THead, TTail> AddBroadcastTarget(
        ITargetBlock<TTail> branchTarget,
        Task branchCompletion,
        Func<TTail, TTail>? cloneFunc,
        ExecutionDataflowBlockOptions dataFlowOptions)
    {
        return AddBroadcastTargets([(branchTarget, branchCompletion)], cloneFunc, dataFlowOptions);
    }

    private DataflowPipelineBuilder<THead, TTail> AddBroadcastTargets(
        IEnumerable<(ITargetBlock<TTail> Target, Task Completion)> targets,
        Func<TTail, TTail>? cloneFunc,
        ExecutionDataflowBlockOptions dataFlowOptions)
    {
        BroadcastBlock<TTail> broadcastBlock = new(cloneFunc, dataFlowOptions);
        tailBlock.LinkTo(broadcastBlock);

        List<Task> updatedTasks = [.. _branchCompletionTasks];
        foreach ((ITargetBlock<TTail> target, Task completion) in targets)
        {
            broadcastBlock.LinkTo(target);
            updatedTasks.Add(completion);
        }

        return new DataflowPipelineBuilder<THead, TTail>(logger, headBlock, broadcastBlock, deadLetterQueueBlock, retryOptions, pipelineCancellationToken, updatedTasks);
    }

    private IDataflowPipeline<THead> BuildTerminalFromConsumer(ITargetBlock<TTail> consumerBlock, Task completionTask)
    {
        tailBlock.LinkTo(consumerBlock);
        return new DataflowPipeline<THead>(
            headBlock,
            _branchCompletionTasks.Count > 0 ? Task.WhenAll([completionTask, .. _branchCompletionTasks]) : completionTask,
            logger);
    }

    private DataflowPipelineBuilder<THead, TNextOut> LinkAndContinue<TNextOut>(IPropagatorBlock<TTail, TNextOut> nextBlock)
    {
        tailBlock.LinkTo(nextBlock);
        return new DataflowPipelineBuilder<THead, TNextOut>(logger, headBlock, nextBlock, deadLetterQueueBlock, retryOptions, pipelineCancellationToken, _branchCompletionTasks);
    }

    private static string GetTelemetryName(string stepName) => $"Pipeline.Step.{stepName}";
}

/// <summary>
/// The concrete execution engine encapsulating the TPL source/target block linkages.
/// </summary>
/// <remarks>
/// Technical Decision: Restricted via file-scoped access and sealed to enable runtime devirtualization optimizations. 
/// Exposes only the absolute minimum required operational surface (Send, Complete, and Await Completion).
/// </remarks>
file sealed class DataflowPipeline<TIn>(ITargetBlock<TIn> headBlock, Task completionTask, ILogger? logger) : IDataflowPipeline<TIn>
{
    /// <inheritdoc/>
    public Task<bool> SendAsync(TIn item, CancellationToken cancellationToken = default) => headBlock.SendAsync(item, cancellationToken);

    /// <inheritdoc/>
    public void Complete()
    {
        logger?.LogInformation("Pipeline completion invoked. Draining buffered messages and propagating completion state.");
        headBlock.Complete();
    }

    /// <inheritdoc/>
    public Task Completion => completionTask;

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        Complete();
        await Completion.ConfigureAwait(false);
    }
}