using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
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
/// Acceptance tests validating TPL Dataflow DAG topology correlation, real-time progress emission via
/// <see cref="ChannelPipelineProgressNotifier"/>, bounded backpressure, Dead-Letter Queue (DLQ) fault isolation,
/// and guaranteed step disposal upon unhandled exceptions.
/// </summary>
public sealed class DataflowBackpressureDlqAndTelemetryAcceptanceTests
{
    private sealed class GroupFanOutStep : IAsyncEnumerablePipelineStep<(string GroupName, CurveSegment[] Segments), (string GroupName, CurveSegment Segment)>
    {
        public string Name => nameof(GroupFanOutStep);
        public bool Disposed { get; private set; }

        public async IAsyncEnumerable<(string GroupName, CurveSegment Segment)> ExecuteAsync(
            (string GroupName, CurveSegment[] Segments) input,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            foreach (CurveSegment segment in input.Segments)
            {
                yield return (input.GroupName, segment);
            }
        }

        public ValueTask DisposeAsync()
        {
            Disposed = true;
            return ValueTask.CompletedTask;
        }
    }

    private sealed class SlowConsumerStep(int delayMs, string? failOnGroupName = null)
        : IAsyncPipelineStep<(string GroupName, CurveSegment[] Segments), (string GroupName, int SegmentCount)>
    {
        public string Name => nameof(SlowConsumerStep);
        public bool Disposed { get; private set; }

        public async Task<(string GroupName, int SegmentCount)> ExecuteAsync(
            (string GroupName, CurveSegment[] Segments) input,
            CancellationToken cancellationToken = default)
        {
            await Task.Delay(delayMs, cancellationToken).ConfigureAwait(false);
            if (failOnGroupName is not null && string.Equals(input.GroupName, failOnGroupName, StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"Simulated deterministic fault on specimen group '{input.GroupName}'.");
            }

            return (input.GroupName, input.Segments.Length);
        }

        public ValueTask DisposeAsync()
        {
            Disposed = true;
            return ValueTask.CompletedTask;
        }
    }

    private sealed class MeanStrainBranchStep : ISyncPipelineStep<(string GroupName, CurveSegment Segment), (string GroupName, double MeanStrain)>
    {
        public string Name => nameof(MeanStrainBranchStep);
        public bool Disposed { get; private set; }

        public (string GroupName, double MeanStrain) Execute((string GroupName, CurveSegment Segment) input)
            => (input.GroupName, input.Segment.ExperimentalStrain.Average());

        public void Dispose() => Disposed = true;
    }

    private sealed class PointCountBranchStep : ISyncPipelineStep<(string GroupName, CurveSegment Segment), int>
    {
        public string Name => nameof(PointCountBranchStep);
        public bool Disposed { get; private set; }

        public int Execute((string GroupName, CurveSegment Segment) input) => input.Segment.TimePoints.Length;

        public void Dispose() => Disposed = true;
    }

    private sealed class MergeGroupedMetricsStep : ISyncPipelineStep<IGrouping<string, Tuple<(string GroupName, double MeanStrain), int>>, (string GroupName, double MeanStrain, int TotalPoints)>
    {
        public string Name => nameof(MergeGroupedMetricsStep);
        public bool Disposed { get; private set; }

        public (string GroupName, double MeanStrain, int TotalPoints) Execute(IGrouping<string, Tuple<(string GroupName, double MeanStrain), int>> input)
            => (input.Key, input.Average(x => x.Item1.MeanStrain), input.Sum(x => x.Item2));

        public void Dispose() => Disposed = true;
    }

