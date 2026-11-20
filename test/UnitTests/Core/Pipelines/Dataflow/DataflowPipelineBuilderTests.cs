using MelloSilveiraTools.Core.Pipelines;
using MelloSilveiraTools.Core.Pipelines.Dataflow;
using MelloSilveiraTools.Core.Pipelines.Models;
using MelloSilveiraTools.Core.Pipelines.Notifiers;
using MelloSilveiraTools.Core.Pipelines.Steps;
using MelloSilveiraTools.Core.Pipelines.Telemetry;
using Microsoft.Extensions.Logging.Abstractions;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace UnitTests.Core.Pipelines.Dataflow;

/// <summary>
/// Unit tests for <see cref="DataflowPipelineBuilder{THead, TTail}"/>, <see cref="ObservabilityExtensions"/>,
/// <see cref="PipelineStepOptions"/>, and <see cref="Grouping{TKey, TElement}"/> covering all TPL Dataflow operators,
/// DLQ error routing, topology tracking, OpenTelemetry spans, and step disposal.
/// </summary>
public sealed class DataflowPipelineBuilderTests
{
    private sealed class SyncDoubleStep(string name = "SyncDouble") : ISyncPipelineStep<int, int>
    {
        public string Name => name;
        public bool Disposed { get; private set; }
        public int Execute(int input) => input < 0 ? throw new InvalidOperationException("Negative input") : input * 2;
        public void Dispose() => Disposed = true;
    }

    private sealed class AsyncAddStep(int delta, string name = "AsyncAdd") : IAsyncPipelineStep<int, int>
    {
        public string Name => name;
        public bool AsyncDisposed { get; private set; }
        public Task<int> ExecuteAsync(int input, CancellationToken cancellationToken = default)
            => input < -100 ? throw new InvalidOperationException("Too negative") : Task.FromResult(input + delta);
        public ValueTask DisposeAsync()
        {
            AsyncDisposed = true;
            return ValueTask.CompletedTask;
        }
    }

    private sealed class SyncExpandStep(bool throwOnNegative = false) : IEnumerablePipelineStep<int, int>
    {
        public string Name => "SyncExpand";
        public IEnumerable<int> Execute(int input, CancellationToken cancellationToken = default)
        {
            if (throwOnNegative && input < 0)
                throw new InvalidOperationException("Sync enumerable error");

            yield return input;
            yield return input + 10;
        }
        public void Dispose() { }
    }

