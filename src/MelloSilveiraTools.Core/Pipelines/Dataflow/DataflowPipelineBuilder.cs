using MelloSilveiraTools.Core.ExtensionMethods;
using MelloSilveiraTools.Core.Pipelines.Models;
using MelloSilveiraTools.Core.Pipelines.Notifiers;
using MelloSilveiraTools.Core.Pipelines.Steps;
using MelloSilveiraTools.Core.Pipelines.Telemetry;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks.Dataflow;

namespace MelloSilveiraTools.Core.Pipelines.Dataflow;

/// <summary>
/// Strongly-typed fluent builder for orchestrating asynchronous data processing topologies.
/// </summary>
/// <remarks>
/// Architectural Decisions:
/// <list type="bullet">
/// <item><description><b>DAG Topology Correlation:</b> Tracks a <see cref="PipelineFlowNode"/> and <see cref="PipelineStepTracker"/> per named domain step. Anonymous operators (<c>AddDataMapping</c>, <c>AddFilterStep</c>, <c>AddGroupWhileStep</c>, <c>JoinBlock</c>, and the default no-op <c>BuildTerminal</c>) act as transparent pass-throughs for <see cref="PipelineProgressEvent.UpstreamSteps"/> while accurately propagating item arrival notifications (<c>ReceivedItems</c>).</description></item>
/// <item><description><b>Upfront Topology Discovery:</b> Emits <see cref="PipelineStepState.Pending"/> notifications for all registered steps on the first <c>SendAsync</c> invocation so UI consumers can render the entire DAG before items finish flowing.</description></item>
/// <item><description><b>Bifurcated DLQ Routing:</b> When a Dead-Letter Queue is configured, steps dynamically wrap execution in <see cref="SafeResult{TIn, TOut}"/> and split successful vs. faulted payloads without faulting the TPL Dataflow graph.</description></item>
/// </list>
/// </remarks>
internal class DataflowPipelineBuilder<THead, TTail>(
    ILogger logger,
    IPipelineProgressNotifier progressNotifier,
    ITargetBlock<THead> headBlock,
    ISourceBlock<TTail> tailBlock,
    ITargetBlock<FailedPayload>? deadLetterQueueBlock,
    RetryOptions? retryOptions,
    PipelineFlowNode rootFlowNode,
    PipelineFlowNode currentFlowNode,
    TimeProvider timeProvider,
    PipelineProgressNotificationSettings? notificationSettings = null,
    List<PipelineStepTracker>? stepTrackers = null,
    List<Task>? branchCompletionTasks = null,
    List<IPipelineStep>? steps = null,
    CancellationToken pipelineCancellationToken = default)
    : IDataflowPipelineBuilder<THead, TTail>
{
    private const string DeadLetterQueueTelemetryName = "Pipeline.DeadLetterQueue";
    private const string DataMappingTelemetryName = "Pipeline.DataMapping";
    private readonly bool _deadLetterQueueEnabled = deadLetterQueueBlock is not null;
    private readonly List<PipelineStepTracker> _stepTrackers = stepTrackers ?? [];
    private readonly List<Task> _branchCompletionTasks = branchCompletionTasks ?? [];
    private readonly List<IPipelineStep> _steps = steps ?? [];

    internal ISourceBlock<TTail> TailBlock => tailBlock;
    internal PipelineFlowNode CurrentFlowNode => currentFlowNode;

    /// <inheritdoc/>
    public IDataflowPipelineBuilder<THead, TTail> WithDeadLetterQueue(Action<FailedPayload> errorHandler, PipelineStepOptions options = default)
    {
        ActionBlock<FailedPayload> actionBlock = new(
            PipelineStepExecutor.HandleExecution(logger, progressNotifier, timeProvider, DeadLetterQueueTelemetryName, errorHandler, pipelineCancellationToken),
            options.ToDataflowOptions(pipelineCancellationToken));

        return WithDeadLetterQueue(actionBlock);
    }

    /// <inheritdoc/>
    public IDataflowPipelineBuilder<THead, TTail> WithDeadLetterQueue(Func<FailedPayload, CancellationToken, Task> errorHandler, PipelineStepOptions options = default)
    {
        ActionBlock<FailedPayload> actionBlock = new(
            PipelineStepExecutor.HandleExecution(logger, progressNotifier, timeProvider, DeadLetterQueueTelemetryName, errorHandler, retryOptions, pipelineCancellationToken),
            options.ToDataflowOptions(pipelineCancellationToken));

        return WithDeadLetterQueue(actionBlock);
    }

    /// <inheritdoc/>
    public IDataflowPipelineBuilder<THead, TTail> WithLoggingErrors(PipelineStepOptions options = default)
    {
        ActionBlock<FailedPayload> actionBlock = new(
            failedPayload => logger.LogError("Failed to execute step. Failed payload: {@FailedPayload}", failedPayload),
            options.ToDataflowOptions(pipelineCancellationToken));

        return WithDeadLetterQueue(actionBlock);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Preserves upstream step names in the DAG while propagating item arrival notifications only for successfully mapped payloads.
    /// </remarks>
    public IDataflowPipelineBuilder<THead, TNextOut> AddDataMapping<TNextOut>(Func<TTail, TNextOut> mapFunc, PipelineStepOptions options = default)
    {
        ExecutionDataflowBlockOptions dataFlowOptions = options.ToDataflowOptions(pipelineCancellationToken);
        PipelineFlowNode nextFlowNode = new(currentFlowNode.UpstreamStepNames);

        async Task<TNextOut> mappedFuncAsync(TTail item, CancellationToken ct)
        {
            TNextOut mapped = mapFunc(item);
            await nextFlowNode.NotifyItemEmittedAsync(ct).ConfigureAwait(false);
            return mapped;
        }

        if (_deadLetterQueueEnabled)
        {
            TransformBlock<TTail, SafeResult<TTail, TNextOut>> safeBlock = new(
                PipelineStepExecutor.HandleSafeExecution<TTail, TNextOut>(logger, progressNotifier, timeProvider, DataMappingTelemetryName, mappedFuncAsync, null, pipelineCancellationToken),
                dataFlowOptions);
            return AddSafeStep(safeBlock, dataFlowOptions, nextFlowNode);
        }

        TransformBlock<TTail, TNextOut> nextBlock = new(
            PipelineStepExecutor.HandleExecution<TTail, TNextOut>(logger, progressNotifier, timeProvider, DataMappingTelemetryName, mappedFuncAsync, null, pipelineCancellationToken),
            dataFlowOptions);
        return LinkAndContinue(nextBlock, nextFlowNode);
    }

    /// <inheritdoc/>
    public IDataflowPipelineBuilder<THead, TTail> AddFilterStep(Predicate<TTail> predicate, PipelineStepOptions options = default)
    {
        ExecutionDataflowBlockOptions dataFlowOptions = options.ToDataflowOptions(pipelineCancellationToken);
        PipelineFlowNode nextFlowNode = new(currentFlowNode.UpstreamStepNames);

        TransformBlock<TTail, TTail> filterBlock = new(
            async item =>
            {
                await nextFlowNode.NotifyItemEmittedAsync(pipelineCancellationToken).ConfigureAwait(false);
                return item;
            },
            dataFlowOptions);

        // Native routing via predicate; rejected items route to NullTarget to prevent buffer deadlock.
        tailBlock.LinkTo(filterBlock, predicate);
        tailBlock.LinkTo(DataflowBlock.NullTarget<TTail>());

        return new DataflowPipelineBuilder<THead, TTail>(
            logger,
            progressNotifier,
            headBlock,
            filterBlock,
            deadLetterQueueBlock,
            retryOptions,
            rootFlowNode,
            nextFlowNode,
            timeProvider,
            notificationSettings,
            _stepTrackers,
            _branchCompletionTasks,
            _steps,
            pipelineCancellationToken);
    }

    /// <inheritdoc/>
    public IDataflowPipelineBuilder<THead, Tuple<TOut1, TOut2>> Fork<TOut1, TOut2>(
        Func<IDataflowPipelineBuilder<THead, TTail>, IDataflowPipelineBuilder<THead, TOut1>> branch1,
        Func<IDataflowPipelineBuilder<THead, TTail>, IDataflowPipelineBuilder<THead, TOut2>> branch2,
        PipelineStepOptions options = default)
    {
        ExecutionDataflowBlockOptions dataFlowOptions = options.ToDataflowOptions(pipelineCancellationToken);

        BroadcastBlock<TTail> broadcastBlock = new(msg => msg, dataFlowOptions);
        tailBlock.LinkTo(broadcastBlock);

        DataflowPipelineBuilder<THead, TOut1> resolvedBranch1 = (DataflowPipelineBuilder<THead, TOut1>)branch1(CreateBranchBuilder(broadcastBlock));
        DataflowPipelineBuilder<THead, TOut2> resolvedBranch2 = (DataflowPipelineBuilder<THead, TOut2>)branch2(CreateBranchBuilder(broadcastBlock));

        JoinBlock<TOut1, TOut2> joinBlock = new(new GroupingDataflowBlockOptions
        {
            BoundedCapacity = dataFlowOptions.BoundedCapacity,
            CancellationToken = pipelineCancellationToken
        });

        resolvedBranch1.TailBlock.LinkTo(joinBlock.Target1);
        resolvedBranch2.TailBlock.LinkTo(joinBlock.Target2);

        PipelineFlowNode joinedFlowNode = PipelineFlowNode.CreateJoined([resolvedBranch1.CurrentFlowNode, resolvedBranch2.CurrentFlowNode]);
        List<Task> mergedTasks = MergeBranchCompletionTasks([resolvedBranch1._branchCompletionTasks, resolvedBranch2._branchCompletionTasks]);

        return new DataflowPipelineBuilder<THead, Tuple<TOut1, TOut2>>(
            logger,
            progressNotifier,
            headBlock,
            joinBlock,
            deadLetterQueueBlock,
            retryOptions,
            rootFlowNode,
            joinedFlowNode,
            timeProvider,
            notificationSettings,
            _stepTrackers,
            mergedTasks,
            _steps,
            pipelineCancellationToken);
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

        DataflowPipelineBuilder<THead, TOut1> resolvedBranch1 = (DataflowPipelineBuilder<THead, TOut1>)branch1(CreateBranchBuilder(broadcastBlock));
        DataflowPipelineBuilder<THead, TOut2> resolvedBranch2 = (DataflowPipelineBuilder<THead, TOut2>)branch2(CreateBranchBuilder(broadcastBlock));
        DataflowPipelineBuilder<THead, TOut3> resolvedBranch3 = (DataflowPipelineBuilder<THead, TOut3>)branch3(CreateBranchBuilder(broadcastBlock));

        JoinBlock<TOut1, TOut2, TOut3> joinBlock = new(new GroupingDataflowBlockOptions
        {
            BoundedCapacity = dataFlowOptions.BoundedCapacity,
            CancellationToken = pipelineCancellationToken
        });

        resolvedBranch1.TailBlock.LinkTo(joinBlock.Target1);
        resolvedBranch2.TailBlock.LinkTo(joinBlock.Target2);
        resolvedBranch3.TailBlock.LinkTo(joinBlock.Target3);

        PipelineFlowNode joinedFlowNode = PipelineFlowNode.CreateJoined([resolvedBranch1.CurrentFlowNode, resolvedBranch2.CurrentFlowNode, resolvedBranch3.CurrentFlowNode]);
        List<Task> mergedTasks = MergeBranchCompletionTasks([resolvedBranch1._branchCompletionTasks, resolvedBranch2._branchCompletionTasks, resolvedBranch3._branchCompletionTasks]);

        return new DataflowPipelineBuilder<THead, Tuple<TOut1, TOut2, TOut3>>(
            logger,
            progressNotifier,
            headBlock,
            joinBlock,
            deadLetterQueueBlock,
            retryOptions,
            rootFlowNode,
            joinedFlowNode,
            timeProvider,
            notificationSettings,
            _stepTrackers,
            mergedTasks,
            _steps,
            pipelineCancellationToken);
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
        IEnumerablePipelineStep<TTail, TOut> syncEnumStep => builder.AddStep(syncEnumStep),
        _ => throw new NotSupportedException($"Pipeline step type '{step.GetType().Name}' is not supported in this Fork overload.")
    };

    /// <inheritdoc/>
    public IDataflowPipelineBuilder<THead, TTail[]> AddGroupWhileStep(Func<TTail, TTail, bool> groupingCondition, PipelineStepOptions options = default)
    {
        ExecutionDataflowBlockOptions dataFlowOptions = options.ToDataflowOptions(pipelineCancellationToken);
        PipelineFlowNode nextFlowNode = new(currentFlowNode.UpstreamStepNames);

        // Must be strictly sequential to accurately evaluate the condition against the previous item.
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
                await nextFlowNode.NotifyItemEmittedAsync(pipelineCancellationToken).ConfigureAwait(false);
                await source.SendAsync([.. buffer], pipelineCancellationToken).ConfigureAwait(false);
                buffer.Clear();
            }
            buffer.Add(item);
        }, dataFlowOptions);

        // Flush the final partial batch trapped in the buffer before propagating completion downstream.
        target.Completion.ContinueWith(source, async () =>
        {
            if (buffer.Count > 0)
            {
                await nextFlowNode.NotifyItemEmittedAsync(pipelineCancellationToken).ConfigureAwait(false);
                await source.SendAsync([.. buffer], pipelineCancellationToken).ConfigureAwait(false);
                buffer.Clear();
            }
        });

        IPropagatorBlock<TTail, TTail[]> groupBlock = DataflowBlock.Encapsulate(target, source);
        return LinkAndContinue(groupBlock, nextFlowNode);
    }

    /// <inheritdoc/>
    public IDataflowPipelineBuilder<THead, TTail[]> AddCollectAllStep(PipelineStepOptions options = default) => AddGroupWhileStep((_, _) => true, options);

    /// <inheritdoc/>
    public IDataflowPipelineBuilder<THead, TNextOut> AddStep<TNextOut>(ISyncPipelineStep<TTail, TNextOut> step, PipelineStepOptions options = default) => AddTransformStep(
        step,
        tracker => PipelineStepExecutor.HandleExecution<TTail, TNextOut>(logger, progressNotifier, timeProvider, tracker, step.Execute, pipelineCancellationToken),
        tracker => PipelineStepExecutor.HandleSafeExecution<TTail, TNextOut>(logger, progressNotifier, timeProvider, tracker, step.Execute, pipelineCancellationToken),
        options);

    /// <inheritdoc/>
    public IDataflowPipelineBuilder<THead, TNextOut> AddStep<TNextOut>(IAsyncPipelineStep<TTail, TNextOut> step, PipelineStepOptions options = default) => AddTransformStep(
        step,
        tracker => PipelineStepExecutor.HandleExecution<TTail, TNextOut>(logger, progressNotifier, timeProvider, tracker, step.ExecuteAsync, retryOptions, pipelineCancellationToken),
        tracker => PipelineStepExecutor.HandleSafeExecution<TTail, TNextOut>(logger, progressNotifier, timeProvider, tracker, step.ExecuteAsync, retryOptions, pipelineCancellationToken),
        options);

    /// <inheritdoc/>
    public IDataflowPipelineBuilder<THead, TNextOut> AddStep<TNextOut>(IAsyncEnumerablePipelineStep<TTail, TNextOut> step, PipelineStepOptions options = default) => AddTransformManyStep(
        step,
        tracker => PipelineStepExecutor.HandleExecution<TTail, TNextOut>(logger, progressNotifier, timeProvider, tracker, step.ExecuteAsync, pipelineCancellationToken),
        tracker => PipelineStepExecutor.HandleSafeExecution<TTail, TNextOut>(logger, progressNotifier, timeProvider, tracker, step.ExecuteAsync, pipelineCancellationToken),
        options);

    /// <inheritdoc/>
    public IDataflowPipelineBuilder<THead, TNextOut> AddStep<TNextOut>(IEnumerablePipelineStep<TTail, TNextOut> step, PipelineStepOptions options = default) => AddTransformManyStep(
        step,
        tracker => PipelineStepExecutor.HandleExecution<TTail, TNextOut>(logger, progressNotifier, timeProvider, tracker, step.Execute, pipelineCancellationToken),
        tracker => PipelineStepExecutor.HandleSafeExecution<TTail, TNextOut>(logger, progressNotifier, timeProvider, tracker, step.Execute, pipelineCancellationToken),
        options);

    /// <inheritdoc/>
    public IDataflowPipelineBuilder<THead, TNextOut> AddForkingStep<TNextOut>(ISyncPipelineStep<TTail, TNextOut> step, Func<TNextOut, bool> fallbackCondition, ISyncPipelineStep<TTail, TNextOut> fallbackStep, PipelineStepOptions options = default) => AddForkingTransformStep(step,
        fallbackStep,
        (primary, fallback) => PipelineStepExecutor.HandleExecution<TTail, TNextOut>(logger, progressNotifier, timeProvider, primary, fallback, step.Execute, fallbackCondition, fallbackStep.Execute, pipelineCancellationToken),
        (primary, fallback) => PipelineStepExecutor.HandleSafeExecution<TTail, TNextOut>(
            logger,
            progressNotifier,
            timeProvider,
            primary,
            fallback,
            step.Execute,
            safeResult => safeResult.Success && safeResult.Output is not null && fallbackCondition(safeResult.Output),
            fallbackStep.Execute,
            pipelineCancellationToken),
        options);

    /// <inheritdoc/>
    public IDataflowPipelineBuilder<THead, TNextOut> AddForkingStep<TNextOut>(IAsyncPipelineStep<TTail, TNextOut> step, Func<TNextOut, bool> fallbackCondition, IAsyncPipelineStep<TTail, TNextOut> fallbackStep, PipelineStepOptions options = default) => AddForkingTransformStep(
        step,
        fallbackStep,
        (primary, fallback) => PipelineStepExecutor.HandleExecution<TTail, TNextOut>(logger, progressNotifier, timeProvider, primary, fallback, step.ExecuteAsync, fallbackCondition, fallbackStep.ExecuteAsync, retryOptions, pipelineCancellationToken),
        (primary, fallback) => PipelineStepExecutor.HandleSafeExecution<TTail, TNextOut>(
            logger,
            progressNotifier,
            timeProvider,
            primary,
            fallback,
            step.ExecuteAsync,
            safeResult => safeResult.Success && safeResult.Output is not null && fallbackCondition(safeResult.Output),
            fallbackStep.ExecuteAsync,
            retryOptions,
            pipelineCancellationToken),
        options);

    /// <inheritdoc/>
    public IDataflowPipelineBuilder<THead, TTail> AddBroadcastStep(IAsyncPipelineStep<TTail> step, Func<TTail, TTail>? cloneFunc = null, PipelineStepOptions options = default) => AddBroadcastCore(step, step.ExecuteAsync, cloneFunc, options);

    /// <inheritdoc/>
    public IDataflowPipelineBuilder<THead, TTail> AddBroadcastStep<TOut>(IAsyncPipelineStep<TOut> step, Func<TTail, TOut> mapFunc, PipelineStepOptions options = default) => AddBroadcastCore(step, (item, ct) => step.ExecuteAsync(mapFunc(item), ct), null, options);

    /// <inheritdoc/>
    public IDataflowPipelineBuilder<THead, TTail> AddBroadcastFork<TBranchOut>(
        Func<IDataflowPipelineBuilder<THead, TTail>, IDataflowPipelineBuilder<THead, TBranchOut>> branch,
        Func<TTail, TTail>? cloneFunc = null,
        PipelineStepOptions options = default)
    {
        ExecutionDataflowBlockOptions dataFlowOptions = options.ToDataflowOptions(pipelineCancellationToken);
        BroadcastBlock<TTail> broadcastBlock = new(cloneFunc, dataFlowOptions);
        tailBlock.LinkTo(broadcastBlock);

        DataflowPipelineBuilder<THead, TBranchOut> branchBuilder = (DataflowPipelineBuilder<THead, TBranchOut>)branch(CreateBranchBuilder(broadcastBlock));
        ActionBlock<TBranchOut> drainBlock = new(_ => { }, dataFlowOptions);
        branchBuilder.TailBlock.LinkTo(drainBlock);

        List<Task> updatedTasks = [.. branchBuilder._branchCompletionTasks, drainBlock.Completion];
        return new DataflowPipelineBuilder<THead, TTail>(
            logger,
            progressNotifier,
            headBlock,
            broadcastBlock,
            deadLetterQueueBlock,
            retryOptions,
            rootFlowNode,
            currentFlowNode,
            timeProvider,
            notificationSettings,
            _stepTrackers,
            updatedTasks,
            _steps,
            pipelineCancellationToken);
    }

    /// <inheritdoc/>
    public IDataflowPipeline<THead> BuildTerminal(string stepName, Action<TTail> terminalAction, PipelineStepOptions options = default)
    {
        PipelineStepTracker tracker = CreateAndRegisterTracker(stepName);
        (ITargetBlock<TTail> consumerBlock, Task completionTask) = CreateConsumer<TTail>(
            tracker,
            (item, _) =>
            {
                terminalAction(item);
                return Task.CompletedTask;
            },
            options.ToDataflowOptions(pipelineCancellationToken),
            stepRetryOptions: null);
        return BuildTerminalFromConsumer(consumerBlock, completionTask);
    }

    /// <inheritdoc/>
    public IDataflowPipeline<THead> BuildTerminal(string stepName, Func<TTail, CancellationToken, Task> terminalAction, PipelineStepOptions options = default)
    {
        PipelineStepTracker tracker = CreateAndRegisterTracker(stepName);
        (ITargetBlock<TTail> consumerBlock, Task completionTask) = CreateConsumer<TTail>(
            tracker,
            terminalAction,
            options.ToDataflowOptions(pipelineCancellationToken),
            retryOptions);
        return BuildTerminalFromConsumer(consumerBlock, completionTask);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Creates an untracked drain block when no custom terminal action is provided so internal drain plumbing does not pollute user-facing DAG topology events.
    /// </remarks>
    public IDataflowPipeline<THead> BuildTerminal(string stepName = "Terminal", PipelineStepOptions options = default)
    {
        ActionBlock<TTail> drainBlock = new(_ => { }, options.ToDataflowOptions(pipelineCancellationToken));
        return BuildTerminalFromConsumer(drainBlock, drainBlock.Completion);
    }

    private DataflowPipelineBuilder<THead, TTail> CreateBranchBuilder(ISourceBlock<TTail> broadcastBlock) => new(
        logger,
        progressNotifier,
        headBlock,
        broadcastBlock,
        deadLetterQueueBlock,
        retryOptions,
        rootFlowNode,
        currentFlowNode,
        timeProvider,
        notificationSettings,
        _stepTrackers,
        _branchCompletionTasks,
        _steps,
        pipelineCancellationToken);

    private IDataflowPipelineBuilder<THead, TNextOut> AddTransformStep<TNextOut>(
        IPipelineStep step,
        Func<PipelineStepTracker, Func<TTail, Task<TNextOut>>> executionFactory,
        Func<PipelineStepTracker, Func<TTail, Task<SafeResult<TTail, TNextOut>>>> safeExecutionFactory,
        PipelineStepOptions options)
    {
        _steps.Add(step);
        PipelineStepTracker tracker = CreateAndRegisterTracker(step.Name);
        ExecutionDataflowBlockOptions dataFlowOptions = options.ToDataflowOptions(pipelineCancellationToken);

        if (_deadLetterQueueEnabled)
        {
            TransformBlock<TTail, SafeResult<TTail, TNextOut>> safeBlock = new(safeExecutionFactory(tracker), dataFlowOptions);
            return AddSafeStep(safeBlock, dataFlowOptions, tracker.OutputNode, AttachStepCompletionAsync(safeBlock.Completion, tracker));
        }

        TransformBlock<TTail, TNextOut> nextBlock = new(executionFactory(tracker), dataFlowOptions);
        return LinkAndContinue(nextBlock, tracker.OutputNode, AttachStepCompletionAsync(nextBlock.Completion, tracker));
    }

    private IDataflowPipelineBuilder<THead, TNextOut> AddTransformManyStep<TNextOut>(
        IPipelineStep step,
        Func<PipelineStepTracker, Func<TTail, IAsyncEnumerable<TNextOut>>> executionFactory,
        Func<PipelineStepTracker, Func<TTail, IAsyncEnumerable<SafeResult<TTail, TNextOut>>>> safeExecutionFactory,
        PipelineStepOptions options)
    {
        _steps.Add(step);
        PipelineStepTracker tracker = CreateAndRegisterTracker(step.Name);
        ExecutionDataflowBlockOptions dataFlowOptions = options.ToDataflowOptions(pipelineCancellationToken);

        if (_deadLetterQueueEnabled)
        {
            TransformManyBlock<TTail, SafeResult<TTail, TNextOut>> safeBlock = new(safeExecutionFactory(tracker), dataFlowOptions);
            return AddSafeStep(safeBlock, dataFlowOptions, tracker.OutputNode, AttachStepCompletionAsync(safeBlock.Completion, tracker));
        }

        TransformManyBlock<TTail, TNextOut> nextBlock = new(executionFactory(tracker), dataFlowOptions);
        return LinkAndContinue(nextBlock, tracker.OutputNode, AttachStepCompletionAsync(nextBlock.Completion, tracker));
    }

    private IDataflowPipelineBuilder<THead, TNextOut> AddForkingTransformStep<TNextOut>(
        IPipelineStep primaryStep,
        IPipelineStep fallbackStep,
        Func<PipelineStepTracker, PipelineStepTracker, Func<TTail, Task<TNextOut>>> executionFactory,
        Func<PipelineStepTracker, PipelineStepTracker, Func<TTail, Task<SafeResult<TTail, TNextOut>>>> safeExecutionFactory,
        PipelineStepOptions options)
    {
        _steps.Add(primaryStep);
        _steps.Add(fallbackStep);
        (PipelineStepTracker primaryTracker, PipelineStepTracker fallbackTracker, PipelineFlowNode mergedOutputNode) = CreateForkingTrackers(primaryStep.Name, fallbackStep.Name);
        ExecutionDataflowBlockOptions dataFlowOptions = options.ToDataflowOptions(pipelineCancellationToken);

        if (_deadLetterQueueEnabled)
        {
            TransformBlock<TTail, SafeResult<TTail, TNextOut>> safeBlock = new(safeExecutionFactory(primaryTracker, fallbackTracker), dataFlowOptions);
            return AddSafeStep(safeBlock, dataFlowOptions, mergedOutputNode, AttachForkingStepCompletionAsync(safeBlock.Completion, primaryTracker, fallbackTracker));
        }

        TransformBlock<TTail, TNextOut> nextBlock = new(executionFactory(primaryTracker, fallbackTracker), dataFlowOptions);
        return LinkAndContinue(nextBlock, mergedOutputNode, AttachForkingStepCompletionAsync(nextBlock.Completion, primaryTracker, fallbackTracker));
    }

    private IDataflowPipelineBuilder<THead, TTail> AddBroadcastCore(
        IPipelineStep step,
        Func<TTail, CancellationToken, Task> consumerAction,
        Func<TTail, TTail>? cloneFunc,
        PipelineStepOptions options)
    {
        _steps.Add(step);
        PipelineStepTracker tracker = CreateAndRegisterTracker(step.Name);
        ExecutionDataflowBlockOptions dataFlowOptions = options.ToDataflowOptions(pipelineCancellationToken);

        BroadcastBlock<TTail> broadcastBlock = new(cloneFunc, dataFlowOptions);
        tailBlock.LinkTo(broadcastBlock);

        (ITargetBlock<TTail> targetBlock, Task completionTask) = CreateConsumer<TTail>(tracker, consumerAction, dataFlowOptions, retryOptions);
        broadcastBlock.LinkTo(targetBlock);

        List<Task> updatedTasks = [.. _branchCompletionTasks, completionTask];
        return new DataflowPipelineBuilder<THead, TTail>(
            logger,
            progressNotifier,
            headBlock,
            broadcastBlock,
            deadLetterQueueBlock,
            retryOptions,
            rootFlowNode,
            currentFlowNode,
            timeProvider,
            notificationSettings,
            _stepTrackers,
            updatedTasks,
            _steps,
            pipelineCancellationToken);
    }

    private (ITargetBlock<TInput> TargetBlock, Task CompletionTask) CreateConsumer<TInput>(
        PipelineStepTracker tracker,
        Func<TInput, CancellationToken, Task> action,
        ExecutionDataflowBlockOptions dataFlowOptions,
        RetryOptions? stepRetryOptions)
    {
        if (_deadLetterQueueEnabled)
        {
            TransformBlock<TInput, SafeResult<TInput>> safeBlock = new(
                PipelineStepExecutor.HandleSafeExecution(logger, progressNotifier, timeProvider, tracker, action, stepRetryOptions, pipelineCancellationToken),
                dataFlowOptions);
            (ITargetBlock<TInput> target, Task dlqCompletion) = LinkDeadLetterQueue(safeBlock, dataFlowOptions);
            return (target, AttachStepCompletionAsync(dlqCompletion, tracker));
        }

        ActionBlock<TInput> terminalBlock = new(
            PipelineStepExecutor.HandleExecution(logger, progressNotifier, timeProvider, tracker, action, stepRetryOptions, pipelineCancellationToken),
            dataFlowOptions);
        return (terminalBlock, AttachStepCompletionAsync(terminalBlock.Completion, tracker));
    }

    private (ITargetBlock<TInput> TargetBlock, Task CompletionTask) LinkDeadLetterQueue<TInput>(TransformBlock<TInput, SafeResult<TInput>> safeBlock, ExecutionDataflowBlockOptions dataFlowOptions)
    {
        ActionBlock<SafeResult<TInput>> failedBlock = new(async safeResult => await deadLetterQueueBlock!.SendAsync(safeResult.FailedPayload, pipelineCancellationToken).ConfigureAwait(false), dataFlowOptions);
        safeBlock.LinkTo(failedBlock, safeResult => !safeResult.Success);
        safeBlock.LinkTo(DataflowBlock.NullTarget<SafeResult<TInput>>());

        return (safeBlock, Task.WhenAll(safeBlock.Completion, failedBlock.Completion));
    }

    private DataflowPipelineBuilder<THead, TTail> WithDeadLetterQueue(ITargetBlock<FailedPayload> deadLetterQueueSink) => new DataflowPipelineBuilder<THead, TTail>(
        logger,
        progressNotifier,
        headBlock,
        tailBlock,
        deadLetterQueueSink,
        retryOptions,
        rootFlowNode,
        currentFlowNode,
        timeProvider,
        notificationSettings,
        _stepTrackers,
        _branchCompletionTasks,
        _steps,
        pipelineCancellationToken);

    private IDataflowPipeline<THead> BuildTerminalFromConsumer(ITargetBlock<TTail> consumerBlock, Task completionTask)
    {
        tailBlock.LinkTo(consumerBlock);
        Task finalCompletionTask = _branchCompletionTasks.Count > 0 ? Task.WhenAll([completionTask, .. _branchCompletionTasks]) : completionTask;
        return new DataflowPipeline<THead>(logger, headBlock, finalCompletionTask, _steps, rootFlowNode, _stepTrackers);
    }

    private DataflowPipelineBuilder<THead, TNextOut> AddSafeStep<TNextOut>(
        IPropagatorBlock<TTail, SafeResult<TTail, TNextOut>> safeBlock,
        ExecutionDataflowBlockOptions dataFlowOptions,
        PipelineFlowNode nextFlowNode,
        Task? stepCompletionTask = null)
    {
        tailBlock.LinkTo(safeBlock);

        TransformBlock<SafeResult<TTail, TNextOut>, TNextOut> successBlock = new(safeResult => safeResult.Output!, dataFlowOptions);
        safeBlock.LinkTo(successBlock, safeResult => safeResult.Success);

        ActionBlock<SafeResult<TTail, TNextOut>> failedBlock = new(async safeResult => await deadLetterQueueBlock!.SendAsync(safeResult.FailedPayload, pipelineCancellationToken).ConfigureAwait(false), dataFlowOptions);
        safeBlock.LinkTo(failedBlock, safeResult => !safeResult.Success);

        List<Task> updatedTasks = stepCompletionTask is not null
            ? [.. _branchCompletionTasks, failedBlock.Completion, stepCompletionTask]
            : [.. _branchCompletionTasks, failedBlock.Completion];

        return new DataflowPipelineBuilder<THead, TNextOut>(
            logger,
            progressNotifier,
            headBlock,
            successBlock,
            deadLetterQueueBlock,
            retryOptions,
            rootFlowNode,
            nextFlowNode,
            timeProvider,
            notificationSettings,
            _stepTrackers,
            updatedTasks,
            _steps,
            pipelineCancellationToken);
    }

    private DataflowPipelineBuilder<THead, TNextOut> LinkAndContinue<TNextOut>(
        IPropagatorBlock<TTail, TNextOut> nextBlock,
        PipelineFlowNode nextFlowNode,
        Task? stepCompletionTask = null)
    {
        tailBlock.LinkTo(nextBlock);
        List<Task> updatedTasks = stepCompletionTask is not null
            ? [.. _branchCompletionTasks, stepCompletionTask]
            : _branchCompletionTasks;

        return new DataflowPipelineBuilder<THead, TNextOut>(
            logger,
            progressNotifier,
            headBlock,
            nextBlock,
            deadLetterQueueBlock,
            retryOptions,
            rootFlowNode,
            nextFlowNode,
            timeProvider,
            notificationSettings,
            _stepTrackers,
            updatedTasks,
            _steps,
            pipelineCancellationToken);
    }

    private PipelineStepTracker CreateAndRegisterTracker(string stepName)
    {
        PipelineStepTracker tracker = new(logger, progressNotifier, timeProvider, notificationSettings, stepName, currentFlowNode.UpstreamStepNames);
        currentFlowNode.Subscribe(tracker.OnItemReceivedAsync);
        _stepTrackers.Add(tracker);
        return tracker;
    }

    private (PipelineStepTracker PrimaryTracker, PipelineStepTracker FallbackTracker, PipelineFlowNode MergedOutputNode) CreateForkingTrackers(string primaryStepName, string fallbackStepName)
    {
        PipelineStepTracker primaryTracker = CreateAndRegisterTracker(primaryStepName);
        PipelineStepTracker fallbackTracker = new(logger, progressNotifier, timeProvider, notificationSettings, fallbackStepName, [primaryStepName]);
        _stepTrackers.Add(fallbackTracker);

        PipelineFlowNode mergedOutputNode = new([primaryStepName, fallbackStepName]);
        primaryTracker.OutputNode.Subscribe(mergedOutputNode.NotifyItemEmittedAsync);
        fallbackTracker.OutputNode.Subscribe(mergedOutputNode.NotifyItemEmittedAsync);

        return (primaryTracker, fallbackTracker, mergedOutputNode);
    }

    private async Task AttachStepCompletionAsync(Task blockCompletion, PipelineStepTracker tracker)
    {
        await blockCompletion.ConfigureAwait(false);
        await tracker.OnBlockCompletedAsync(pipelineCancellationToken).ConfigureAwait(false);
    }

    private async Task AttachForkingStepCompletionAsync(Task blockCompletion, PipelineStepTracker primaryTracker, PipelineStepTracker fallbackTracker)
    {
        await blockCompletion.ConfigureAwait(false);
        await primaryTracker.OnBlockCompletedAsync(pipelineCancellationToken).ConfigureAwait(false);
        await fallbackTracker.OnBlockCompletedAsync(pipelineCancellationToken).ConfigureAwait(false);
    }

    private List<Task> MergeBranchCompletionTasks(IReadOnlyList<List<Task>> branchTaskLists)
    {
        List<Task> merged = [.. _branchCompletionTasks];
        for (int i = 0; i < branchTaskLists.Count; i++)
        {
            List<Task> branchTasks = branchTaskLists[i];
            for (int j = 0; j < branchTasks.Count; j++)
            {
                Task task = branchTasks[j];
                if (!merged.Contains(task))
                    merged.Add(task);
            }
        }

        return merged;
    }
}

/// <summary>
/// Sealed execution engine representing the finalized graph.
/// Exposes inputs, triggers completion, and awaits termination while automatically disposing registered steps on shutdown.
/// </summary>
file sealed class DataflowPipeline<TIn>(
    ILogger logger,
    ITargetBlock<TIn> headBlock,
    Task completionTask,
    IReadOnlyCollection<IPipelineStep> steps,
    PipelineFlowNode rootFlowNode,
    IReadOnlyList<PipelineStepTracker> stepTrackers)
    : IDataflowPipeline<TIn>
{
    private int _topologyInitialized;

    /// <inheritdoc/>
    public async Task<bool> SendAsync(TIn item, CancellationToken cancellationToken = default)
    {
        if (Interlocked.CompareExchange(ref _topologyInitialized, 1, 0) == 0)
        {
            for (int i = 0; i < stepTrackers.Count; i++)
            {
                await stepTrackers[i].NotifyPendingAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        await rootFlowNode.NotifyItemEmittedAsync(cancellationToken).ConfigureAwait(false);
        return await headBlock.SendAsync(item, cancellationToken).ConfigureAwait(false);
    }

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