    /// <summary>
    /// Verifies that <see cref="IDataflowPipeline{TIn}.Topology"/> captures upstream dependencies accurately across
    /// streaming, forking, and grouping stages, and that <see cref="ChannelPipelineProgressNotifier"/> delivers
    /// lifecycle state transitions and consistent item counters for every step.
    /// </summary>
    [Fact]
    public async Task RealTimeTelemetryAndTopology_WithSharedSpecimenGroups_EmitsAccurateDagAndCounters()
    {
        IReadOnlyList<(string GroupName, CurveSegment[] Segments)> groups = await AcceptanceTestHelpers.LoadSegmentGroupsForPrefixAsync("SchaperyRelaxationOnly", useSharedFiles: true);
        Assert.True(groups.Count >= 5);

        await using ChannelPipelineProgressNotifier notifier = new(
            TimeProvider.System,
            new PipelineProgressNotifierSettings
            {
                ChannelCapacity = 512,
                FullMode = BoundedChannelFullMode.Wait,
                MaxChannels = 8
            });

        PipelineFactory factory = new(notifier, TimeProvider.System);
        string pipelineId = $"telemetry-{Guid.NewGuid():N}";
        ConcurrentBag<(string GroupName, double MeanStrain, int TotalPoints)> terminalOutputs = [];

        GroupFanOutStep fanOutStep = new();
        MeanStrainBranchStep branch1Step = new();
        PointCountBranchStep branch2Step = new();
        MergeGroupedMetricsStep mergeStep = new();

        await using IDataflowPipeline<(string GroupName, CurveSegment[] Segments)> pipeline = factory
            .CreateDataflow<(string GroupName, CurveSegment[] Segments)>(NullLogger.Instance, pipelineId, initialBufferSize: 16)
            .AddStep(fanOutStep)
            .Fork(branch1Step, branch2Step)
            .AddGroupWhileStep(tuple => tuple.Item1.GroupName)
            .AddStep(mergeStep)
            .BuildTerminal("RecordMetrics", summary => terminalOutputs.Add(summary));

        // 1. Validate Topology DAG dependencies
        Assert.Equal(4, pipeline.Topology.Count);
        Assert.Contains(pipeline.Topology, node => node.StepName == nameof(GroupFanOutStep) && node.UpstreamSteps.Length == 0);
        Assert.Contains(pipeline.Topology, node => node.StepName == nameof(MeanStrainBranchStep) && node.UpstreamSteps.SequenceEqual([nameof(GroupFanOutStep)]));
        Assert.Contains(pipeline.Topology, node => node.StepName == nameof(PointCountBranchStep) && node.UpstreamSteps.SequenceEqual([nameof(GroupFanOutStep)]));
        Assert.Contains(pipeline.Topology, node => node.StepName == nameof(MergeGroupedMetricsStep)
            && node.UpstreamSteps.Contains(nameof(MeanStrainBranchStep))
            && node.UpstreamSteps.Contains(nameof(PointCountBranchStep)));

        // 2. Execute all specimen groups
        foreach ((string GroupName, CurveSegment[] Segments) group in groups)
        {
            Assert.True(await pipeline.SendAsync(group));
        }

        pipeline.Complete();
        await pipeline.Completion;

        List<PipelineProgressEvent> events = [];
        await foreach (PipelineProgressEvent evt in notifier.ReadAllAsync(pipelineId))
        {
            events.Add(evt);
        }

        int totalSegments = groups.Sum(g => g.Segments.Length);
        Assert.Equal(groups.Count, terminalOutputs.Count);
        Assert.Equal(totalSegments, groups.Sum(g => g.Segments.Length));
        Assert.NotEmpty(events);
        Assert.DoesNotContain(events, e => e.State == PipelineStepState.Faulted);

        // Ingestion head events: Started and Completed with final count == groups.Count
        List<PipelineProgressEvent> ingestionEvents = [.. events.Where(e => e.StepName.Contains("SendAsync", StringComparison.Ordinal))];
        Assert.Contains(ingestionEvents, e => e.State == PipelineStepState.Started);
        Assert.Contains(ingestionEvents, e => e.State == PipelineStepState.Completed && e.ReceivedItems == groups.Count && e.ProcessedItems == groups.Count);

        // Streaming fan-out step events: Started, Running (per yielded item), and Completed
        List<PipelineProgressEvent> fanOutEvents = [.. events.Where(e => e.StepName == nameof(GroupFanOutStep))];
        Assert.Contains(fanOutEvents, e => e.State == PipelineStepState.Started);
        Assert.Contains(fanOutEvents, e => e.State == PipelineStepState.Running);
        Assert.Contains(fanOutEvents, e => e.State == PipelineStepState.Completed && e.ReceivedItems == groups.Count);

        // Fork branch steps: Started and Running with ReceivedItems == ProcessedItems == totalSegments
        Assert.Contains(events, e => e.StepName == nameof(MeanStrainBranchStep) && e.State == PipelineStepState.Running && e.ReceivedItems == totalSegments && e.ProcessedItems == totalSegments);
        Assert.Contains(events, e => e.StepName == nameof(PointCountBranchStep) && e.State == PipelineStepState.Running && e.ReceivedItems == totalSegments && e.ProcessedItems == totalSegments);

        // Terminal consumer step events: Started and Running with ReceivedItems == ProcessedItems == groups.Count
        List<PipelineProgressEvent> terminalEvents = [.. events.Where(e => e.StepName == "RecordMetrics")];
        Assert.Contains(terminalEvents, e => e.State == PipelineStepState.Started);
        Assert.Contains(terminalEvents, e => e.State == PipelineStepState.Running && e.ReceivedItems == groups.Count && e.ProcessedItems == groups.Count);
    }

