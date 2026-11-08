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
/// <item><description><b>Bifurcated DLQ Routing:</b> When a Dead-Letter Queue is configured, steps dynamically wrap execution in <see cref="SafeResult{TIn, TOut}"/> and split successful vs. faulted payloads without faulting the TPL Dataflow graph.</description></item>
/// <item><description><b>Topology Tracking:</b> Collects ordered <c>(PreviousStepNames, CurrentStep)</c> node pairs. Main-flow operators track <c>currentStepNames</c>, while side-effect observers (<c>AddBroadcastStep</c>) preserve the upstream predecessors for subsequent steps.</description></item>
/// <item><description><b>Unified Observability:</b> All pipeline operations route through <see cref="ObservabilityExtensions"/> to capture distributed tracing, structured logging, and non-blocking real-time progress notifications.</description></item>
/// </list>
/// </remarks>
internal class DataflowPipelineBuilder<THead, TTail>(
    ILogger logger,
    IPipelineProgressNotifier progressNotifier,
    ITargetBlock<THead> headBlock,
    ISourceBlock<TTail> tailBlock,
    ITargetBlock<FailedPayload>? deadLetterQueueBlock,
    TimeProvider timeProvider,
    string pipelineIdentifier,
    List<Task>? branchCompletionTasks = null,
    List<(string[] PreviousStepNames, IPipelineStep CurrentStep)>? nodes = null,
    string[]? currentStepNames = null,
    CancellationToken pipelineCancellationToken = default)
    : IDataflowPipelineBuilder<THead, TTail>
{
    private const string DeadLetterQueueTelemetryName = "Pipeline.DeadLetterQueue";
    private const string DataMappingTelemetryName = "Pipeline.DataMapping";
    private readonly bool _deadLetterQueueEnabled = deadLetterQueueBlock is not null;
    private readonly List<Task> _branchCompletionTasks = branchCompletionTasks ?? [];
    private readonly List<(string[] PreviousStepNames, IPipelineStep CurrentStep)> _nodes = nodes ?? [];

    internal ISourceBlock<TTail> TailBlock => tailBlock;
    internal string[] CurrentStepNames { get; } = currentStepNames ?? [];

    /// <inheritdoc/>
    public IDataflowPipelineBuilder<THead, TTail> WithDeadLetterQueue(Action<FailedPayload> errorHandler, PipelineStepOptions options = default)
        => WithDeadLetterQueue(new ActionBlock<FailedPayload>(
            ObservabilityExtensions.HandleExecution(logger, progressNotifier, timeProvider, pipelineIdentifier, DeadLetterQueueTelemetryName, errorHandler, pipelineCancellationToken),
            options.ToDataflowOptions(pipelineCancellationToken)));

    /// <inheritdoc/>
    public IDataflowPipelineBuilder<THead, TTail> WithDeadLetterQueue(Func<FailedPayload, CancellationToken, Task> errorHandler, PipelineStepOptions options = default)
        => WithDeadLetterQueue(new ActionBlock<FailedPayload>(
            ObservabilityExtensions.HandleExecution(logger, progressNotifier, timeProvider, pipelineIdentifier, DeadLetterQueueTelemetryName, errorHandler, pipelineCancellationToken),
            options.ToDataflowOptions(pipelineCancellationToken)));

    /// <inheritdoc/>
    public IDataflowPipelineBuilder<THead, TTail> WithLoggingErrors(PipelineStepOptions options = default)
        => WithDeadLetterQueue(new ActionBlock<FailedPayload>(
            ObservabilityExtensions.HandleExecution<FailedPayload>(
                logger,
                progressNotifier,
                timeProvider,
                pipelineIdentifier,
                DeadLetterQueueTelemetryName,
                failedPayload => logger.LogError("Failed to execute step. Failed payload: {@FailedPayload}", failedPayload),
                pipelineCancellationToken),
            options.ToDataflowOptions(pipelineCancellationToken)));

    /// <inheritdoc/>
    public IDataflowPipelineBuilder<THead, TNextOut> AddDataMapping<TNextOut>(Func<TTail, TNextOut> mapFunc, PipelineStepOptions options = default)
    {
        ExecutionDataflowBlockOptions dataFlowOptions = options.ToDataflowOptions(pipelineCancellationToken);

        return _deadLetterQueueEnabled
            ? AddSafeStep(new TransformBlock<TTail, SafeResult<TTail, TNextOut>>(
                ObservabilityExtensions.HandleSafeExecution(logger, progressNotifier, timeProvider, pipelineIdentifier, DataMappingTelemetryName, mapFunc, pipelineCancellationToken),
                dataFlowOptions), dataFlowOptions, CurrentStepNames)
            : LinkAndContinue(new TransformBlock<TTail, TNextOut>(
                ObservabilityExtensions.HandleExecution(logger, progressNotifier, timeProvider, pipelineIdentifier, DataMappingTelemetryName, mapFunc, pipelineCancellationToken),
                dataFlowOptions), CurrentStepNames);
    }

    /// <inheritdoc/>
    public IDataflowPipelineBuilder<THead, TTail> AddFilterStep(Predicate<TTail> predicate, PipelineStepOptions options = default)
    {
        ExecutionDataflowBlockOptions dataFlowOptions = options.ToDataflowOptions(pipelineCancellationToken);
        TransformBlock<TTail, TTail> filterBlock = new(
            ObservabilityExtensions.HandleExecution<TTail, TTail>(logger, progressNotifier, timeProvider, pipelineIdentifier, "Pipeline.Filter", item => item, pipelineCancellationToken),
            dataFlowOptions);

        tailBlock.LinkTo(filterBlock, predicate);
        tailBlock.LinkTo(DataflowBlock.NullTarget<TTail>());

        return ContinueWith(filterBlock, CurrentStepNames);
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

        List<Task> mergedTasks = MergeBranchCompletionTasks(resolvedBranch1._branchCompletionTasks, resolvedBranch2._branchCompletionTasks);
        string[] joinedStepNames = MergeStepNames(resolvedBranch1.CurrentStepNames, resolvedBranch2.CurrentStepNames);

        return ContinueWith(joinBlock, joinedStepNames, mergedTasks);
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

        List<Task> mergedTasks = MergeBranchCompletionTasks(resolvedBranch1._branchCompletionTasks, resolvedBranch2._branchCompletionTasks, resolvedBranch3._branchCompletionTasks);
        string[] joinedStepNames = MergeStepNames(resolvedBranch1.CurrentStepNames, resolvedBranch2.CurrentStepNames, resolvedBranch3.CurrentStepNames);

        return ContinueWith(joinBlock, joinedStepNames, mergedTasks);
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
    public IDataflowPipelineBuilder<THead, IGrouping<TKey, TTail>> AddGroupWhileStep<TKey>(
        Func<TTail, TKey> keySelector,
        PipelineStepOptions options = default)
        => AddGroupWhileStep((previous, current) => EqualityComparer<TKey>.Default.Equals(keySelector(previous), keySelector(current)), keySelector, options);

    /// <inheritdoc/>
    public IDataflowPipelineBuilder<THead, IGrouping<TKey, TTail>> AddGroupWhileStep<TKey>(
        Func<TTail, TTail, bool> groupingCondition,
        Func<TTail, TKey> keySelector,
        PipelineStepOptions options = default)
    {
        ExecutionDataflowBlockOptions dataFlowOptions = options.ToDataflowOptions(pipelineCancellationToken);

        // Must be strictly sequential to accurately evaluate the condition against the previous item.
        if (dataFlowOptions.MaxDegreeOfParallelism > 1)
        {
            logger.LogWarning("Max workers cannot be greater than 1 for AddGroupWhileStep. Overriding to 1.");
            dataFlowOptions.MaxDegreeOfParallelism = 1;
        }

        List<TTail> buffer = new(128);
        TKey currentKey = default!;
        bool hasKey = false;
        BufferBlock<IGrouping<TKey, TTail>> source = new(dataFlowOptions);
        ActionBlock<TTail> target = new(
            ObservabilityExtensions.HandleExecution<TTail>(
                logger,
                progressNotifier,
                timeProvider,
                pipelineIdentifier,
                "Pipeline.GroupWhile",
                async (item, ct) =>
                {
                    if (buffer.Count > 0 && !groupingCondition(buffer[^1], item))
                    {
                        Grouping<TKey, TTail> group = new(currentKey, [.. buffer]);
                        await source.SendAsync(group, ct).ConfigureAwait(false);
                        buffer.Clear();
                        hasKey = false;
                    }

                    if (!hasKey)
                    {
                        currentKey = keySelector(item);
                        hasKey = true;
                    }

                    buffer.Add(item);
                },
                pipelineCancellationToken),
            dataFlowOptions);

        // Flush the final partial batch trapped in the buffer before propagating completion downstream.
        target.Completion.ContinueWith(source, async () =>
        {
            if (buffer.Count > 0)
            {
                Grouping<TKey, TTail> group = new(currentKey, [.. buffer]);
                await source.SendAsync(group, pipelineCancellationToken).ConfigureAwait(false);
                buffer.Clear();
            }
        });

        IPropagatorBlock<TTail, IGrouping<TKey, TTail>> groupBlock = DataflowBlock.Encapsulate(target, source);
        return LinkAndContinue(groupBlock, CurrentStepNames);
    }

    /// <inheritdoc/>
    public IDataflowPipelineBuilder<THead, TTail[]> AddCollectAllStep(PipelineStepOptions options = default)
    {
        ExecutionDataflowBlockOptions dataFlowOptions = options.ToDataflowOptions(pipelineCancellationToken);

        // Must be strictly sequential to accumulate all items into a single collection.
        if (dataFlowOptions.MaxDegreeOfParallelism > 1)
        {
            logger.LogWarning("Max workers cannot be greater than 1 for AddCollectAllStep. Overriding to 1.");
            dataFlowOptions.MaxDegreeOfParallelism = 1;
        }

        List<TTail> buffer = new(1024);
        BufferBlock<TTail[]> source = new(dataFlowOptions);
        ActionBlock<TTail> target = new(
            ObservabilityExtensions.HandleExecution<TTail>(
                logger,
                progressNotifier,
                timeProvider,
                pipelineIdentifier,
                "Pipeline.CollectAll",
                (item, _) =>
                {
                    buffer.Add(item);
                    return Task.CompletedTask;
                },
                pipelineCancellationToken),
            dataFlowOptions);

        target.Completion.ContinueWith(source, async () =>
        {
            if (buffer.Count > 0)
            {
                await source.SendAsync([.. buffer], pipelineCancellationToken).ConfigureAwait(false);
                buffer.Clear();
            }
        });

        IPropagatorBlock<TTail, TTail[]> collectBlock = DataflowBlock.Encapsulate(target, source);
        return LinkAndContinue(collectBlock, CurrentStepNames);
    }

    /// <inheritdoc/>
    public IDataflowPipelineBuilder<THead, TNextOut> AddStep<TNextOut>(ISyncPipelineStep<TTail, TNextOut> step, PipelineStepOptions options = default) => AddTransformStep(
        step,
        ObservabilityExtensions.HandleExecution<TTail, TNextOut>(logger, progressNotifier, timeProvider, pipelineIdentifier, step.Name, step.Execute, pipelineCancellationToken),
        ObservabilityExtensions.HandleSafeExecution<TTail, TNextOut>(logger, progressNotifier, timeProvider, pipelineIdentifier, step.Name, step.Execute, pipelineCancellationToken),
        options);

    /// <inheritdoc/>
    public IDataflowPipelineBuilder<THead, TNextOut> AddStep<TNextOut>(IAsyncPipelineStep<TTail, TNextOut> step, PipelineStepOptions options = default) => AddTransformStep(
        step,
        ObservabilityExtensions.HandleExecution<TTail, TNextOut>(logger, progressNotifier, timeProvider, pipelineIdentifier, step.Name, step.ExecuteAsync, pipelineCancellationToken),
        ObservabilityExtensions.HandleSafeExecution<TTail, TNextOut>(logger, progressNotifier, timeProvider, pipelineIdentifier, step.Name, step.ExecuteAsync, pipelineCancellationToken),
        options);

    /// <inheritdoc/>
    public IDataflowPipelineBuilder<THead, TNextOut> AddStep<TNextOut>(IAsyncEnumerablePipelineStep<TTail, TNextOut> step, PipelineStepOptions options = default) => AddTransformManyStep(
        step,
        ObservabilityExtensions.HandleExecution<TTail, TNextOut>(logger, progressNotifier, timeProvider, pipelineIdentifier, step.Name, step.ExecuteAsync, pipelineCancellationToken),
        ObservabilityExtensions.HandleSafeExecution<TTail, TNextOut>(logger, progressNotifier, timeProvider, pipelineIdentifier, step.Name, step.ExecuteAsync, pipelineCancellationToken),
        options);

    /// <inheritdoc/>
    public IDataflowPipelineBuilder<THead, TNextOut> AddStep<TNextOut>(IEnumerablePipelineStep<TTail, TNextOut> step, PipelineStepOptions options = default) => AddTransformManyStep(
        step,
        ObservabilityExtensions.HandleExecution<TTail, TNextOut>(logger, progressNotifier, timeProvider, pipelineIdentifier, step.Name, step.Execute, pipelineCancellationToken),
        ObservabilityExtensions.HandleSafeExecution<TTail, TNextOut>(logger, progressNotifier, timeProvider, pipelineIdentifier, step.Name, step.Execute, pipelineCancellationToken),
        options);

    /// <inheritdoc/>
    public IDataflowPipelineBuilder<THead, TNextOut> AddForkingStep<TNextOut>(ISyncPipelineStep<TTail, TNextOut> step, Func<TNextOut, bool> fallbackCondition, ISyncPipelineStep<TTail, TNextOut> fallbackStep, PipelineStepOptions options = default) => AddForkingTransformStep(
        step,
        fallbackStep,
        ObservabilityExtensions.HandleExecution<TTail, TNextOut>(logger, progressNotifier, timeProvider, pipelineIdentifier, step.Name, fallbackStep.Name, step.Execute, fallbackCondition, fallbackStep.Execute, pipelineCancellationToken),
        ObservabilityExtensions.HandleSafeExecution<TTail, TNextOut>(
            logger,
            progressNotifier,
            timeProvider,
            pipelineIdentifier,
            step.Name,
            fallbackStep.Name,
            step.Execute,
            safeResult => safeResult.Success && safeResult.Output is not null && fallbackCondition(safeResult.Output),
            fallbackStep.Execute,
            pipelineCancellationToken),
        options);

    /// <inheritdoc/>
    public IDataflowPipelineBuilder<THead, TNextOut> AddForkingStep<TNextOut>(IAsyncPipelineStep<TTail, TNextOut> step, Func<TNextOut, bool> fallbackCondition, IAsyncPipelineStep<TTail, TNextOut> fallbackStep, PipelineStepOptions options = default) => AddForkingTransformStep(
        step,
        fallbackStep,
        ObservabilityExtensions.HandleExecution<TTail, TNextOut>(logger, progressNotifier, timeProvider, pipelineIdentifier, step.Name, fallbackStep.Name, step.ExecuteAsync, fallbackCondition, fallbackStep.ExecuteAsync, pipelineCancellationToken),
        ObservabilityExtensions.HandleSafeExecution<TTail, TNextOut>(
            logger,
            progressNotifier,
            timeProvider,
            pipelineIdentifier,
            step.Name,
            fallbackStep.Name,
            step.ExecuteAsync,
            safeResult => safeResult.Success && safeResult.Output is not null && fallbackCondition(safeResult.Output),
            fallbackStep.ExecuteAsync,
            pipelineCancellationToken),
        options);

    /// <inheritdoc/>
    public IDataflowPipelineBuilder<THead, TTail> AddBroadcastStep(IAsyncPipelineStep<TTail> step, Func<TTail, TTail>? cloneFunc = null, PipelineStepOptions options = default)
        => AddBroadcastCore(step, step.ExecuteAsync, cloneFunc, options);

    /// <inheritdoc/>
    public IDataflowPipelineBuilder<THead, TTail> AddBroadcastStep<TOut>(IAsyncPipelineStep<TOut> step, Func<TTail, TOut> mapFunc, PipelineStepOptions options = default)
        => AddBroadcastCore(step, (item, ct) => step.ExecuteAsync(mapFunc(item), ct), null, options);

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

        return ContinueWith(broadcastBlock, CurrentStepNames, [.. branchBuilder._branchCompletionTasks, drainBlock.Completion]);
    }

    /// <inheritdoc/>
    public IDataflowPipeline<THead> BuildTerminal(string stepName, Action<TTail> terminalAction, PipelineStepOptions options = default)
        => BuildTerminal(stepName, (item, _) =>
        {
            terminalAction(item);
            return Task.CompletedTask;
        }, options);

    /// <inheritdoc/>
    public IDataflowPipeline<THead> BuildTerminal(string stepName, Func<TTail, CancellationToken, Task> terminalAction, PipelineStepOptions options = default)
    {
        (ITargetBlock<TTail> consumerBlock, Task completionTask) = CreateConsumer<TTail>(
            stepName,
            terminalAction,
            options.ToDataflowOptions(pipelineCancellationToken));
        return BuildTerminalFromConsumer(consumerBlock, completionTask);
    }

    /// <inheritdoc/>
    public IDataflowPipeline<THead> BuildTerminal(string stepName = "Terminal", PipelineStepOptions options = default) => BuildTerminal(stepName, (_, _) => Task.CompletedTask, options);

    private DataflowPipelineBuilder<THead, TTail> CreateBranchBuilder(ISourceBlock<TTail> broadcastBlock)
        => ContinueWith(broadcastBlock, CurrentStepNames);

    private IDataflowPipelineBuilder<THead, TNextOut> AddTransformStep<TNextOut>(
        IPipelineStep step,
        Func<TTail, Task<TNextOut>> executionDelegate,
        Func<TTail, Task<SafeResult<TTail, TNextOut>>> safeExecutionDelegate,
        PipelineStepOptions options)
    {
        _nodes.Add((CurrentStepNames, step));
        ExecutionDataflowBlockOptions dataFlowOptions = options.ToDataflowOptions(pipelineCancellationToken);

        return _deadLetterQueueEnabled
            ? AddSafeStep(new TransformBlock<TTail, SafeResult<TTail, TNextOut>>(safeExecutionDelegate, dataFlowOptions), dataFlowOptions, [step.Name])
            : LinkAndContinue(new TransformBlock<TTail, TNextOut>(executionDelegate, dataFlowOptions), [step.Name]);
    }

    private IDataflowPipelineBuilder<THead, TNextOut> AddTransformManyStep<TNextOut>(
        IPipelineStep step,
        Func<TTail, IAsyncEnumerable<TNextOut>> executionDelegate,
        Func<TTail, IAsyncEnumerable<SafeResult<TTail, TNextOut>>> safeExecutionDelegate,
        PipelineStepOptions options)
    {
        _nodes.Add((CurrentStepNames, step));
        ExecutionDataflowBlockOptions dataFlowOptions = options.ToDataflowOptions(pipelineCancellationToken);

        return _deadLetterQueueEnabled
            ? AddSafeStep(new TransformManyBlock<TTail, SafeResult<TTail, TNextOut>>(safeExecutionDelegate, dataFlowOptions), dataFlowOptions, [step.Name])
            : LinkAndContinue(new TransformManyBlock<TTail, TNextOut>(executionDelegate, dataFlowOptions), [step.Name]);
    }

    private IDataflowPipelineBuilder<THead, TNextOut> AddForkingTransformStep<TNextOut>(
        IPipelineStep primaryStep,
        IPipelineStep fallbackStep,
        Func<TTail, Task<TNextOut>> executionDelegate,
        Func<TTail, Task<SafeResult<TTail, TNextOut>>> safeExecutionDelegate,
        PipelineStepOptions options)
    {
        _nodes.Add((CurrentStepNames, primaryStep));
        _nodes.Add((CurrentStepNames, fallbackStep));
        ExecutionDataflowBlockOptions dataFlowOptions = options.ToDataflowOptions(pipelineCancellationToken);
        string[] compoundStepNames = [primaryStep.Name, fallbackStep.Name];

        return _deadLetterQueueEnabled
            ? AddSafeStep(new TransformBlock<TTail, SafeResult<TTail, TNextOut>>(safeExecutionDelegate, dataFlowOptions), dataFlowOptions, compoundStepNames)
            : LinkAndContinue(new TransformBlock<TTail, TNextOut>(executionDelegate, dataFlowOptions), compoundStepNames);
    }

    private IDataflowPipelineBuilder<THead, TTail> AddBroadcastCore(
        IPipelineStep step,
        Func<TTail, CancellationToken, Task> consumerAction,
        Func<TTail, TTail>? cloneFunc,
        PipelineStepOptions options)
    {
        _nodes.Add((CurrentStepNames, step));
        ExecutionDataflowBlockOptions dataFlowOptions = options.ToDataflowOptions(pipelineCancellationToken);

        BroadcastBlock<TTail> broadcastBlock = new(cloneFunc, dataFlowOptions);
        tailBlock.LinkTo(broadcastBlock);

        (ITargetBlock<TTail> targetBlock, Task completionTask) = CreateConsumer<TTail>(step.Name, consumerAction, dataFlowOptions);
        broadcastBlock.LinkTo(targetBlock);

        return ContinueWith(broadcastBlock, CurrentStepNames, [.. _branchCompletionTasks, completionTask]);
    }

    private (ITargetBlock<TInput> TargetBlock, Task CompletionTask) CreateConsumer<TInput>(
        string stepName,
        Func<TInput, CancellationToken, Task> action,
        ExecutionDataflowBlockOptions dataFlowOptions)
    {
        if (_deadLetterQueueEnabled)
        {
            TransformBlock<TInput, SafeResult<TInput>> safeBlock = new(
                ObservabilityExtensions.HandleSafeExecution<TInput>(logger, progressNotifier, timeProvider, pipelineIdentifier, stepName, action, pipelineCancellationToken),
                dataFlowOptions);
            ActionBlock<SafeResult<TInput>> failedBlock = new(async safeResult => await deadLetterQueueBlock!.SendAsync(safeResult.FailedPayload, pipelineCancellationToken).ConfigureAwait(false), dataFlowOptions);
            safeBlock.LinkTo(failedBlock, safeResult => !safeResult.Success);
            safeBlock.LinkTo(DataflowBlock.NullTarget<SafeResult<TInput>>());
            return (safeBlock, Task.WhenAll(safeBlock.Completion, failedBlock.Completion));
        }

        ActionBlock<TInput> terminalBlock = new(
            ObservabilityExtensions.HandleExecution<TInput>(logger, progressNotifier, timeProvider, pipelineIdentifier, stepName, action, pipelineCancellationToken),
            dataFlowOptions);
        return (terminalBlock, terminalBlock.Completion);
    }

    private DataflowPipelineBuilder<THead, TTail> WithDeadLetterQueue(ITargetBlock<FailedPayload> deadLetterQueueSink) => new(
        logger,
        progressNotifier,
        headBlock,
        tailBlock,
        deadLetterQueueSink,
        timeProvider,
        pipelineIdentifier,
        _branchCompletionTasks,
        _nodes,
        CurrentStepNames,
        pipelineCancellationToken);

    private IDataflowPipeline<THead> BuildTerminalFromConsumer(ITargetBlock<TTail> consumerBlock, Task completionTask)
    {
        tailBlock.LinkTo(consumerBlock);
        Task finalCompletionTask = _branchCompletionTasks.Count > 0 ? Task.WhenAll([completionTask, .. _branchCompletionTasks]) : completionTask;
        return new DataflowPipeline<THead>(logger, progressNotifier, headBlock, timeProvider, pipelineIdentifier, finalCompletionTask, _nodes);
    }

    private DataflowPipelineBuilder<THead, TNextOut> AddSafeStep<TNextOut>(
        IPropagatorBlock<TTail, SafeResult<TTail, TNextOut>> safeBlock,
        ExecutionDataflowBlockOptions dataFlowOptions,
        string[] nextStepNames)
    {
        tailBlock.LinkTo(safeBlock);

        TransformBlock<SafeResult<TTail, TNextOut>, TNextOut> successBlock = new(safeResult => safeResult.Output!, dataFlowOptions);
        safeBlock.LinkTo(successBlock, safeResult => safeResult.Success);

        ActionBlock<SafeResult<TTail, TNextOut>> failedBlock = new(async safeResult => await deadLetterQueueBlock!.SendAsync(safeResult.FailedPayload, pipelineCancellationToken).ConfigureAwait(false), dataFlowOptions);
        safeBlock.LinkTo(failedBlock, safeResult => !safeResult.Success);

        return ContinueWith(successBlock, nextStepNames, [.. _branchCompletionTasks, failedBlock.Completion]);
    }

    private DataflowPipelineBuilder<THead, TNextOut> LinkAndContinue<TNextOut>(
        IPropagatorBlock<TTail, TNextOut> nextBlock,
        string[] nextStepNames)
    {
        tailBlock.LinkTo(nextBlock);
        return ContinueWith(nextBlock, nextStepNames);
    }

    private DataflowPipelineBuilder<THead, TNextOut> ContinueWith<TNextOut>(
        ISourceBlock<TNextOut> nextTailBlock,
        string[] nextStepNames,
        List<Task>? branchTasks = null) => new(
            logger,
            progressNotifier,
            headBlock,
            nextTailBlock,
            deadLetterQueueBlock,
            timeProvider,
            pipelineIdentifier,
            branchTasks ?? _branchCompletionTasks,
            _nodes,
            nextStepNames,
            pipelineCancellationToken);

    private static string[] MergeStepNames(params ReadOnlySpan<string[]> stepNameCollections)
    {
        List<string> distinct = [];
        foreach (string[] names in stepNameCollections)
        {
            foreach (string name in names)
            {
                if (!string.IsNullOrEmpty(name) && !distinct.Contains(name, StringComparer.Ordinal))
                    distinct.Add(name);
            }
        }

        return [.. distinct];
    }

    private List<Task> MergeBranchCompletionTasks(params ReadOnlySpan<List<Task>> branchTaskLists)
    {
        List<Task> merged = [.. _branchCompletionTasks];
        foreach (List<Task> branchTasks in branchTaskLists)
        {
            foreach (Task task in branchTasks)
            {
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
    IPipelineProgressNotifier progressNotifier,
    ITargetBlock<TIn> headBlock,
    TimeProvider timeProvider,
    string pipelineIdentifier,
    Task completionTask,
    IReadOnlyList<(string[] PreviousStepNames, IPipelineStep CurrentStep)> nodes)
    : IDataflowPipeline<TIn>
{
    private int _receivedItems;
    private int _processedItems;

    /// <inheritdoc/>
    public IReadOnlyList<(string[] UpstreamSteps, string StepName)> Topology => nodes.Select(node => (node.PreviousStepNames, node.CurrentStep.Name)).ToList();

    /// <inheritdoc/>
    public async Task<bool> SendAsync(TIn item, CancellationToken cancellationToken = default)
    {
        int received = Interlocked.Increment(ref _receivedItems);
        string? itemDetails = ObservabilityExtensions.SerializeDetails(new { item });

        await progressNotifier
            .NotifyAsync(new PipelineProgressEvent
            {
                PipelineIdentifier = pipelineIdentifier,
                StepName = $"{nameof(DataflowPipeline<>)}.{nameof(SendAsync)}",
                Description = "Started ingestion of input item into pipeline.",
                State = PipelineStepState.Started,
                Timestamp = timeProvider.GetUtcNow(),
                ReceivedItems = received,
                ProcessedItems = Volatile.Read(ref _processedItems),
                Details = itemDetails
            }, cancellationToken)
            .ConfigureAwait(false);

        bool accepted = await headBlock.SendAsync(item, cancellationToken).ConfigureAwait(false);
        if (!accepted)
        {
            await progressNotifier
                .NotifyAsync(new PipelineProgressEvent
                {
                    PipelineIdentifier = pipelineIdentifier,
                    StepName = $"{nameof(DataflowPipeline<>)}.{nameof(SendAsync)}",
                    Description = "Failed to ingest item into pipeline (head block rejected input).",
                    State = PipelineStepState.Faulted,
                    Timestamp = timeProvider.GetUtcNow(),
                    ReceivedItems = received,
                    ProcessedItems = Volatile.Read(ref _processedItems),
                    Details = itemDetails
                }, cancellationToken)
                .ConfigureAwait(false);

            logger.LogError("Failed to ingest item into the pipeline. Item: {@Item}", item);
            return false;
        }

        await progressNotifier
            .NotifyAsync(new PipelineProgressEvent
            {
                PipelineIdentifier = pipelineIdentifier,
                StepName = $"{nameof(DataflowPipeline<>)}.{nameof(SendAsync)}",
                Description = "Completed ingestion of input item into pipeline.",
                State = PipelineStepState.Completed,
                Timestamp = timeProvider.GetUtcNow(),
                ReceivedItems = received,
                ProcessedItems = Interlocked.Increment(ref _processedItems),
                Details = itemDetails
            }, cancellationToken)
            .ConfigureAwait(false);

        return accepted;
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
            HashSet<IPipelineStep> distinctSteps = [];
            foreach ((_, IPipelineStep currentStep) in nodes)
                distinctSteps.Add(currentStep);

            foreach (IPipelineStep step in distinctSteps)
            {
                if (step is IAsyncDisposable asyncDisposableStep)
                    await asyncDisposableStep.DisposeAsync().ConfigureAwait(false);
                else if (step is IDisposable disposableStep)
                    disposableStep.Dispose();
            }
        }
    }
}
