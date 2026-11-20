using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using AcceptanceTests.MechanicalOfMaterials.Optimizations.CurveFitterSteps;
using MelloSilveiraTools.Core.Pipelines;
using MelloSilveiraTools.Core.Pipelines.Dataflow;
using MelloSilveiraTools.Core.Pipelines.Models;
using MelloSilveiraTools.Core.Pipelines.Notifiers;
using MelloSilveiraTools.Core.Pipelines.Steps;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Models;
using Microsoft.Extensions.Logging.Abstractions;

namespace AcceptanceTests.Core.Pipelines;

/// <summary>
/// Acceptance tests validating step-by-step TPL Dataflow execution across all step contracts,
/// fan-out/fan-in joins, broadcasts, ordering guarantees, and deterministic resource disposal
/// using real experimental curve segments from <c>TestData</c>.
/// </summary>
public sealed class DataflowStepByStepExecutionAcceptanceTests
{
    private sealed record LedgerEntry(string StepName, string ItemKey, long Tick);

    private sealed record TrackedSegment(int OriginId, int CopyId, CurveSegment Segment)
    {
        public string Key => $"{OriginId}:{CopyId}:{Segment.Type}:{Segment.TimePoints.Length}";
    }

    private sealed class SequenceClock
    {
        private int _nextOriginId;
        private long _tick;

        public int NextOriginId() => Interlocked.Increment(ref _nextOriginId);
        public long NextTick() => Interlocked.Increment(ref _tick);
    }

    private sealed class SyncTaggingStep(string name, ConcurrentQueue<LedgerEntry> ledger, SequenceClock clock)
        : ISyncPipelineStep<CurveSegment, TrackedSegment>
    {
        public string Name => name;
        public bool Disposed { get; private set; }

        public TrackedSegment Execute(CurveSegment input)
        {
            TrackedSegment tracked = new(clock.NextOriginId(), 0, input);
            ledger.Enqueue(new LedgerEntry(Name, tracked.Key, clock.NextTick()));
            return tracked;
        }

        public void Dispose() => Disposed = true;
    }

    private sealed class AsyncPassThroughStep(string name, ConcurrentQueue<LedgerEntry> ledger, SequenceClock clock, int delayMs = 0)
        : IAsyncPipelineStep<TrackedSegment, TrackedSegment>
    {
        public string Name => name;
        public bool AsyncDisposed { get; private set; }

        public async Task<TrackedSegment> ExecuteAsync(TrackedSegment input, CancellationToken cancellationToken = default)
        {
            if (delayMs > 0)
            {
                await Task.Delay(delayMs, cancellationToken).ConfigureAwait(false);
            }

            ledger.Enqueue(new LedgerEntry(Name, input.Key, clock.NextTick()));
            return input;
        }

        public ValueTask DisposeAsync()
        {
            AsyncDisposed = true;
            return ValueTask.CompletedTask;
        }
    }

    private sealed class SyncExpandCopiesStep(string name, ConcurrentQueue<LedgerEntry> ledger, SequenceClock clock)
        : IEnumerablePipelineStep<TrackedSegment, TrackedSegment>
    {
        public string Name => name;
        public bool Disposed { get; private set; }

        public IEnumerable<TrackedSegment> Execute(TrackedSegment input, CancellationToken cancellationToken = default)
        {
            TrackedSegment primary = input with { CopyId = 1 };
            ledger.Enqueue(new LedgerEntry(Name, primary.Key, clock.NextTick()));
            yield return primary;

            TrackedSegment secondary = input with { CopyId = 2 };
            ledger.Enqueue(new LedgerEntry(Name, secondary.Key, clock.NextTick()));
            yield return secondary;
        }

        public void Dispose() => Disposed = true;
    }

    private sealed class AsyncStreamStep(string name, ConcurrentQueue<LedgerEntry> ledger, SequenceClock clock)
        : IAsyncEnumerablePipelineStep<TrackedSegment, TrackedSegment>
    {
        public string Name => name;
        public bool AsyncDisposed { get; private set; }

        public async IAsyncEnumerable<TrackedSegment> ExecuteAsync(TrackedSegment input, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            TrackedSegment next = input with { CopyId = input.CopyId * 10 };
            ledger.Enqueue(new LedgerEntry(Name, next.Key, clock.NextTick()));
            yield return next;
        }

        public ValueTask DisposeAsync()
        {
            AsyncDisposed = true;
            return ValueTask.CompletedTask;
        }
    }

