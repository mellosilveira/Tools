using MelloSilveiraTools.Core.Pipelines.Models;
using MelloSilveiraTools.Core.Pipelines.Steps;
using MelloSilveiraTools.Core.Pipelines.Telemetry;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Threading.Tasks.Dataflow;

namespace MelloSilveiraTools.Core.Pipelines.Dataflow;

// TODO: ADICIONAR CIRCUIT BREAKER

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
    List<Task>? branchCompletionTasks = null,
    List<IPipelineStep>? steps = null)
    : IDataflowPipelineBuilder<THead, TTail>
{
    private const string DeadLetterQueueTelemetryName = "Pipeline.DeadLetterQueue";
    private const string DataMappingTelemetryName = "Pipeline.DataMapping";
    private readonly bool _deadLetterQueueEnabled = deadLetterQueueBlock is not null;
    private readonly List<Task> _branchCompletionTasks = branchCompletionTasks ?? [];
    private readonly List<IPipelineStep> _steps = steps ?? [];

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
    public IDataflowPipelineBuilder<THead, TTail> AddFilterStep(Predicate<TTail> predicate, PipelineStepOptions options = default)
    {
        // Creates an output buffer to hold only the items that pass the filter
        BufferBlock<TTail> filterBlock = new(options.ToDataflowOptions(pipelineCancellationToken));

        // 1. Native Routing: Links the previous block to the filter buffer using the predicate.
        // Only items that evaluate to 'true' are forwarded. This guarantees zero memory allocation 
        // per item (no arrays or IEnumerables created).
        tailBlock.LinkTo(filterBlock, predicate);

        // 2. Discard Bin (Mandatory): Items that evaluate to 'false' must be routed to a NullTarget.
        // If this is missing, rejected items will remain stuck in the tailBlock's output buffer,
        // eventually filling up its capacity and causing a silent pipeline deadlock.
        tailBlock.LinkTo(DataflowBlock.NullTarget<TTail>());

        return new DataflowPipelineBuilder<THead, TTail>(logger, headBlock, filterBlock, deadLetterQueueBlock, retryOptions, pipelineCancellationToken, _branchCompletionTasks, _steps);
    }

    /// <inheritdoc/>
    public IDataflowPipelineBuilder<THead, TTail[]> AddGroupWhileStep(Func<TTail, TTail, bool> groupingCondition, PipelineStepOptions options = default)
    {
        ExecutionDataflowBlockOptions dataFlowOptions = options.ToDataflowOptions(pipelineCancellationToken);

        // Must be strictly sequential to evaluate the condition against the previous item accurately.
        if (dataFlowOptions.MaxDegreeOfParallelism > 1)
        {
            logger.LogWarning("Max workers cannot be greater than 1 for AddGroupWhileStep. Overriding to 1.");
            dataFlowOptions.MaxDegreeOfParallelism = 1;
        }

        List<TTail> buffer = [];
        BufferBlock<TTail[]> source = new(dataFlowOptions);
        ActionBlock<TTail> target = new(async item =>
        {
            await TrySendAsync(source, buffer, !groupingCondition(buffer[^1], item)).ConfigureAwait(false);
            buffer.Add(item);
        }, dataFlowOptions);

        // Technical Decision: When the pipeline invokes Complete(), we must force-flush the final 
        // partial batch trapped in the buffer before propagating the completion state downstream.
        target.Completion.ContinueWith(source, () => TrySendAsync(source, buffer, true));

        // Encapsulate combines the receiving ActionBlock and emitting BufferBlock into a single logical Propagator node.
        IPropagatorBlock<TTail, TTail[]> groupBlock = DataflowBlock.Encapsulate(target, source);
        return LinkAndContinue(groupBlock);

        static async Task TrySendAsync(BufferBlock<TTail[]> source, List<TTail> buffer, bool additionalCondition)
        {
            if (buffer.Count > 0 && additionalCondition)
            {
                await source.SendAsync([.. buffer]).ConfigureAwait(false);
                buffer.Clear();
            }
        }
    }

    /// <inheritdoc/>
    public IDataflowPipelineBuilder<THead, TTail[]> AddCollectAllStep(PipelineStepOptions options = default) => AddGroupWhileStep((_, _) => true, options);

    /// <inheritdoc/>
    public IDataflowPipelineBuilder<THead, TNextOut> AddStep<TNextOut>(ISyncPipelineStep<TTail, TNextOut> step, PipelineStepOptions options = default)
    {
        _steps.Add(step);
        ExecutionDataflowBlockOptions dataFlowOptions = options.ToDataflowOptions(pipelineCancellationToken);

        if (_deadLetterQueueEnabled)
        {
            TransformBlock<TTail, SafeResult<TTail, TNextOut>> safeBlock = new(TelemetryExtensions.HandleSafeExecution<TTail, TNextOut>(logger, GetTelemetryName(step.Name), step.Execute, pipelineCancellationToken), dataFlowOptions);
            return AddSafeStep(safeBlock, dataFlowOptions);
        }

        TransformBlock<TTail, TNextOut> nextBlock = new(TelemetryExtensions.HandleExecution<TTail, TNextOut>(logger, GetTelemetryName(step.Name), step.Execute, pipelineCancellationToken), dataFlowOptions);
        return LinkAndContinue(nextBlock);
    }

    /// <inheritdoc/>
    public IDataflowPipelineBuilder<THead, TNextOut> AddStep<TNextOut>(IAsyncPipelineStep<TTail, TNextOut> step, PipelineStepOptions options = default)
    {
        _steps.Add(step);
        ExecutionDataflowBlockOptions dataFlowOptions = options.ToDataflowOptions(pipelineCancellationToken);

        if (_deadLetterQueueEnabled)
        {
            TransformBlock<TTail, SafeResult<TTail, TNextOut>> safeBlock = new(
                TelemetryExtensions.HandleSafeExecution<TTail, TNextOut>(logger, GetTelemetryName(step.Name), step.ExecuteAsync, retryOptions, pipelineCancellationToken),
                dataFlowOptions);
            return AddSafeStep(safeBlock, dataFlowOptions);
        }

        TransformBlock<TTail, TNextOut> nextBlock = new(TelemetryExtensions.HandleExecution<TTail, TNextOut>(logger, GetTelemetryName(step.Name), step.ExecuteAsync, retryOptions, pipelineCancellationToken), dataFlowOptions);
        return LinkAndContinue(nextBlock);
    }

    /// <inheritdoc/>
    public IDataflowPipelineBuilder<THead, TNextOut> AddStep<TNextOut>(IAsyncEnumerablePipelineStep<TTail, TNextOut> step, PipelineStepOptions options = default)
    {
        _steps.Add(step);

        Func<TTail, CancellationToken, IAsyncEnumerable<TNextOut>> stepFunc = step.ExecuteAsync;

        ExecutionDataflowBlockOptions dataFlowOptions = options.ToDataflowOptions(pipelineCancellationToken);
        BufferBlock<TNextOut> source = new(dataFlowOptions);

        ActionBlock<TTail> target;
        if (_deadLetterQueueEnabled)
        {
            Func<TTail, IAsyncEnumerable<SafeResult<TTail, TNextOut>>> safeStreamFunc = TelemetryExtensions.HandleSafeExecution(logger, GetTelemetryName(step.Name), stepFunc, pipelineCancellationToken);
            target = new(async item =>
            {
                await foreach (SafeResult<TTail, TNextOut> safeResult in safeStreamFunc(item).WithCancellation(pipelineCancellationToken).ConfigureAwait(false))
                {
                    if (safeResult.Success)
                        await source.SendAsync(safeResult.Output!, pipelineCancellationToken).ConfigureAwait(false);
                    else
                        await deadLetterQueueBlock!.SendAsync(safeResult.FailedPayload, pipelineCancellationToken).ConfigureAwait(false);
                }
            }, dataFlowOptions);
        }
        else
        {
            Func<TTail, IAsyncEnumerable<TNextOut>> streamFunc = TelemetryExtensions.HandleExecution(logger, GetTelemetryName(step.Name), stepFunc, pipelineCancellationToken);
            target = new(async item =>
            {
                await foreach (TNextOut outItem in streamFunc(item).WithCancellation(pipelineCancellationToken).ConfigureAwait(false))
                {
                    await source.SendAsync(outItem, pipelineCancellationToken).ConfigureAwait(false);
                }
            }, dataFlowOptions);
        }

        target.Completion.ContinueWith(source);
        return LinkAndContinue(DataflowBlock.Encapsulate(target, source));
    }

    public IDataflowPipelineBuilder<THead, TNextOut> AddForkingStep<TNextOut>(ISyncPipelineStep<TTail, TNextOut> step, Func<TNextOut, bool> fallbackCondition, ISyncPipelineStep<TTail, TNextOut> fallbackStep, PipelineStepOptions options = default)
    {
        _steps.Add(step);
        _steps.Add(fallbackStep);

        ExecutionDataflowBlockOptions dataFlowOptions = options.ToDataflowOptions(pipelineCancellationToken);

        if (_deadLetterQueueEnabled)
        {
            TransformBlock<TTail, SafeResult<TTail, TNextOut>> safeBlock = new(
                TelemetryExtensions.HandleSafeExecution<TTail, TNextOut>(
                    logger,
                    GetTelemetryName(step.Name),
                    GetTelemetryName(fallbackStep.Name),
                    step.Execute,
                    safeResult => safeResult.Success && safeResult.Output is not null && fallbackCondition(safeResult.Output),
                    fallbackStep.Execute,
                    pipelineCancellationToken),
                dataFlowOptions);
            return AddSafeStep(safeBlock, dataFlowOptions);
        }

        TransformBlock<TTail, TNextOut> nextBlock = new(
            TelemetryExtensions.HandleExecution<TTail, TNextOut>(logger, GetTelemetryName(step.Name), GetTelemetryName(fallbackStep.Name), step.Execute, fallbackCondition, fallbackStep.Execute, pipelineCancellationToken),
            dataFlowOptions);
        return LinkAndContinue(nextBlock);
    }

    public IDataflowPipelineBuilder<THead, TNextOut> AddForkingStep<TNextOut>(IAsyncPipelineStep<TTail, TNextOut> step, Func<TNextOut, bool> fallbackCondition, IAsyncPipelineStep<TTail, TNextOut> fallbackStep, PipelineStepOptions options = default)
    {
        _steps.Add(step);
        _steps.Add(fallbackStep);

        ExecutionDataflowBlockOptions dataFlowOptions = options.ToDataflowOptions(pipelineCancellationToken);

        if (_deadLetterQueueEnabled)
        {
            TransformBlock<TTail, SafeResult<TTail, TNextOut>> safeBlock = new(
                TelemetryExtensions.HandleSafeExecution<TTail, TNextOut>(
                    logger,
                    GetTelemetryName(step.Name),
                    GetTelemetryName(fallbackStep.Name),
                    step.ExecuteAsync, safeResult => safeResult.Success && safeResult.Output is not null && fallbackCondition(safeResult.Output),
                    fallbackStep.ExecuteAsync,
                    retryOptions,
                    pipelineCancellationToken),
                dataFlowOptions);
            return AddSafeStep(safeBlock, dataFlowOptions);
        }

        TransformBlock<TTail, TNextOut> nextBlock = new(TelemetryExtensions.HandleExecution<TTail, TNextOut>(logger, GetTelemetryName(step.Name), GetTelemetryName(fallbackStep.Name), step.ExecuteAsync, fallbackCondition, fallbackStep.ExecuteAsync, retryOptions, pipelineCancellationToken)!, dataFlowOptions);
        return LinkAndContinue(nextBlock);
    }

    public IDataflowPipelineBuilder<THead, TTail> AddBroadcastStep(IAsyncPipelineStep<TTail> step, Func<TTail, TTail>? cloneFunc = null, PipelineStepOptions options = default)
    {
        _steps.Add(step);

        ExecutionDataflowBlockOptions dataFlowOptions = options.ToDataflowOptions(pipelineCancellationToken);

        BroadcastBlock<TTail> broadcastBlock = new(cloneFunc, dataFlowOptions);
        tailBlock.LinkTo(broadcastBlock);

        (ITargetBlock<TTail> targetBlock, Task completionTask) = CreateConsumer(step.Name, step.ExecuteAsync, dataFlowOptions);
        broadcastBlock.LinkTo(targetBlock);

        List<Task> updatedTasks = [.. _branchCompletionTasks, completionTask];
        return new DataflowPipelineBuilder<THead, TTail>(logger, headBlock, broadcastBlock, deadLetterQueueBlock, retryOptions, pipelineCancellationToken, updatedTasks, _steps);
    }

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

    private (ITargetBlock<TTail> TargetBlock, Task CompletionTask) CreateConsumer(string stepName, Action<TTail> action, ExecutionDataflowBlockOptions dataFlowOptions)
    {
        if (_deadLetterQueueEnabled)
        {
            TransformBlock<TTail, SafeResult<TTail>> safeBlock = new(TelemetryExtensions.HandleSafeExecution(logger, GetTelemetryName(stepName), action, pipelineCancellationToken), dataFlowOptions);
            return LinkDeadLetterQueue(safeBlock, dataFlowOptions);
        }

        ActionBlock<TTail> terminalBlock = new(TelemetryExtensions.HandleExecution(logger, GetTelemetryName(stepName), action, pipelineCancellationToken), dataFlowOptions);
        return (terminalBlock, terminalBlock.Completion);
    }

    private (ITargetBlock<TTail> TargetBlock, Task CompletionTask) CreateConsumer(string stepName, Func<TTail, CancellationToken, Task> action, ExecutionDataflowBlockOptions dataFlowOptions)
    {
        if (_deadLetterQueueEnabled)
        {
            TransformBlock<TTail, SafeResult<TTail>> safeBlock = new(TelemetryExtensions.HandleSafeExecution(logger, GetTelemetryName(stepName), action, retryOptions, pipelineCancellationToken), dataFlowOptions);
            return LinkDeadLetterQueue(safeBlock, dataFlowOptions);
        }

        ActionBlock<TTail> terminalBlock = new(TelemetryExtensions.HandleExecution(logger, GetTelemetryName(stepName), action, retryOptions, pipelineCancellationToken), dataFlowOptions);
        return (terminalBlock, terminalBlock.Completion);
    }

    private (ITargetBlock<TTail> TargetBlock, Task CompletionTask) LinkDeadLetterQueue(TransformBlock<TTail, SafeResult<TTail>> safeBlock, ExecutionDataflowBlockOptions dataFlowOptions)
    {
        ActionBlock<SafeResult<TTail>> failedBlock = new(async safeResult => await deadLetterQueueBlock!.SendAsync(safeResult.FailedPayload, pipelineCancellationToken).ConfigureAwait(false), dataFlowOptions);
        safeBlock.LinkTo(failedBlock, safeResult => !safeResult.Success);
        safeBlock.LinkTo(DataflowBlock.NullTarget<SafeResult<TTail>>());

        return (safeBlock, Task.WhenAll(safeBlock.Completion, failedBlock.Completion));
    }

    private DataflowPipelineBuilder<THead, TTail> WithDeadLetterQueue(ITargetBlock<FailedPayload> deadLetterQueueSink) => new(logger, headBlock, tailBlock, deadLetterQueueSink, retryOptions, pipelineCancellationToken, _branchCompletionTasks);

    private IDataflowPipeline<THead> BuildTerminalFromConsumer(ITargetBlock<TTail> consumerBlock, Task completionTask)
    {
        tailBlock.LinkTo(consumerBlock);
        Task finalCompletionTask = _branchCompletionTasks.Count > 0 ? Task.WhenAll([completionTask, .. _branchCompletionTasks]) : completionTask;
        return new DataflowPipeline<THead>(logger, headBlock, finalCompletionTask, _steps);
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

        return new DataflowPipelineBuilder<THead, TNextOut>(logger, headBlock, successBlock, deadLetterQueueBlock, retryOptions, pipelineCancellationToken, _branchCompletionTasks, _steps);
    }

    private DataflowPipelineBuilder<THead, TNextOut> LinkAndContinue<TNextOut>(IPropagatorBlock<TTail, TNextOut> nextBlock)
    {
        tailBlock.LinkTo(nextBlock);
        return new DataflowPipelineBuilder<THead, TNextOut>(logger, headBlock, nextBlock, deadLetterQueueBlock, retryOptions, pipelineCancellationToken, _branchCompletionTasks, _steps);
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
file sealed class DataflowPipeline<TIn>(ILogger logger, ITargetBlock<TIn> headBlock, Task completionTask, IReadOnlyCollection<IPipelineStep> steps) : IDataflowPipeline<TIn>
{
    /// <inheritdoc/>
    public Task<bool> SendAsync(TIn item, CancellationToken cancellationToken = default) => headBlock.SendAsync(item, cancellationToken);

    /// <inheritdoc/>
    public void Complete()
    {
        logger.LogInformation("Pipeline completion invoked. Draining buffered messages and propagating completion state.");
        headBlock.Complete();
    }

    /// <inheritdoc/>
    public Task Completion => completionTask;

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        Complete();
        await Completion.ConfigureAwait(false);

        foreach (IPipelineStep step in steps)
        {
            if (step is IAsyncDisposable asyncDisposableStep)
                await asyncDisposableStep.DisposeAsync().ConfigureAwait(false);
            else if (step is IDisposable disposableStep)
                disposableStep.Dispose();
        }
    }
}