    private sealed class AsyncExpandStep(bool throwOnNegative = false) : IAsyncEnumerablePipelineStep<int, int>
    {
        public string Name => "AsyncExpand";
        public async IAsyncEnumerable<int> ExecuteAsync(int input, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            if (throwOnNegative && input < 0)
                throw new InvalidOperationException("Async enumerable error");

            yield return input * 10;
            yield return input * 10 + 1;
        }
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private sealed class AsyncObserverStep(ConcurrentBag<int> sink, string name = "Observer") : IAsyncPipelineStep<int>
    {
        public string Name => name;
        public Task ExecuteAsync(int input, CancellationToken cancellationToken = default)
        {
            sink.Add(input);
            return Task.CompletedTask;
        }
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private sealed class UnsupportedStep : IPipelineStep<int, int>
    {
        public string Name => "Unsupported";
    }

    private static PipelineFactory CreateFactory(IPipelineProgressNotifier? notifier = null)
        => new(notifier ?? NullPipelineProgressNotifier.Instance, TimeProvider.System);

    /// <summary>
    /// Verifies sync, async, mapping, filter, broadcast, topology tracking, ActivityListener telemetry, and step disposal.
    /// </summary>
    [Fact]
    public async Task LinearAndBroadcastPipeline_ProcessesItemsTracksTopologyAndDisposesSteps()
    {
        // Arrange
        using ActivityListener activityListener = new()
        {
            ShouldListenTo = source => source.Name == TelemetryConstants.PipelineSourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded
        };
        ActivitySource.AddActivityListener(activityListener);

        PipelineProgressNotifierSettings notifierSettings = new()
        {
            ChannelCapacity = 128,
            FullMode = BoundedChannelFullMode.Wait,
            MaxChannels = 4
        };
        await using ChannelPipelineProgressNotifier notifier = new(TimeProvider.System, notifierSettings);
        PipelineFactory factory = CreateFactory(notifier);

        SyncDoubleStep syncStep = new();
        AsyncAddStep asyncStep = new(3);
        ConcurrentBag<int> observedSameType = [];
        ConcurrentBag<int> observedMapped = [];
        ConcurrentBag<int> broadcastForkObserved = [];
        ConcurrentBag<int> terminalResults = [];

        IDataflowPipeline<int> pipeline = factory
            .CreateDataflow<int>(NullLogger.Instance, "pipe-linear", initialBufferSize: 8)
            .AddStep(syncStep, new PipelineStepOptions(maxWorkers: 2, maxBufferSize: 8, keepOrder: true))
            .AddBroadcastStep(new AsyncObserverStep(observedSameType, "Obs1"), cloneFunc: x => x)
            .AddBroadcastStep(new AsyncObserverStep(observedMapped, "Obs2"), mapFunc: x => x * 100)
            .AddBroadcastFork(branch => branch.AddDataMapping(x => x + 1).AddBroadcastStep(new AsyncObserverStep(broadcastForkObserved, "ObsFork")))
            .AddStep(asyncStep)
            .AddFilterStep(x => x > 5)
            .AddDataMapping(x => x * 2)
            .BuildTerminal("Collect", item => terminalResults.Add(item));

        // Act
        Assert.True(await pipeline.SendAsync(1)); // 1*2=2 -> +3=5 -> filtered out
        Assert.True(await pipeline.SendAsync(2)); // 2*2=4 -> +3=7 -> *2=14
        await pipeline.DisposeAsync();
        notifier.TryComplete("pipe-linear");

        List<PipelineProgressEvent> progressEvents = [];
        await foreach (PipelineProgressEvent evt in notifier.ReadAllAsync("pipe-linear"))
            progressEvents.Add(evt);

        // Assert
        Assert.Equal([14], [.. terminalResults]);
        Assert.Equal([2, 4], observedSameType.Order().ToArray());
        Assert.Equal([200, 400], observedMapped.Order().ToArray());
        Assert.Equal([3, 5], broadcastForkObserved.Order().ToArray());
        Assert.True(syncStep.Disposed);
        Assert.True(asyncStep.AsyncDisposed);
        Assert.NotEmpty(pipeline.Topology);
        Assert.Contains(progressEvents, e => e.State == PipelineStepState.Running && e.StepName == "SyncDouble");
    }

    /// <summary>
    /// Verifies IEnumerablePipelineStep and IAsyncEnumerablePipelineStep fan-out, AddGroupWhileStep, and AddCollectAllStep.
    /// </summary>
    [Fact]
    public async Task EnumerableGroupWhileAndCollectAll_ProduceExpectedGroupedAndCollectedOutputs()
    {
        // Arrange
        PipelineFactory factory = CreateFactory();
        List<int[]> collectedBatches = [];
        List<(int Key, int[] Items)> groupedBatches = [];

        await using IDataflowPipeline<int> pipeline = factory
            .CreateDataflow<int>(NullLogger.Instance, "pipe-grouping", 16)
            .AddStep(new SyncExpandStep())
            .AddStep(new AsyncExpandStep())
            .AddGroupWhileStep(x => x / 100, new PipelineStepOptions { MaxWorkers = 2, MaxBufferSize = 0 })
            .AddBroadcastFork(b => b.AddDataMapping(g =>
            {
                groupedBatches.Add((g.Key, g.ToArray()));
                return g.Key;
            }))
            .AddDataMapping(g => g.Sum())
            .AddCollectAllStep(new PipelineStepOptions { MaxWorkers = 2 })
            .BuildTerminal("Save", (batch, _) =>
            {
                collectedBatches.Add(batch);
                return Task.CompletedTask;
            });

        // Act
        await pipeline.SendAsync(1);
        pipeline.Complete();
        await pipeline.Completion;

        // Assert
        Assert.Single(collectedBatches);
        Assert.Equal(2, collectedBatches[0].Length);
        Assert.NotEmpty(groupedBatches);
    }

    /// <summary>
    /// Verifies 2-way and 3-way Fork overloads (both delegate and IPipelineStep overloads) and unsupported step exception.
    /// </summary>
    [Fact]
    public async Task Fork_TwoWayAndThreeWayDelegatesAndSteps_JoinOutputsCorrectly()
    {
        // Arrange
        PipelineFactory factory = CreateFactory();
        List<Tuple<int, int>> twoWayStepResults = [];
        List<Tuple<int, int, int>> threeWayStepResults = [];

        await using IDataflowPipeline<int> twoWayPipeline = factory
            .CreateDataflow<int>(NullLogger.Instance, "fork-2", 8)
            .Fork(new SyncDoubleStep("D1"), new AsyncAddStep(5, "A1"))
            .BuildTerminal("End2", item => twoWayStepResults.Add(item));

        await using IDataflowPipeline<int> threeWayPipeline = factory
            .CreateDataflow<int>(NullLogger.Instance, "fork-3", 8)
            .Fork(new SyncDoubleStep("D1"), new AsyncAddStep(5, "A1"), new SyncDoubleStep("D2"))
            .BuildTerminal("End3", item => threeWayStepResults.Add(item));

        // Act
        await twoWayPipeline.SendAsync(3);
        twoWayPipeline.Complete();
        await twoWayPipeline.Completion;

        await threeWayPipeline.SendAsync(4);
        threeWayPipeline.Complete();
        await threeWayPipeline.Completion;

        // Assert
        Assert.Single(twoWayStepResults);
        Assert.Equal(Tuple.Create(6, 8), twoWayStepResults[0]);
        Assert.Single(threeWayStepResults);
        Assert.Equal(Tuple.Create(8, 9, 8), threeWayStepResults[0]);

        Assert.Throws<NotSupportedException>(() =>
            factory.CreateDataflow<int>(NullLogger.Instance, "fork-unsupported", 8)
                .Fork(new UnsupportedStep(), new SyncDoubleStep()));
    }

    /// <summary>
    /// Verifies AddForkingStep (sync and async) triggers fallback when condition is met and bypasses fallback otherwise, both with and without DLQ.
    /// </summary>
    [Fact]
    public async Task AddForkingStep_SyncAndAsync_RoutesFallbackConditionally()
    {
        // Arrange
        PipelineFactory factory = CreateFactory();
        List<int> syncResults = [];
        List<int> asyncResults = [];
        List<FailedPayload> dlqFailures = [];
        TaskCompletionSource<bool> dlqProcessedTcs = new();

        await using IDataflowPipeline<int> syncPipe = factory
            .CreateDataflow<int>(NullLogger.Instance, "forking-sync", 8)
            .AddForkingStep(
                new SyncDoubleStep("Primary"),
                output => output > 10,
                new SyncDoubleStep("Fallback"))
            .BuildTerminal("SyncOut", item => syncResults.Add(item));

        await using IDataflowPipeline<int> asyncPipe = factory
            .CreateDataflow<int>(NullLogger.Instance, "forking-async-dlq", 8)
            .WithDeadLetterQueue(fp =>
            {
                dlqFailures.Add(fp);
                dlqProcessedTcs.TrySetResult(true);
            })
            .AddForkingStep(
                new AsyncAddStep(1, "PrimaryAsync"),
                output => output > 10,
                new AsyncAddStep(100, "FallbackAsync"))
            .BuildTerminal("AsyncOut", item => asyncResults.Add(item));

        // Act
        await syncPipe.SendAsync(3);  // 6 (no fallback)
        await syncPipe.SendAsync(6);  // 12 (> 10 -> fallback(6) = 12)
        syncPipe.Complete();
        await syncPipe.Completion;

        await asyncPipe.SendAsync(5);    // 6 (no fallback)
        await asyncPipe.SendAsync(12);   // 13 (> 10 -> fallback(12) = 112)
        await asyncPipe.SendAsync(-200); // throws -> captured by DLQ
        asyncPipe.Complete();
        await asyncPipe.Completion;
        await dlqProcessedTcs.Task.WaitAsync(TimeSpan.FromSeconds(5));

        // Assert
        Assert.Equal([6, 12], syncResults);
        Assert.Equal([6, 112], asyncResults);
        Assert.Single(dlqFailures);
        Assert.Equal("PrimaryAsync", dlqFailures[0].CallbackName);
    }

    /// <summary>
    /// Verifies WithDeadLetterQueue (Action, Func, and WithLoggingErrors) isolates exceptions in steps, enumerables, and terminal actions.
    /// </summary>
    [Fact]
    public async Task WithDeadLetterQueue_IsolatesFailuresAcrossStepsAndTerminalWithoutFaultingGraph()
    {
        // Arrange
        PipelineFactory factory = CreateFactory();
        List<FailedPayload> failedPayloads = [];
        List<int> terminalOutputs = [];
        TaskCompletionSource<bool> dlqAllReceivedTcs = new();

        await using IDataflowPipeline<int> pipeline = factory
            .CreateDataflow<int>(NullLogger.Instance, "dlq-pipe", 16)
            .WithDeadLetterQueue((fp, _) =>
            {
                failedPayloads.Add(fp);
                if (failedPayloads.Count >= 3)
                    dlqAllReceivedTcs.TrySetResult(true);
                return Task.CompletedTask;
            })
            .AddStep(new SyncExpandStep(throwOnNegative: true))
            .AddStep(new AsyncExpandStep(throwOnNegative: false))
            .AddDataMapping(x => x == 0 ? throw new DivideByZeroException("Zero") : x)
            .BuildTerminal("SafeTerminal", item =>
            {
                if (item == 11)
                    throw new InvalidOperationException("Terminal failure");

                terminalOutputs.Add(item);
            });

        // Act
        await pipeline.SendAsync(-1); // Fails in SyncExpandStep
        await pipeline.SendAsync(0);  // 0 -> [0, 10] -> [0, 1, 100, 101]; 0 fails in DataMapping
        await pipeline.SendAsync(1);  // 1 -> [1, 11] -> [10, 11, 110, 111]; 11 fails in SafeTerminal
        pipeline.Complete();
        await pipeline.Completion;
        await dlqAllReceivedTcs.Task.WaitAsync(TimeSpan.FromSeconds(5));

        // Assert
        Assert.Equal(3, failedPayloads.Count);
        Assert.Contains(failedPayloads, f => f.CallbackName == "SyncExpand");
        Assert.Contains(failedPayloads, f => f.CallbackName == "Pipeline.DataMapping");
        Assert.Contains(failedPayloads, f => f.CallbackName == "SafeTerminal");
        Assert.Equal([1, 100, 101, 10, 110, 111], terminalOutputs);

        // Also test WithLoggingErrors and default BuildTerminal
        await using IDataflowPipeline<int> loggingPipe = factory
            .CreateDataflow<int>(NullLogger.Instance, "dlq-log", 4)
            .WithLoggingErrors()
            .AddStep(new SyncDoubleStep())
            .BuildTerminal();

        await loggingPipe.SendAsync(-5);
        await loggingPipe.SendAsync(5);
        loggingPipe.Complete();
        await loggingPipe.Completion;
    }

    /// <summary>
    /// Verifies SendAsync returns false after Complete() is called and DisposeAsync reclaims steps even if pipeline faults.
    /// </summary>
    [Fact]
    public async Task SendAsync_AfterComplete_ReturnsFalseAndDisposeAsyncCleansStepsOnFault()
    {
        // Arrange
        PipelineFactory factory = CreateFactory();
        SyncDoubleStep step = new();
        IDataflowPipeline<int> pipeline = factory
            .CreateDataflow<int>(NullLogger.Instance, "fault-pipe", 4)
            .AddStep(step)
            .BuildTerminal("End", _ => { });

        // Act: Send negative item without DLQ to fault the block, and also check SendAsync after Complete()
        await pipeline.SendAsync(-1);
        AggregateException ex = await Assert.ThrowsAsync<AggregateException>(() => pipeline.DisposeAsync().AsTask());
        Assert.IsType<InvalidOperationException>(ex.InnerException);
        Assert.True(step.Disposed);

        await using IDataflowPipeline<int> completedPipe = factory
            .CreateDataflow<int>(NullLogger.Instance, "completed-pipe", 4)
            .BuildTerminal();
        completedPipe.Complete();
        bool accepted = await completedPipe.SendAsync(1);
        Assert.False(accepted);
    }

    /// <summary>
    /// Verifies MaxWorkers override logging in GroupWhile/CollectAll and all AddStepDynamic Fork overloads.
    /// </summary>
    [Fact]
    public async Task MaxWorkersOverride_AndAllForkStepInterfaces_ExecuteCorrectly()
    {
        PipelineFactory factory = CreateFactory();
        PipelineStepOptions multiWorker = new() { MaxWorkers = 2 };

        List<IGrouping<int, int>> grouped = [];
        await using IDataflowPipeline<int> groupPipe = factory
            .CreateDataflow<int>(NullLogger.Instance, "group-warn", 8)
            .AddGroupWhileStep(x => x / 10, multiWorker)
            .BuildTerminal("GroupOut", g => grouped.Add(g));

        await groupPipe.SendAsync(1);
        await groupPipe.SendAsync(12);
        groupPipe.Complete();
        await groupPipe.Completion;
        Assert.Equal(2, grouped.Count);

        List<int[]> collected = [];
        await using IDataflowPipeline<int> collectPipe = factory
            .CreateDataflow<int>(NullLogger.Instance, "collect-warn", 8)
            .AddCollectAllStep(multiWorker)
            .BuildTerminal("CollectOut", arr => collected.Add(arr));

        await collectPipe.SendAsync(7);
        await collectPipe.SendAsync(8);
        collectPipe.Complete();
        await collectPipe.Completion;
        Assert.Equal([7, 8], Assert.Single(collected));

        // Cover AddStepDynamic IEnumerablePipelineStep and IAsyncEnumerablePipelineStep branches in Fork
        List<Tuple<int, int>> enumForkOut = [];
        await using IDataflowPipeline<int> enumForkPipe = factory
            .CreateDataflow<int>(NullLogger.Instance, "enum-fork", 8)
            .Fork(new SyncExpandStep(), new AsyncExpandStep())
            .BuildTerminal("EnumForkOut", t => enumForkOut.Add(t));

        await enumForkPipe.SendAsync(5);
        enumForkPipe.Complete();
        await enumForkPipe.Completion;
        Assert.NotEmpty(enumForkOut);

        // Sync AddForkingStep with DLQ covers SafeResult branches on line 327; nested Fork covers MergeBranchCompletionTasks
        List<int> syncDlqOut = [];
        await using IDataflowPipeline<int> syncDlqPipe = factory
            .CreateDataflow<int>(NullLogger.Instance, "sync-fork-dlq", 8)
            .WithLoggingErrors()
            .AddForkingStep(
                new SyncDoubleStep("PrimarySync"),
                x => x > 10,
                new SyncDoubleStep("FallbackSync"),
                new PipelineStepOptions { MaxWorkers = 0 })
            .BuildTerminal("SyncDlqOut", x => syncDlqOut.Add(x));

        await syncDlqPipe.SendAsync(3);
        await syncDlqPipe.SendAsync(6);
        await syncDlqPipe.SendAsync(-1);
        syncDlqPipe.Complete();
        await syncDlqPipe.Completion;
        Assert.Equal([6, 12], syncDlqOut);

        List<Tuple<Tuple<int, int>, int>> nestedOut = [];
        await using IDataflowPipeline<int> nestedForkPipe = factory
            .CreateDataflow<int>(NullLogger.Instance, "nested-fork", 8)
            .Fork(
                b1 => b1.Fork(new SyncDoubleStep("N1"), new SyncDoubleStep("N2")),
                b2 => b2.AddStep(new AsyncAddStep(1, "N3")))
            .BuildTerminal("NestedOut", t => nestedOut.Add(t));

        await nestedForkPipe.SendAsync(2);
        nestedForkPipe.Complete();
        await nestedForkPipe.Completion;
        Assert.Single(nestedOut);
    }
}