    private sealed class BroadcastObserverStep<T>(string name, ConcurrentQueue<T> sink) : IAsyncPipelineStep<T>
    {
        public string Name => name;
        public bool AsyncDisposed { get; private set; }

        public Task ExecuteAsync(T input, CancellationToken cancellationToken = default)
        {
            sink.Enqueue(input);
            return Task.CompletedTask;
        }

        public ValueTask DisposeAsync()
        {
            AsyncDisposed = true;
            return ValueTask.CompletedTask;
        }
    }

    private sealed class AsyncForkBranchStep(string name, ConcurrentQueue<LedgerEntry> ledger, SequenceClock clock)
        : IAsyncPipelineStep<TrackedSegment, string>
    {
        public string Name => name;
        public bool AsyncDisposed { get; private set; }

        public async Task<string> ExecuteAsync(TrackedSegment input, CancellationToken cancellationToken = default)
        {
            await Task.Delay(5, cancellationToken).ConfigureAwait(false);
            ledger.Enqueue(new LedgerEntry(Name, input.Key, clock.NextTick()));
            return $"{Name}:{input.OriginId}";
        }

        public ValueTask DisposeAsync()
        {
            AsyncDisposed = true;
            return ValueTask.CompletedTask;
        }
    }

    private sealed class SyncForkBranchStep(string name, ConcurrentQueue<LedgerEntry> ledger, SequenceClock clock)
        : ISyncPipelineStep<TrackedSegment, int>
    {
        public string Name => name;
        public bool Disposed { get; private set; }

        public int Execute(TrackedSegment input)
        {
            ledger.Enqueue(new LedgerEntry(Name, input.Key, clock.NextTick()));
            return input.Segment.TimePoints.Length;
        }

        public void Dispose() => Disposed = true;
    }

