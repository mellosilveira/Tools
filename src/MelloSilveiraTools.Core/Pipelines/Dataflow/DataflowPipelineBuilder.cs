using MelloSilveiraTools.Core.ExtensionMethods;
using MelloSilveiraTools.Core.Pipelines.Models;
using MelloSilveiraTools.Core.Pipelines.Steps;
using MelloSilveiraTools.Core.Pipelines.Telemetry;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks.Dataflow;

namespace MelloSilveiraTools.Core.Pipelines.Dataflow;

/// <summary>
/// Strongly-typed fluent builder for orchestrating asynchronous data processing topologies.
/// </summary>
/// <remarks>
/// Design: Encapsulates block instantiation, telemetry wrapping, and graph linkage. Dynamically injects bifurcated routing nodes for DLQ without exposing graph complexity.
/// Constraint: Assumes a linear or singular-convergence topology. Forked branches must reconcile back to a single primary data type to proceed.
/// Note: Circuit-breaker and rate-limiter resilience strategies are planned for future integration at the block level.
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
    /// Design: Automatically provisions an internal ActionBlock to integrate the synchronous handler into the execution graph.
    /// Constraint: Blocking I/O stalls the underlying ThreadPool thread assigned to this terminal block.
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
    /// Design: Provides optimal DLQ routing via async execution (e.g., remote queues), honoring configured retry constraints.
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
    /// Design: Fallback fault tolerance to prevent cascading block failures without complex recovery logic.
    /// Constraint: Failed payloads are strictly serialized to logs and discarded from memory.
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
    /// Design: Dynamically shifts to a safe execution block if DLQ is active, routing exceptions internally without faulting the block.
    /// Constraint: Purely synchronous. Retry policies are bypassed to prevent thread exhaustion.
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
    public IDataflowPipelineBuilder<THead, TTail> AddFilterStep(Predicate<TTail> predicate, PipelineStepOptions options = default)
    {
        BufferBlock<TTail> filterBlock = new(options.ToDataflowOptions(pipelineCancellationToken));

        // Design: Native routing via predicate. True evaluations are forwarded with zero memory allocation.
        tailBlock.LinkTo(filterBlock, predicate);

        // Constraint: Rejected items must route to a NullTarget. Missing this causes the tailBlock buffer to overflow and deadlock the pipeline.
        tailBlock.LinkTo(DataflowBlock.NullTarget<TTail>());

        return new DataflowPipelineBuilder<THead, TTail>(logger, headBlock, filterBlock, deadLetterQueueBlock, retryOptions, pipelineCancellationToken, _branchCompletionTasks, _steps);
    }

    internal ISourceBlock<TTail> TailBlock => tailBlock;

    /// <inheritdoc/>
    public IDataflowPipelineBuilder<THead, Tuple<TOut1, TOut2>> Fork<TOut1, TOut2>(
        Func<IDataflowPipelineBuilder<THead, TTail>, IDataflowPipelineBuilder<THead, TOut1>> branch1,
        Func<IDataflowPipelineBuilder<THead, TTail>, IDataflowPipelineBuilder<THead, TOut2>> branch2,
        PipelineStepOptions options = default)
    {
        ExecutionDataflowBlockOptions dataFlowOptions = options.ToDataflowOptions(pipelineCancellationToken);

        // 1. Create a BroadcastBlock to copy the same payload to both branches.
        BroadcastBlock<TTail> broadcastBlock = new(msg => msg, dataFlowOptions);
        tailBlock.LinkTo(broadcastBlock);

        // 2. Initialize branch builders stemming from the broadcast block.
        DataflowPipelineBuilder<THead, TTail> builder1 = new(logger, headBlock, broadcastBlock, deadLetterQueueBlock, retryOptions, pipelineCancellationToken, _branchCompletionTasks, _steps);
        DataflowPipelineBuilder<THead, TTail> builder2 = new(logger, headBlock, broadcastBlock, deadLetterQueueBlock, retryOptions, pipelineCancellationToken, _branchCompletionTasks, _steps);

        // 3. Create the convergence JoinBlock.
        JoinBlock<TOut1, TOut2> joinBlock = new(new GroupingDataflowBlockOptions
        {
            BoundedCapacity = dataFlowOptions.BoundedCapacity,
            CancellationToken = pipelineCancellationToken
        });

        // 4. Connect the terminal ends of the branches to the JoinBlock.
        ((DataflowPipelineBuilder<THead, TOut1>)branch1(builder1)).TailBlock.LinkTo(joinBlock.Target1);
        ((DataflowPipelineBuilder<THead, TOut2>)branch2(builder2)).TailBlock.LinkTo(joinBlock.Target2);
        return new DataflowPipelineBuilder<THead, Tuple<TOut1, TOut2>>(logger, headBlock, joinBlock, deadLetterQueueBlock, retryOptions, pipelineCancellationToken, _branchCompletionTasks, _steps);
    }

    /// <inheritdoc/>
    public IDataflowPipelineBuilder<THead, Tuple<TOut1, TOut2, TOut3>> Fork<TOut1, TOut2, TOut3>(
        Func<IDataflowPipelineBuilder<THead, TTail>, IDataflowPipelineBuilder<THead, TOut1>> branch1,
        Func<IDataflowPipelineBuilder<THead, TTail>, IDataflowPipelineBuilder<THead, TOut2>> branch2,
        Func<IDataflowPipelineBuilder<THead, TTail>, IDataflowPipelineBuilder<THead, TOut3>> branch3,
        PipelineStepOptions options = default)
    {
        ExecutionDataflowBlockOptions dataFlowOptions = options.ToDataflowOptions(pipelineCancellationToken);

        BroadcastBlock<TTail> broadcastBlock = new(msg => msg, dataFlowOptions);
        tailBlock.LinkTo(broadcastBlock);

        DataflowPipelineBuilder<THead, TTail> builder1 = new(logger, headBlock, broadcastBlock, deadLetterQueueBlock, retryOptions, pipelineCancellationToken, _branchCompletionTasks, _steps);
        DataflowPipelineBuilder<THead, TTail> builder2 = new(logger, headBlock, broadcastBlock, deadLetterQueueBlock, retryOptions, pipelineCancellationToken, _branchCompletionTasks, _steps);
        DataflowPipelineBuilder<THead, TTail> builder3 = new(logger, headBlock, broadcastBlock, deadLetterQueueBlock, retryOptions, pipelineCancellationToken, _branchCompletionTasks, _steps);

        JoinBlock<TOut1, TOut2, TOut3> joinBlock = new(new GroupingDataflowBlockOptions
        {
            BoundedCapacity = dataFlowOptions.BoundedCapacity,
            CancellationToken = pipelineCancellationToken
        });

        ((DataflowPipelineBuilder<THead, TOut1>)branch1(builder1)).TailBlock.LinkTo(joinBlock.Target1);
        ((DataflowPipelineBuilder<THead, TOut2>)branch2(builder2)).TailBlock.LinkTo(joinBlock.Target2);
        ((DataflowPipelineBuilder<THead, TOut3>)branch3(builder3)).TailBlock.LinkTo(joinBlock.Target3);
        return new DataflowPipelineBuilder<THead, Tuple<TOut1, TOut2, TOut3>>(logger, headBlock, joinBlock, deadLetterQueueBlock, retryOptions, pipelineCancellationToken, _branchCompletionTasks, _steps);
    }

    /// <inheritdoc/>
    public IDataflowPipelineBuilder<THead, Tuple<TOut1, TOut2>> Fork<TOut1, TOut2>(IPipelineStep<TTail, TOut1> branch1Step, IPipelineStep<TTail, TOut2> branch2Step, PipelineStepOptions options = default)
        => Fork(b => AddStepDynamic(b, branch1Step), b => AddStepDynamic(b, branch2Step), options);

    /// <inheritdoc/>
    public IDataflowPipelineBuilder<THead, Tuple<TOut1, TOut2, TOut3>> Fork<TOut1, TOut2, TOut3>(IPipelineStep<TTail, TOut1> branch1Step, IPipelineStep<TTail, TOut2> branch2Step, IPipelineStep<TTail, TOut3> branch3Step, PipelineStepOptions options = default)
        => Fork(b => AddStepDynamic(b, branch1Step), b => AddStepDynamic(b, branch2Step), b => AddStepDynamic(b, branch3Step), options);

    private static IDataflowPipelineBuilder<THead, TOut> AddStepDynamic<TOut>(IDataflowPipelineBuilder<THead, TTail> builder, IPipelineStep<TTail, TOut> step) => step switch
    {
        ISyncPipelineStep<TTail, TOut> syncStep => builder.AddStep(syncStep),
        IAsyncPipelineStep<TTail, TOut> asyncStep => builder.AddStep(asyncStep),
        IAsyncEnumerablePipelineStep<TTail, TOut> enumStep => builder.AddStep(enumStep),
        _ => throw new NotSupportedException($"Pipeline step type '{step.GetType().Name}' is not supported in this Fork overload.")
    };

    /// <inheritdoc/>
    public IDataflowPipelineBuilder<THead, TTail[]> AddGroupWhileStep(Func<TTail, TTail, bool> groupingCondition, PipelineStepOptions options = default)
    {
        ExecutionDataflowBlockOptions dataFlowOptions = options.ToDataflowOptions(pipelineCancellationToken);

        // Constraint: Must be strictly sequential to accurately evaluate the condition against the previous item.
        if (dataFlowOptions.MaxDegreeOfParallelism > 1)
        {
            logger.LogWarning("Max workers cannot be greater than 1 for AddGroupWhileStep. Overriding to 1.");
            dataFlowOptions.MaxDegreeOfParallelism = 1;
        }

        List<TTail> buffer = new(128);
        BufferBlock<TTail[]> source = new(dataFlowOptions);
        ActionBlock<TTail> target = new(async item =>
        {
            if (buffer.Count > 0 && !groupingCondition(buffer[^1], item))
            {
                await source.SendAsync([.. buffer], pipelineCancellationToken).ConfigureAwait(false);
                buffer.Clear();
            }
            buffer.Add(item);
        }, dataFlowOptions);

        // Design: Forces flushing of the final partial batch trapped in the buffer before propagating completion downstream.
        target.Completion.ContinueWith(source, async () =>
        {
            if (buffer.Count > 0)
            {
                await source.SendAsync([.. buffer], pipelineCancellationToken).ConfigureAwait(false);
                buffer.Clear();
            }
        });

        // Design: Encapsulates the receiving and emitting blocks into a single logical Propagator node.
        IPropagatorBlock<TTail, TTail[]> groupBlock = DataflowBlock.Encapsulate(target, source);
        return LinkAndContinue(groupBlock);
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

    /// <inheritdoc/>
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

    /// <inheritdoc/>
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

    /// <inheritdoc/>
    public IDataflowPipelineBuilder<THead, TTail> AddBroadcastStep(IAsyncPipelineStep<TTail> step, Func<TTail, TTail>? cloneFunc = null, PipelineStepOptions options = default)
    {
        _steps.Add(step);

        ExecutionDataflowBlockOptions dataFlowOptions = options.ToDataflowOptions(pipelineCancellationToken);

        BroadcastBlock<TTail> broadcastBlock = new(cloneFunc, dataFlowOptions);
        tailBlock.LinkTo(broadcastBlock);

        (ITargetBlock<TTail> targetBlock, Task completionTask) = CreateConsumer<TTail>(step.Name, step.ExecuteAsync, dataFlowOptions);
        broadcastBlock.LinkTo(targetBlock);

        List<Task> updatedTasks = [.. _branchCompletionTasks, completionTask];
        return new DataflowPipelineBuilder<THead, TTail>(logger, headBlock, broadcastBlock, deadLetterQueueBlock, retryOptions, pipelineCancellationToken, updatedTasks, _steps);
    }

    /// <inheritdoc/>
    public IDataflowPipelineBuilder<THead, TTail> AddBroadcastStep<TOut>(IAsyncPipelineStep<TOut> step, Func<TTail, TOut> mapFunc, PipelineStepOptions options = default)
    {
        _steps.Add(step);

        ExecutionDataflowBlockOptions dataFlowOptions = options.ToDataflowOptions(pipelineCancellationToken);

        BroadcastBlock<TTail> broadcastBlock = new(null, dataFlowOptions);
        tailBlock.LinkTo(broadcastBlock);

        TransformBlock<TTail, TOut> mapBlock = new(mapFunc, new ExecutionDataflowBlockOptions
        {
            CancellationToken = pipelineCancellationToken,
            EnsureOrdered = true,
            MaxDegreeOfParallelism = 1
        });

        broadcastBlock.LinkTo(mapBlock);

        (ITargetBlock<TOut> targetBlock, Task completionTask) = CreateConsumer<TOut>(step.Name, step.ExecuteAsync, dataFlowOptions);
        mapBlock.LinkTo(targetBlock);

        List<Task> updatedTasks = [.. _branchCompletionTasks, completionTask];
        return new DataflowPipelineBuilder<THead, TTail>(logger, headBlock, broadcastBlock, deadLetterQueueBlock, retryOptions, pipelineCancellationToken, updatedTasks, _steps);
    }

    /// <inheritdoc/>
    public IDataflowPipeline<THead> BuildTerminal(string stepName, Action<TTail> terminalAction, PipelineStepOptions options = default)
    {
        (ITargetBlock<TTail> consumerBlock, Task completionTask) = CreateConsumer<TTail>(stepName, terminalAction, options.ToDataflowOptions(pipelineCancellationToken));
        return BuildTerminalFromConsumer(consumerBlock, completionTask);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Design: Caps the underlying graph by resolving to a final asynchronous block, returning a sealed interface preventing further linkage.
    /// </remarks>
    public IDataflowPipeline<THead> BuildTerminal(string stepName, Func<TTail, CancellationToken, Task> terminalAction, PipelineStepOptions options = default)
    {
        (ITargetBlock<TTail> consumerBlock, Task completionTask) = CreateConsumer<TTail>(stepName, terminalAction, options.ToDataflowOptions(pipelineCancellationToken));
        return BuildTerminalFromConsumer(consumerBlock, completionTask);
    }

    /// <inheritdoc/>
    public IDataflowPipeline<THead> BuildTerminal(string stepName = "Terminal", PipelineStepOptions options = default) => BuildTerminal(stepName, _ => { }, options);

    private (ITargetBlock<TInput> TargetBlock, Task CompletionTask) CreateConsumer<TInput>(string stepName, Action<TInput> action, ExecutionDataflowBlockOptions dataFlowOptions)
    {
        if (_deadLetterQueueEnabled)
        {
            TransformBlock<TInput, SafeResult<TInput>> safeBlock = new(TelemetryExtensions.HandleSafeExecution(logger, GetTelemetryName(stepName), action, pipelineCancellationToken), dataFlowOptions);
            return LinkDeadLetterQueue(safeBlock, dataFlowOptions);
        }

        ActionBlock<TInput> terminalBlock = new(TelemetryExtensions.HandleExecution(logger, GetTelemetryName(stepName), action, pipelineCancellationToken), dataFlowOptions);
        return (terminalBlock, terminalBlock.Completion);
    }

    private (ITargetBlock<TInput> TargetBlock, Task CompletionTask) CreateConsumer<TInput>(string stepName, Func<TInput, CancellationToken, Task> action, ExecutionDataflowBlockOptions dataFlowOptions)
    {
        if (_deadLetterQueueEnabled)
        {
            TransformBlock<TInput, SafeResult<TInput>> safeBlock = new(TelemetryExtensions.HandleSafeExecution(logger, GetTelemetryName(stepName), action, retryOptions, pipelineCancellationToken), dataFlowOptions);
            return LinkDeadLetterQueue(safeBlock, dataFlowOptions);
        }

        ActionBlock<TInput> terminalBlock = new(TelemetryExtensions.HandleExecution(logger, GetTelemetryName(stepName), action, retryOptions, pipelineCancellationToken), dataFlowOptions);
        return (terminalBlock, terminalBlock.Completion);
    }

    private (ITargetBlock<TInput> TargetBlock, Task CompletionTask) LinkDeadLetterQueue<TInput>(TransformBlock<TInput, SafeResult<TInput>> safeBlock, ExecutionDataflowBlockOptions dataFlowOptions)
    {
        ActionBlock<SafeResult<TInput>> failedBlock = new(async safeResult => await deadLetterQueueBlock!.SendAsync(safeResult.FailedPayload, pipelineCancellationToken).ConfigureAwait(false), dataFlowOptions);
        safeBlock.LinkTo(failedBlock, safeResult => !safeResult.Success);
        safeBlock.LinkTo(DataflowBlock.NullTarget<SafeResult<TInput>>());

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
    /// Constructs a bifurcated routing topology to handle safe result payloads.
    /// </summary>
    /// <remarks>
    /// Design: Splits a single logical step into three blocks (execution wrapper, success router, failure router) to keep consumer configuration clean while satisfying complex routing constraints.
    /// </remarks>
    private DataflowPipelineBuilder<THead, TNextOut> AddSafeStep<TNextOut>(TransformBlock<TTail, SafeResult<TTail, TNextOut>> safeBlock, ExecutionDataflowBlockOptions dataFlowOptions)
    {
        tailBlock.LinkTo(safeBlock);

        TransformBlock<SafeResult<TTail, TNextOut>, TNextOut> successBlock = new(safeResult => safeResult.Output!, dataFlowOptions);
        safeBlock.LinkTo(successBlock, safeResult => safeResult.Success);

        ActionBlock<SafeResult<TTail, TNextOut>> failedBlock = new(async safeResult => await deadLetterQueueBlock!.SendAsync(safeResult.FailedPayload, pipelineCancellationToken).ConfigureAwait(false), dataFlowOptions);
        safeBlock.LinkTo(failedBlock, safeResult => !safeResult.Success);

        List<Task> updatedTasks = [.. _branchCompletionTasks, failedBlock.Completion];
        return new DataflowPipelineBuilder<THead, TNextOut>(logger, headBlock, successBlock, deadLetterQueueBlock, retryOptions, pipelineCancellationToken, updatedTasks, _steps);
    }

    private DataflowPipelineBuilder<THead, TNextOut> LinkAndContinue<TNextOut>(IPropagatorBlock<TTail, TNextOut> nextBlock)
    {
        tailBlock.LinkTo(nextBlock);
        return new DataflowPipelineBuilder<THead, TNextOut>(logger, headBlock, nextBlock, deadLetterQueueBlock, retryOptions, pipelineCancellationToken, _branchCompletionTasks, _steps);
    }

    private static string GetTelemetryName(string stepName) => $"Pipeline.Step.{stepName}";
}

/// <summary>
/// Sealed execution engine representing the finalized graph.
/// Exposes inputs, triggers completion, and awaits termination while automatically disposing registered steps on shutdown.
/// </summary>
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
        try
        {
            await Completion.ConfigureAwait(false);
        }
        finally
        {
            foreach (IPipelineStep step in steps)
            {
                if (step is IAsyncDisposable asyncDisposableStep)
                    await asyncDisposableStep.DisposeAsync().ConfigureAwait(false);
                else if (step is IDisposable disposableStep)
                    disposableStep.Dispose();
            }
        }
    }
}