    /// <summary>
    /// Verifies that bounded backpressure (<see cref="PipelineStepOptions.MaxBufferSize"/> = 2) and Dead-Letter Queue
    /// routing isolate faults on a specific specimen group while allowing all healthy groups to complete, and that
    /// non-DLQ pipeline faults emit <see cref="PipelineStepState.Faulted"/> and still dispose all registered steps.
    /// </summary>
    [Fact]
    public async Task BackpressureAndDeadLetterQueue_IsolatesFaultsAndGuaranteesDisposalOnFailure()
    {
        IReadOnlyList<(string GroupName, CurveSegment[] Segments)> groups = await AcceptanceTestHelpers.LoadSegmentGroupsForPrefixAsync("SchaperyRelaxationOnly", useSharedFiles: true);
        Assert.True(groups.Count >= 5);

        const string FailingGroupName = "LCL_08_07_21_L32_D9";
        Assert.Contains(groups, g => g.GroupName == FailingGroupName);

        await using ChannelPipelineProgressNotifier notifier = new(
            TimeProvider.System,
            new PipelineProgressNotifierSettings
            {
                ChannelCapacity = 512,
                FullMode = BoundedChannelFullMode.Wait,
                MaxChannels = 8
            });

        PipelineFactory factory = new(notifier, TimeProvider.System);
        ConcurrentBag<FailedPayload> dlqPayloads = [];
        ConcurrentBag<(string GroupName, int SegmentCount)> healthyOutputs = [];
        SlowConsumerStep dlqStep = new(delayMs: 15, failOnGroupName: FailingGroupName);

        // 1. Bounded backpressure (MaxBufferSize = 2) + DLQ fault isolation
        string dlqPipelineId = $"dlq-{Guid.NewGuid():N}";
        await using (IDataflowPipeline<(string GroupName, CurveSegment[] Segments)> dlqPipeline = factory
            .CreateDataflow<(string GroupName, CurveSegment[] Segments)>(NullLogger.Instance, dlqPipelineId, initialBufferSize: 2)
            .WithDeadLetterQueue((FailedPayload fp, CancellationToken _) =>
            {
                dlqPayloads.Add(fp);
                return Task.CompletedTask;
            })
            .AddStep(dlqStep, new PipelineStepOptions(maxWorkers: 1, maxBufferSize: 2, keepOrder: true))
            .BuildTerminal("HealthyTerminal", item => healthyOutputs.Add(item)))
        {
            foreach ((string GroupName, CurveSegment[] Segments) group in groups)
            {
                Assert.True(await dlqPipeline.SendAsync(group));
            }

            dlqPipeline.Complete();
            await dlqPipeline.Completion;
        }

        Assert.True(dlqStep.Disposed);
        FailedPayload capturedFailure = Assert.Single(dlqPayloads);
        Assert.Equal(nameof(SlowConsumerStep), capturedFailure.CallbackName);
        Assert.IsType<InvalidOperationException>(capturedFailure.Exception);
        (string GroupName, CurveSegment[] Segments) failedTuple = Assert.IsType<(string GroupName, CurveSegment[] Segments)>(capturedFailure.Payload);
        Assert.Equal(FailingGroupName, failedTuple.GroupName);

        Assert.Equal(groups.Count - 1, healthyOutputs.Count);
        Assert.DoesNotContain(healthyOutputs, o => o.GroupName == FailingGroupName);

        // 2. Non-DLQ pipeline faulting: verify Faulted progress event AND step disposal in finally
        SlowConsumerStep faultingStep = new(delayMs: 0, failOnGroupName: FailingGroupName);
        string faultPipelineId = $"fault-{Guid.NewGuid():N}";

        IDataflowPipeline<(string GroupName, CurveSegment[] Segments)> faultingPipeline = factory
            .CreateDataflow<(string GroupName, CurveSegment[] Segments)>(NullLogger.Instance, faultPipelineId, initialBufferSize: 8)
            .AddStep(faultingStep)
            .BuildTerminal("FaultSink");

        foreach ((string GroupName, CurveSegment[] Segments) group in groups)
        {
            await faultingPipeline.SendAsync(group);
        }

        faultingPipeline.Complete();
        AggregateException aggEx = await Assert.ThrowsAsync<AggregateException>(async () => await faultingPipeline.DisposeAsync());
        Assert.IsType<InvalidOperationException>(aggEx.GetBaseException());

        Assert.True(faultingStep.Disposed);

        List<PipelineProgressEvent> faultEvents = [];
        Exception? channelCompletionEx = await Record.ExceptionAsync(async () =>
        {
            await foreach (PipelineProgressEvent evt in notifier.ReadAllAsync(faultPipelineId))
            {
                faultEvents.Add(evt);
            }
        });

        Assert.NotNull(channelCompletionEx);
        Assert.IsType<InvalidOperationException>(channelCompletionEx.GetBaseException());
        Assert.Contains(faultEvents, e => e.State == PipelineStepState.Faulted && e.StepName == nameof(SlowConsumerStep));
    }
}