    /// <summary>
    /// Verifies that a multi-contract TPL Dataflow DAG executes every step in exact topological order,
    /// preserves item pairing across forks, triggers broadcast observers without mutating the primary stream,
    /// groups and collects batches accurately, and disposes 100% of registered steps.
    /// </summary>
    [Fact]
    public async Task DataflowPipeline_AllStepContractsAndOperators_ExecuteStepByStepAndDisposeAllSteps()
    {
        IReadOnlyList<(string GroupName, CurveSegment[] Segments)> groups = await AcceptanceTestHelpers.LoadSegmentGroupsForPrefixAsync("FungRelaxationOnly", useSharedFiles: true);
        Assert.NotEmpty(groups);

        CurveSegment[] allSegments = [.. groups.SelectMany(g => g.Segments)];
        Assert.NotEmpty(allSegments);

        SequenceClock clock = new();
        ConcurrentQueue<LedgerEntry> ledger = [];
        ConcurrentQueue<TrackedSegment> broadcastSameTypeSink = [];
        ConcurrentQueue<(SegmentType Type, int Length)> broadcastMappedSink = [];
        ConcurrentQueue<TrackedSegment> broadcastForkSink = [];
        ConcurrentQueue<IGrouping<SegmentType, Tuple<TrackedSegment, string>>[]> collectedBatches = [];

        SyncTaggingStep syncTagStep = new("Step1_SyncTag", ledger, clock);
        AsyncPassThroughStep asyncStep = new("Step2_AsyncPass", ledger, clock);
        SyncExpandCopiesStep syncExpandStep = new("Step3_SyncExpand", ledger, clock);
        AsyncStreamStep asyncStreamStep = new("Step4_AsyncStream", ledger, clock);
        BroadcastObserverStep<TrackedSegment> observerSame = new("Broadcast_SameType", broadcastSameTypeSink);
        BroadcastObserverStep<(SegmentType Type, int Length)> observerMapped = new("Broadcast_Mapped", broadcastMappedSink);
        AsyncPassThroughStep broadcastForkInner = new("BroadcastFork_Inner", ledger, clock);
        AsyncPassThroughStep forkLeftStep = new("Step7_ForkLeft", ledger, clock);
        AsyncForkBranchStep forkRightStep = new("Step7_ForkRight", ledger, clock);

        PipelineFactory factory = new(NullPipelineProgressNotifier.Instance, TimeProvider.System);
        PipelineStepOptions orderedOptions = new(maxWorkers: 2, maxBufferSize: 8, keepOrder: true);

        IDataflowPipeline<CurveSegment> pipeline = factory
            .CreateDataflow<CurveSegment>(NullLogger.Instance, "accept-step-by-step-dag", initialBufferSize: 16)
            .AddStep(syncTagStep)
            .AddStep(asyncStep, orderedOptions)
            .AddStep(syncExpandStep)
            .AddStep(asyncStreamStep)
            .AddBroadcastStep(observerSame, cloneFunc: ts => ts with { })
            .AddBroadcastStep(observerMapped, mapFunc: ts => (ts.Segment.Type, ts.Segment.TimePoints.Length))
            .AddBroadcastFork(branch => branch
                .AddStep(broadcastForkInner)
                .AddDataMapping(ts =>
                {
                    broadcastForkSink.Enqueue(ts);
                    return ts.OriginId;
                }))
            .AddFilterStep(ts => ts.CopyId == 10)
            .Fork(
                b1 => b1.AddStep(forkLeftStep),
                b2 => b2.AddStep(forkRightStep))
            .AddGroupWhileStep(tuple => tuple.Item1.Segment.Type)
            .AddCollectAllStep()
            .BuildTerminal("Step10_Terminal", (IGrouping<SegmentType, Tuple<TrackedSegment, string>>[] batches) =>
            {
                foreach (IGrouping<SegmentType, Tuple<TrackedSegment, string>> batch in batches)
                {
                    foreach (Tuple<TrackedSegment, string> tuple in batch)
                    {
                        ledger.Enqueue(new LedgerEntry("Step10_Terminal", tuple.Item1.Key, clock.NextTick()));
                    }
                }

                collectedBatches.Enqueue(batches);
            });

        foreach (CurveSegment segment in allSegments)
        {
            Assert.True(await pipeline.SendAsync(segment));
        }

        pipeline.Complete();
        await pipeline.Completion;
        await pipeline.DisposeAsync();

        // 1. Assert all steps disposed
        Assert.True(syncTagStep.Disposed);
        Assert.True(asyncStep.AsyncDisposed);
        Assert.True(syncExpandStep.Disposed);
        Assert.True(asyncStreamStep.AsyncDisposed);
        Assert.True(observerSame.AsyncDisposed);
        Assert.True(observerMapped.AsyncDisposed);
        Assert.True(broadcastForkInner.AsyncDisposed);
        Assert.True(forkLeftStep.AsyncDisposed);
        Assert.True(forkRightStep.AsyncDisposed);

        // 2. Assert broadcasts observed all expanded items (2 copies per input segment)
        int expectedExpandedCount = allSegments.Length * 2;
        Assert.Equal(expectedExpandedCount, broadcastSameTypeSink.Count);
        Assert.Equal(expectedExpandedCount, broadcastMappedSink.Count);
        Assert.Equal(expectedExpandedCount, broadcastForkSink.Count);

        // 3. Assert filtered items (CopyId == 10) match exact count of input segments at terminal & preserve order
        List<Tuple<TrackedSegment, string>> terminalTuples = [.. collectedBatches.SelectMany(arr => arr).SelectMany(g => g)];
        Assert.Equal(allSegments.Length, terminalTuples.Count);
        for (int i = 0; i < terminalTuples.Count; i++)
        {
            int expectedOriginId = i + 1;
            Assert.Equal(expectedOriginId, terminalTuples[i].Item1.OriginId);
            Assert.Equal($"Step7_ForkRight:{expectedOriginId}", terminalTuples[i].Item2);
        }

        // 4. Prove monotonically increasing step sequence ticks per origin item
        Dictionary<string, List<LedgerEntry>> entriesByOrigin = ledger
            .GroupBy(e => e.ItemKey.Split(':')[0])
            .ToDictionary(g => g.Key, g => g.OrderBy(x => x.Tick).ToList());

        Assert.Equal(allSegments.Length, entriesByOrigin.Count);
        foreach (KeyValuePair<string, List<LedgerEntry>> pair in entriesByOrigin)
        {
            List<string> stepNames = [.. pair.Value.Select(e => e.StepName)];
            int idxSyncTag = stepNames.IndexOf("Step1_SyncTag");
            int idxAsyncPass = stepNames.IndexOf("Step2_AsyncPass");
            int idxSyncExpand = stepNames.IndexOf("Step3_SyncExpand");
            int idxAsyncStream = stepNames.IndexOf("Step4_AsyncStream");
            int idxBroadcastFork = stepNames.IndexOf("BroadcastFork_Inner");
            int idxForkLeft = stepNames.IndexOf("Step7_ForkLeft");
            int idxForkRight = stepNames.IndexOf("Step7_ForkRight");
            int idxTerminal = stepNames.IndexOf("Step10_Terminal");

            Assert.True(idxSyncTag >= 0);
            Assert.True(idxAsyncPass > idxSyncTag);
            Assert.True(idxSyncExpand > idxAsyncPass);
            Assert.True(idxAsyncStream > idxSyncExpand);
            Assert.True(idxBroadcastFork > idxAsyncStream);
            Assert.True(idxForkLeft > idxAsyncStream);
            Assert.True(idxForkRight > idxAsyncStream);
            Assert.True(idxTerminal > idxForkLeft);
            Assert.True(idxTerminal > idxForkRight);
        }
    }

