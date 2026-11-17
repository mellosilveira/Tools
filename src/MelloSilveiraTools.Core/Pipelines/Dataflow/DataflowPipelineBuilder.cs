using MelloSilveiraTools.Core.ExtensionMethods;
using MelloSilveiraTools.Core.Pipelines.Models;
using MelloSilveiraTools.Core.Pipelines.Steps;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks.Dataflow;

namespace MelloSilveiraTools.Core.Pipelines.Dataflow;

/// <summary>
/// Strongly-typed fluent builder for orchestrating asynchronous TPL Dataflow processing topologies.
/// </summary>
/// <remarks>
/// Architectural Decisions:
/// <list type="bullet">
/// <item><description><b>Bifurcated DLQ Routing:</b> When a Dead-Letter Queue is configured, steps dynamically wrap execution in <see cref="SafeResult{TIn, TOut}"/> and split successful vs. faulted payloads without faulting the TPL Dataflow graph.</description></item>
/// <item><description><b>Lossless Fan-Out Dispatch:</b> Fork and broadcast operators use <see cref="DataflowFanOut"/> instead of TPL Dataflow's native <see cref="BroadcastBlock{T}"/> to guarantee 100% delivery, backpressure, and ordered tuple pairing.</description></item>
/// <item><description><b>Topology Tracking:</b> Collects ordered <c>(PreviousStepNames, CurrentStep)</c> node pairs while preserving upstream predecessors across side-effect observers (<c>AddBroadcastStep</c>, <c>AddBroadcastFork</c>).</description></item>
/// </list>
/// </remarks>
internal sealed class DataflowPipelineBuilder<THead, TTail>(
    PipelineBuildContext<THead> context,
    ISourceBlock<TTail> tailBlock,
    List<Task>? branchCompletionTasks = null,
    string[]? currentStepNames = null)
    : IDataflowPipelineBuilder<THead, TTail>
{
    private const string DeadLetterQueueTelemetryName = "Pipeline.DeadLetterQueue";
    private const string DataMappingTelemetryName = "Pipeline.DataMapping";
    private readonly List<Task> _branchCompletionTasks = branchCompletionTasks ?? [];

    internal ISourceBlock<TTail> TailBlock => tailBlock;
    internal string[] CurrentStepNames { get; } = currentStepNames ?? [];

    /// <inheritdoc/>
    public IDataflowPipelineBuilder<THead, TTail> WithDeadLetterQueue(Action<FailedPayload> errorHandler, PipelineStepOptions options = default)
        => WithDeadLetterQueue(new ActionBlock<FailedPayload>(
            context.WrapExecution(DeadLetterQueueTelemetryName, errorHandler),
            options.ToDataflowOptions(context.CancellationToken)));

    /// <inheritdoc/>
    public IDataflowPipelineBuilder<THead, TTail> WithDeadLetterQueue(Func<FailedPayload, CancellationToken, Task> errorHandler, PipelineStepOptions options = default)
        => WithDeadLetterQueue(new ActionBlock<FailedPayload>(
            context.WrapExecution(DeadLetterQueueTelemetryName, errorHandler),
            options.ToDataflowOptions(context.CancellationToken)));

    /// <inheritdoc/>
    public IDataflowPipelineBuilder<THead, TTail> WithLoggingErrors(PipelineStepOptions options = default)
        => WithDeadLetterQueue(new ActionBlock<FailedPayload>(
            context.WrapExecution<FailedPayload>(
                DeadLetterQueueTelemetryName,
                failedPayload => context.Logger.LogError("Failed to execute step. Failed payload: {@FailedPayload}", failedPayload)),
            options.ToDataflowOptions(context.CancellationToken)));

    /// <inheritdoc/>
    public IDataflowPipelineBuilder<THead, TNextOut> AddDataMapping<TNextOut>(Func<TTail, TNextOut> mapFunc, PipelineStepOptions options = default)
    {
        ExecutionDataflowBlockOptions dataFlowOptions = options.ToDataflowOptions(context.CancellationToken);

        return context.IsDeadLetterQueueEnabled
            ? AddSafeStep(new TransformBlock<TTail, SafeResult<TTail, TNextOut>>(context.WrapSafeExecution(DataMappingTelemetryName, mapFunc), dataFlowOptions), dataFlowOptions, CurrentStepNames)
            : LinkAndContinue(new TransformBlock<TTail, TNextOut>(context.WrapExecution(DataMappingTelemetryName, mapFunc), dataFlowOptions), CurrentStepNames);
    }

    /// <inheritdoc/>
    public IDataflowPipelineBuilder<THead, TTail> AddFilterStep(Predicate<TTail> predicate, PipelineStepOptions options = default)
    {
        ExecutionDataflowBlockOptions dataFlowOptions = options.ToDataflowOptions(context.CancellationToken);
        TransformBlock<TTail, TTail> filterBlock = new(
            context.WrapExecution<TTail, TTail>("Pipeline.Filter", item => item),
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
        ExecutionDataflowBlockOptions dataFlowOptions = options.ToDataflowOptions(context.CancellationToken);
        (BufferBlock<TTail> buffer1, DataflowPipelineBuilder<THead, TOut1> resolved1) = ResolveBranch(branch1, dataFlowOptions);
        (BufferBlock<TTail> buffer2, DataflowPipelineBuilder<THead, TOut2> resolved2) = ResolveBranch(branch2, dataFlowOptions);

        JoinBlock<TOut1, TOut2> joinBlock = new(context.CreateGroupingOptions(dataFlowOptions));
        resolved1.TailBlock.LinkTo(joinBlock.Target1);
        resolved2.TailBlock.LinkTo(joinBlock.Target2);

        return CompleteFork(
            joinBlock,
            dataFlowOptions,
            [buffer1, buffer2],
            [resolved1._branchCompletionTasks, resolved2._branchCompletionTasks],
            resolved1.CurrentStepNames,
            resolved2.CurrentStepNames);
    }

    /// <inheritdoc/>
    public IDataflowPipelineBuilder<THead, Tuple<TOut1, TOut2, TOut3>> Fork<TOut1, TOut2, TOut3>(
        Func<IDataflowPipelineBuilder<THead, TTail>, IDataflowPipelineBuilder<THead, TOut1>> branch1,
        Func<IDataflowPipelineBuilder<THead, TTail>, IDataflowPipelineBuilder<THead, TOut2>> branch2,
        Func<IDataflowPipelineBuilder<THead, TTail>, IDataflowPipelineBuilder<THead, TOut3>> branch3,
        PipelineStepOptions options = default)
    {
        ExecutionDataflowBlockOptions dataFlowOptions = options.ToDataflowOptions(context.CancellationToken);
        (BufferBlock<TTail> buffer1, DataflowPipelineBuilder<THead, TOut1> resolved1) = ResolveBranch(branch1, dataFlowOptions);
        (BufferBlock<TTail> buffer2, DataflowPipelineBuilder<THead, TOut2> resolved2) = ResolveBranch(branch2, dataFlowOptions);
        (BufferBlock<TTail> buffer3, DataflowPipelineBuilder<THead, TOut3> resolved3) = ResolveBranch(branch3, dataFlowOptions);

        JoinBlock<TOut1, TOut2, TOut3> joinBlock = new(context.CreateGroupingOptions(dataFlowOptions));
        resolved1.TailBlock.LinkTo(joinBlock.Target1);
        resolved2.TailBlock.LinkTo(joinBlock.Target2);
        resolved3.TailBlock.LinkTo(joinBlock.Target3);

        return CompleteFork(
            joinBlock,
            dataFlowOptions,
            [buffer1, buffer2, buffer3],
            [resolved1._branchCompletionTasks, resolved2._branchCompletionTasks, resolved3._branchCompletionTasks],
            resolved1.CurrentStepNames,
            resolved2.CurrentStepNames,
            resolved3.CurrentStepNames);
    }

    /// <inheritdoc/>
    public IDataflowPipelineBuilder<THead, Tuple<TOut1, TOut2>> Fork<TOut1, TOut2>(
        IPipelineStep<TTail, TOut1> branch1Step,
        IPipelineStep<TTail, TOut2> branch2Step,
        PipelineStepOptions options = default)
        => Fork(b => AddStepDynamic(b, branch1Step), b => AddStepDynamic(b, branch2Step), options);

    /// <inheritdoc/>
    public IDataflowPipelineBuilder<THead, Tuple<TOut1, TOut2, TOut3>> Fork<TOut1, TOut2, TOut3>(
        IPipelineStep<TTail, TOut1> branch1Step,
        IPipelineStep<TTail, TOut2> branch2Step,
        IPipelineStep<TTail, TOut3> branch3Step,
        PipelineStepOptions options = default)
        => Fork(b => AddStepDynamic(b, branch1Step), b => AddStepDynamic(b, branch2Step), b => AddStepDynamic(b, branch3Step), options);

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
        List<TTail> buffer = new(128);
        TKey currentKey = default!;
        bool hasKey = false;

        return AddSequentialAccumulatorStep<IGrouping<TKey, TTail>>(
            "Pipeline.GroupWhile",
            nameof(AddGroupWhileStep),
            source => async (item, ct) =>
            {
                if (buffer.Count > 0 && !groupingCondition(buffer[^1], item))
                {
                    await source.SendAsync(new Grouping<TKey, TTail>(currentKey, [.. buffer]), ct).ConfigureAwait(false);
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
            async source =>
            {
                // Flush the final partial batch trapped in the buffer before propagating completion downstream.
                if (buffer.Count > 0)
                {
                    await source.SendAsync(new Grouping<TKey, TTail>(currentKey, [.. buffer]), context.CancellationToken).ConfigureAwait(false);
                    buffer.Clear();
                }
            },
            options);
    }

    /// <inheritdoc/>
    public IDataflowPipelineBuilder<THead, TTail[]> AddCollectAllStep(PipelineStepOptions options = default)
    {
        List<TTail> buffer = new(1024);

        return AddSequentialAccumulatorStep<TTail[]>(
            "Pipeline.CollectAll",
            nameof(AddCollectAllStep),
            _ => (item, _) =>
            {
                buffer.Add(item);
                return Task.CompletedTask;
            },
            async source =>
            {
                if (buffer.Count > 0)
                {
                    await source.SendAsync([.. buffer], context.CancellationToken).ConfigureAwait(false);
                    buffer.Clear();
                }
            },
            options);
    }

    /// <inheritdoc/>
    public IDataflowPipelineBuilder<THead, TNextOut> AddStep<TNextOut>(ISyncPipelineStep<TTail, TNextOut> step, PipelineStepOptions options = default) => AddTransformStep(
        step,
        () => context.WrapExecution<TTail, TNextOut>(step.Name, step.Execute),
        () => context.WrapSafeExecution<TTail, TNextOut>(step.Name, step.Execute),
        options);

    /// <inheritdoc/>
    public IDataflowPipelineBuilder<THead, TNextOut> AddStep<TNextOut>(IAsyncPipelineStep<TTail, TNextOut> step, PipelineStepOptions options = default) => AddTransformStep(
        step,
        () => context.WrapExecution<TTail, TNextOut>(step.Name, step.ExecuteAsync),
        () => context.WrapSafeExecution<TTail, TNextOut>(step.Name, step.ExecuteAsync),
        options);

    /// <inheritdoc/>
    public IDataflowPipelineBuilder<THead, TNextOut> AddStep<TNextOut>(IAsyncEnumerablePipelineStep<TTail, TNextOut> step, PipelineStepOptions options = default) => AddTransformManyStep(
        step,
        () => context.WrapExecution<TTail, TNextOut>(step.Name, step.ExecuteAsync),
        () => context.WrapSafeExecution<TTail, TNextOut>(step.Name, step.ExecuteAsync),
        options);

    /// <inheritdoc/>
    public IDataflowPipelineBuilder<THead, TNextOut> AddStep<TNextOut>(IEnumerablePipelineStep<TTail, TNextOut> step, PipelineStepOptions options = default) => AddTransformManyStep(
        step,
        () => context.WrapExecution<TTail, TNextOut>(step.Name, step.Execute),
        () => context.WrapSafeExecution<TTail, TNextOut>(step.Name, step.Execute),
        options);

    /// <inheritdoc/>
    public IDataflowPipelineBuilder<THead, TNextOut> AddForkingStep<TNextOut>(
        ISyncPipelineStep<TTail, TNextOut> step,
        Func<TNextOut, bool> fallbackCondition,
        ISyncPipelineStep<TTail, TNextOut> fallbackStep,
        PipelineStepOptions options = default)
        => AddForkingTransformStep(
            step,
            fallbackStep,
            () => context.WrapForkExecution<TTail, TNextOut>(step.Name, fallbackStep.Name, step.Execute, fallbackCondition, fallbackStep.Execute),
            () => context.WrapSafeForkExecution<TTail, TNextOut>(
                step.Name,
                fallbackStep.Name,
                step.Execute,
                safeResult => safeResult.Success && safeResult.Output is not null && fallbackCondition(safeResult.Output),
                fallbackStep.Execute),
            options);

    /// <inheritdoc/>
    public IDataflowPipelineBuilder<THead, TNextOut> AddForkingStep<TNextOut>(
        IAsyncPipelineStep<TTail, TNextOut> step,
        Func<TNextOut, bool> fallbackCondition,
        IAsyncPipelineStep<TTail, TNextOut> fallbackStep,
        PipelineStepOptions options = default)
        => AddForkingTransformStep(
            step,
            fallbackStep,
            () => context.WrapForkExecution<TTail, TNextOut>(step.Name, fallbackStep.Name, step.ExecuteAsync, fallbackCondition, fallbackStep.ExecuteAsync),
            () => context.WrapSafeForkExecution<TTail, TNextOut>(
                step.Name,
                fallbackStep.Name,
                step.ExecuteAsync,
                safeResult => safeResult.Success && safeResult.Output is not null && fallbackCondition(safeResult.Output),
                fallbackStep.ExecuteAsync),
            options);

    /// <inheritdoc/>
    /// <remarks>
    /// Intentionally uses <see cref="DataflowFanOut"/> instead of <see cref="BroadcastBlock{T}"/> so slow side-effect
    /// observers apply backpressure and never lose messages silently.
    /// </remarks>
    public IDataflowPipelineBuilder<THead, TTail> AddBroadcastStep(IAsyncPipelineStep<TTail> step, Func<TTail, TTail>? cloneFunc = null, PipelineStepOptions options = default)
        => AddBroadcastCore(step, step.ExecuteAsync, cloneFunc, options);

    /// <inheritdoc/>
    /// <remarks>
    /// Intentionally uses <see cref="DataflowFanOut"/> instead of <see cref="BroadcastBlock{T}"/> so slow side-effect
    /// observers apply backpressure and never lose messages silently.
    /// </remarks>
    public IDataflowPipelineBuilder<THead, TTail> AddBroadcastStep<TOut>(IAsyncPipelineStep<TOut> step, Func<TTail, TOut> mapFunc, PipelineStepOptions options = default)
        => AddBroadcastCore(step, (item, ct) => step.ExecuteAsync(mapFunc(item), ct), null, options);

    /// <inheritdoc/>
    /// <remarks>
    /// Intentionally uses <see cref="DataflowFanOut"/> instead of <see cref="BroadcastBlock{T}"/> so the secondary
    /// branch receives every item with full backpressure without affecting the primary stream.
    /// </remarks>
    public IDataflowPipelineBuilder<THead, TTail> AddBroadcastFork<TBranchOut>(
        Func<IDataflowPipelineBuilder<THead, TTail>, IDataflowPipelineBuilder<THead, TBranchOut>> branch,
        Func<TTail, TTail>? cloneFunc = null,
        PipelineStepOptions options = default)
    {
        ExecutionDataflowBlockOptions dataFlowOptions = options.ToDataflowOptions(context.CancellationToken);
        BufferBlock<TTail> mainBuffer = new(context.CreateBufferOptions(dataFlowOptions));
        (BufferBlock<TTail> branchBuffer, DataflowPipelineBuilder<THead, TBranchOut> resolvedBranch) = ResolveBranch(branch, dataFlowOptions);

        ActionBlock<TBranchOut> drainBlock = new(_ => { }, dataFlowOptions);
        resolvedBranch.TailBlock.LinkTo(drainBlock);

        Task dispatchTask = DataflowFanOut.LinkDispatcher(tailBlock, dataFlowOptions, [mainBuffer, branchBuffer], cloneFunc, context.CancellationToken);
        return ContinueWith(mainBuffer, CurrentStepNames, [.. resolvedBranch._branchCompletionTasks, drainBlock.Completion, dispatchTask]);
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
            options.ToDataflowOptions(context.CancellationToken));

        tailBlock.LinkTo(consumerBlock);
        Task pipelineBodyCompletionTask = _branchCompletionTasks.Count > 0
            ? Task.WhenAll([completionTask, .. _branchCompletionTasks])
            : completionTask;

        return new DataflowPipeline<THead>(context, MonitorCompletionAsync(pipelineBodyCompletionTask));
    }

    /// <inheritdoc/>
    public IDataflowPipeline<THead> BuildTerminal(string stepName = "Terminal", PipelineStepOptions options = default)
        => BuildTerminal(stepName, (_, _) => Task.CompletedTask, options);

    private static IDataflowPipelineBuilder<THead, TOut> AddStepDynamic<TOut>(IDataflowPipelineBuilder<THead, TTail> builder, IPipelineStep<TTail, TOut> step) => step switch
    {
        ISyncPipelineStep<TTail, TOut> syncStep => builder.AddStep(syncStep),
        IAsyncPipelineStep<TTail, TOut> asyncStep => builder.AddStep(asyncStep),
        IAsyncEnumerablePipelineStep<TTail, TOut> enumStep => builder.AddStep(enumStep),
        IEnumerablePipelineStep<TTail, TOut> syncEnumStep => builder.AddStep(syncEnumStep),
        _ => throw new NotSupportedException($"Pipeline step type '{step.GetType().Name}' is not supported in this Fork overload.")
    };

    private (BufferBlock<TTail> Buffer, DataflowPipelineBuilder<THead, TOut> Builder) ResolveBranch<TOut>(
        Func<IDataflowPipelineBuilder<THead, TTail>, IDataflowPipelineBuilder<THead, TOut>> branch,
        ExecutionDataflowBlockOptions dataFlowOptions)
    {
        BufferBlock<TTail> branchBuffer = new(context.CreateBufferOptions(dataFlowOptions));
        DataflowPipelineBuilder<THead, TOut> resolvedBranch = (DataflowPipelineBuilder<THead, TOut>)branch(ContinueWith(branchBuffer, CurrentStepNames));
        return (branchBuffer, resolvedBranch);
    }

    private DataflowPipelineBuilder<THead, TJoinOut> CompleteFork<TJoinOut>(
        ISourceBlock<TJoinOut> joinBlock,
        ExecutionDataflowBlockOptions dataFlowOptions,
        ITargetBlock<TTail>[] branchBuffers,
        List<Task>[] branchTaskLists,
        params string[][] branchStepNames)
    {
        Task dispatchTask = DataflowFanOut.LinkDispatcher(tailBlock, dataFlowOptions, branchBuffers, cancellationToken: context.CancellationToken);
        List<Task> mergedTasks = [.. MergeBranchCompletionTasks(branchTaskLists), dispatchTask];
        string[] joinedStepNames = MergeStepNames(branchStepNames);

        return ContinueWith(joinBlock, joinedStepNames, mergedTasks);
    }

    private IDataflowPipelineBuilder<THead, TOut> AddSequentialAccumulatorStep<TOut>(
        string telemetryName,
        string callerMethodName,
        Func<BufferBlock<TOut>, Func<TTail, CancellationToken, Task>> createItemHandler,
        Func<BufferBlock<TOut>, Task> flushHandler,
        PipelineStepOptions options)
    {
        ExecutionDataflowBlockOptions dataFlowOptions = context.ToSequentialDataflowOptions(options, callerMethodName);
        BufferBlock<TOut> source = new(dataFlowOptions);
        ActionBlock<TTail> target = new(
            context.WrapExecution(telemetryName, createItemHandler(source)),
            dataFlowOptions);

        Task flushTask = target.Completion.ContinueWith(source, () => flushHandler(source));
        IPropagatorBlock<TTail, TOut> accumulatorBlock = DataflowBlock.Encapsulate(target, source);

        return LinkAndContinue(accumulatorBlock, CurrentStepNames, [.. _branchCompletionTasks, flushTask]);
    }

    private IDataflowPipelineBuilder<THead, TNextOut> AddTransformStep<TNextOut>(
        IPipelineStep step,
        Func<Func<TTail, Task<TNextOut>>> executionFactory,
        Func<Func<TTail, Task<SafeResult<TTail, TNextOut>>>> safeExecutionFactory,
        PipelineStepOptions options)
    {
        context.Nodes.Add((CurrentStepNames, step));
        ExecutionDataflowBlockOptions dataFlowOptions = options.ToDataflowOptions(context.CancellationToken);

        return context.IsDeadLetterQueueEnabled
            ? AddSafeStep(new TransformBlock<TTail, SafeResult<TTail, TNextOut>>(safeExecutionFactory(), dataFlowOptions), dataFlowOptions, [step.Name])
            : LinkAndContinue(new TransformBlock<TTail, TNextOut>(executionFactory(), dataFlowOptions), [step.Name]);
    }

    private IDataflowPipelineBuilder<THead, TNextOut> AddTransformManyStep<TNextOut>(
        IPipelineStep step,
        Func<Func<TTail, IAsyncEnumerable<TNextOut>>> executionFactory,
        Func<Func<TTail, IAsyncEnumerable<SafeResult<TTail, TNextOut>>>> safeExecutionFactory,
        PipelineStepOptions options)
    {
        context.Nodes.Add((CurrentStepNames, step));
        ExecutionDataflowBlockOptions dataFlowOptions = options.ToDataflowOptions(context.CancellationToken);

        return context.IsDeadLetterQueueEnabled
            ? AddSafeStep(new TransformManyBlock<TTail, SafeResult<TTail, TNextOut>>(safeExecutionFactory(), dataFlowOptions), dataFlowOptions, [step.Name])
            : LinkAndContinue(new TransformManyBlock<TTail, TNextOut>(executionFactory(), dataFlowOptions), [step.Name]);
    }

    private IDataflowPipelineBuilder<THead, TNextOut> AddForkingTransformStep<TNextOut>(
        IPipelineStep primaryStep,
        IPipelineStep fallbackStep,
        Func<Func<TTail, Task<TNextOut>>> executionFactory,
        Func<Func<TTail, Task<SafeResult<TTail, TNextOut>>>> safeExecutionFactory,
        PipelineStepOptions options)
    {
        context.Nodes.Add((CurrentStepNames, primaryStep));
        context.Nodes.Add((CurrentStepNames, fallbackStep));
        ExecutionDataflowBlockOptions dataFlowOptions = options.ToDataflowOptions(context.CancellationToken);
        string[] compoundStepNames = [primaryStep.Name, fallbackStep.Name];

        return context.IsDeadLetterQueueEnabled
            ? AddSafeStep(new TransformBlock<TTail, SafeResult<TTail, TNextOut>>(safeExecutionFactory(), dataFlowOptions), dataFlowOptions, compoundStepNames)
            : LinkAndContinue(new TransformBlock<TTail, TNextOut>(executionFactory(), dataFlowOptions), compoundStepNames);
    }

    private IDataflowPipelineBuilder<THead, TTail> AddBroadcastCore(
        IPipelineStep step,
        Func<TTail, CancellationToken, Task> consumerAction,
        Func<TTail, TTail>? cloneFunc,
        PipelineStepOptions options)
    {
        context.Nodes.Add((CurrentStepNames, step));
        ExecutionDataflowBlockOptions dataFlowOptions = options.ToDataflowOptions(context.CancellationToken);
        BufferBlock<TTail> mainBuffer = new(context.CreateBufferOptions(dataFlowOptions));

        (ITargetBlock<TTail> targetBlock, Task completionTask) = CreateConsumer<TTail>(step.Name, consumerAction, dataFlowOptions);
        Task dispatchTask = DataflowFanOut.LinkDispatcher(tailBlock, dataFlowOptions, [mainBuffer, targetBlock], cloneFunc, context.CancellationToken);

        return ContinueWith(mainBuffer, CurrentStepNames, [.. _branchCompletionTasks, completionTask, dispatchTask]);
    }

    private (ITargetBlock<TInput> TargetBlock, Task CompletionTask) CreateConsumer<TInput>(
        string stepName,
        Func<TInput, CancellationToken, Task> action,
        ExecutionDataflowBlockOptions dataFlowOptions)
    {
        if (context.IsDeadLetterQueueEnabled)
        {
            TransformBlock<TInput, SafeResult<TInput>> safeBlock = new(
                context.WrapSafeExecution(stepName, action),
                dataFlowOptions);

            Task failureSinkCompletion = LinkDeadLetterFailure(safeBlock, r => r.Success, r => r.FailedPayload!, dataFlowOptions);
            safeBlock.LinkTo(DataflowBlock.NullTarget<SafeResult<TInput>>());
            return (safeBlock, Task.WhenAll(safeBlock.Completion, failureSinkCompletion));
        }

        ActionBlock<TInput> terminalBlock = new(
            context.WrapExecution(stepName, action),
            dataFlowOptions);
        return (terminalBlock, terminalBlock.Completion);
    }

    private DataflowPipelineBuilder<THead, TTail> WithDeadLetterQueue(ITargetBlock<FailedPayload> deadLetterQueueSink)
        => new(context with { DeadLetterQueueBlock = deadLetterQueueSink }, tailBlock, _branchCompletionTasks, CurrentStepNames);

    private DataflowPipelineBuilder<THead, TNextOut> AddSafeStep<TNextOut>(
        IPropagatorBlock<TTail, SafeResult<TTail, TNextOut>> safeBlock,
        ExecutionDataflowBlockOptions dataFlowOptions,
        string[] nextStepNames)
    {
        tailBlock.LinkTo(safeBlock);

        TransformBlock<SafeResult<TTail, TNextOut>, TNextOut> successBlock = new(safeResult => safeResult.Output!, dataFlowOptions);
        safeBlock.LinkTo(successBlock, safeResult => safeResult.Success);

        Task failureSinkCompletion = LinkDeadLetterFailure(safeBlock, r => r.Success, r => r.FailedPayload!, dataFlowOptions);
        return ContinueWith(successBlock, nextStepNames, [.. _branchCompletionTasks, failureSinkCompletion]);
    }

    private Task LinkDeadLetterFailure<TSafe>(
        ISourceBlock<TSafe> safeBlock,
        Func<TSafe, bool> isSuccess,
        Func<TSafe, FailedPayload> getFailedPayload,
        ExecutionDataflowBlockOptions dataFlowOptions)
    {
        // Route failed payloads through an adapter block instead of linking directly so step completion never closes the shared DLQ.
        ActionBlock<TSafe> failedBlock = new(
            safeResult => context.DeadLetterQueueBlock!.SendAsync(getFailedPayload(safeResult), context.CancellationToken),
            dataFlowOptions);

        safeBlock.LinkTo(failedBlock, safeResult => !isSuccess(safeResult));
        return failedBlock.Completion;
    }

    private async Task MonitorCompletionAsync(Task pipelineBodyCompletionTask)
    {
        Task finalTask = pipelineBodyCompletionTask;
        if (context.DeadLetterQueueBlock is not null)
        {
            try
            {
                await pipelineBodyCompletionTask.ConfigureAwait(false);
                context.DeadLetterQueueBlock.Complete();
            }
            catch (Exception ex)
            {
                context.DeadLetterQueueBlock.Fault(pipelineBodyCompletionTask.Exception ?? ex);
            }

            finalTask = context.DeadLetterQueueBlock.Completion;
        }

        try
        {
            await finalTask.ConfigureAwait(false);
            context.ProgressNotifier.TryComplete(context.PipelineIdentifier);
        }
        catch (Exception ex)
        {
            context.ProgressNotifier.TryComplete(context.PipelineIdentifier, finalTask.Exception ?? ex);
            throw;
        }
    }

    private DataflowPipelineBuilder<THead, TNextOut> LinkAndContinue<TNextOut>(
        IPropagatorBlock<TTail, TNextOut> nextBlock,
        string[] nextStepNames,
        List<Task>? branchTasks = null)
    {
        tailBlock.LinkTo(nextBlock);
        return ContinueWith(nextBlock, nextStepNames, branchTasks);
    }

    private DataflowPipelineBuilder<THead, TNextOut> ContinueWith<TNextOut>(
        ISourceBlock<TNextOut> nextTailBlock,
        string[] nextStepNames,
        List<Task>? branchTasks = null)
        => new(context, nextTailBlock, branchTasks ?? _branchCompletionTasks, nextStepNames);

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