    /// <summary>
    /// Verifies that the 3-way <see cref="IDataflowPipelineBuilder{THead, TTail}.Fork{TOut1, TOut2, TOut3}(IPipelineStep{TTail, TOut1}, IPipelineStep{TTail, TOut2}, IPipelineStep{TTail, TOut3}, PipelineStepOptions)"/>
    /// overload accepting step instances joins synchronized triples in exact order under concurrent execution.
    /// </summary>
    [Fact]
    public async Task DataflowPipeline_ThreeWayStepFork_PreservesSynchronizedTriplesAndKeepOrder()
    {
        IReadOnlyList<(string GroupName, CurveSegment[] Segments)> groups = await AcceptanceTestHelpers.LoadSegmentGroupsForPrefixAsync("FungRelaxationOnly", useSharedFiles: false);
        Assert.NotEmpty(groups);

        CurveSegment[] segments = [.. groups.SelectMany(g => g.Segments)];
        SequenceClock clock = new();
        ConcurrentQueue<LedgerEntry> ledger = [];
        List<Tuple<TrackedSegment, string, int>> joinedResults = [];

        SyncTaggingStep tagStep = new("Tag", ledger, clock);
        AsyncPassThroughStep branch1 = new("Branch1", ledger, clock, delayMs: 8);
        AsyncForkBranchStep branch2 = new("Branch2", ledger, clock);
        SyncForkBranchStep branch3 = new("Branch3", ledger, clock);

        PipelineFactory factory = new(NullPipelineProgressNotifier.Instance, TimeProvider.System);
        PipelineStepOptions orderedParallel = new(maxWorkers: 2, maxBufferSize: 8, keepOrder: true);

        IDataflowPipeline<CurveSegment> pipeline = factory
            .CreateDataflow<CurveSegment>(NullLogger.Instance, "accept-3way-fork", initialBufferSize: 16)
            .AddStep(tagStep)
            .Fork(branch1, branch2, branch3, orderedParallel)
            .BuildTerminal("CollectTriples", item => joinedResults.Add(item));

        foreach (CurveSegment segment in segments)
        {
            Assert.True(await pipeline.SendAsync(segment));
        }

        pipeline.Complete();
        await pipeline.Completion;
        await pipeline.DisposeAsync();

        Assert.Equal(segments.Length, joinedResults.Count);
        for (int i = 0; i < segments.Length; i++)
        {
            Assert.Equal(i + 1, joinedResults[i].Item1.OriginId);
            Assert.Equal($"Branch2:{i + 1}", joinedResults[i].Item2);
            Assert.Equal(segments[i].TimePoints.Length, joinedResults[i].Item3);
        }

        Assert.True(tagStep.Disposed);
        Assert.True(branch1.AsyncDisposed);
        Assert.True(branch2.AsyncDisposed);
        Assert.True(branch3.Disposed);
    }
}
