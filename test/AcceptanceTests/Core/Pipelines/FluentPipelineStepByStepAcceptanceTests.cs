using AcceptanceTests.MechanicalOfMaterials.Optimizations.CurveFitterSteps;
using MelloSilveiraTools.Core.Pipelines;
using MelloSilveiraTools.Core.Pipelines.Fluent;
using MelloSilveiraTools.Core.Pipelines.Models;
using MelloSilveiraTools.Core.Pipelines.Notifiers;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Models;
using Microsoft.Extensions.Logging.Abstractions;

namespace AcceptanceTests.Core.Pipelines;

/// <summary>
/// Acceptance tests validating step-by-step execution, chronological state transitions, cooperative cancellation,
/// and deterministic exception wrapping in the pull-based Fluent pipeline engine (<see cref="PipelineFactory.StartFluent{T}"/>)
/// over real experimental CSV curve segments.
/// </summary>
public sealed class FluentPipelineStepByStepAcceptanceTests
{
    private readonly PipelineFactory _factory = new(NullPipelineProgressNotifier.Instance, TimeProvider.System);

    private sealed record SegmentSummary(
        string SourceDataset,
        int SegmentCount,
        double PeakStress,
        double FinalStress,
        double MaxStrain,
        IReadOnlyList<string> ExecutedSteps);

    /// <summary>
    /// Verifies that a multi-step Fluent pipeline executes synchronous and asynchronous delegates sequentially
    /// on real <c>FungRelaxationOnly</c> and <c>SimplifiedFungRelaxationOnly</c> CSV segments without skipping steps.
    /// </summary>
    [Theory]
    [InlineData("FungRelaxationOnly")]
    [InlineData("SimplifiedFungRelaxationOnly")]
    public async Task StartFluent_WithRealDatasetSegments_ExecutesAllStepsSequentiallyAndComputesMetrics(string datasetPrefix)
    {
        IReadOnlyList<(string GroupName, CurveSegment[] Segments)> groups = await AcceptanceTestHelpers.LoadSegmentGroupsForPrefixAsync(datasetPrefix, useSharedFiles: false);
        Assert.NotEmpty(groups);

        CurveSegment[] inputSegments = groups[0].Segments;
        Assert.NotEmpty(inputSegments);

        List<string> ledger = [];

        await using IFluentPipeline<CurveSegment[], SegmentSummary> pipeline = _factory
            .StartFluent<CurveSegment[]>()
            .AddStep("Step1_ValidateAndFilterNonEmpty", segments =>
            {
                ledger.Add("Step1_ValidateAndFilterNonEmpty");
                return segments.Where(s => s.TimePoints.Length > 0).ToArray();
            })
            .AddStep("Step2_ExtractRelaxationMetricsAsync", async (CurveSegment[] segments, CancellationToken ct) =>
            {
                await Task.Yield();
                ct.ThrowIfCancellationRequested();
                ledger.Add("Step2_ExtractRelaxationMetricsAsync");

                double peakStress = segments.SelectMany(s => s.ExperimentalStress).Max();
                double maxStrain = segments.SelectMany(s => s.ExperimentalStrain).Max();
                double finalStress = segments[^1].ExperimentalStress[^1];
                return new SegmentSummary(datasetPrefix, segments.Length, peakStress, finalStress, maxStrain, [.. ledger]);
            })
            .AddStep("Step3_AppendAuditTrail", summary =>
            {
                ledger.Add("Step3_AppendAuditTrail");
                return summary with { ExecutedSteps = [.. ledger] };
            })
            .Build(NullLogger.Instance);

        SegmentSummary result = await pipeline.ExecuteAsync(inputSegments, CancellationToken.None);

        Assert.Equal(datasetPrefix, result.SourceDataset);
        Assert.Equal(inputSegments.Length, result.SegmentCount);
        Assert.True(result.PeakStress > 0.0);
        Assert.True(result.MaxStrain > 0.0);
        Assert.Equal(["Step1_ValidateAndFilterNonEmpty", "Step2_ExtractRelaxationMetricsAsync", "Step3_AppendAuditTrail"], result.ExecutedSteps);
    }

    /// <summary>
    /// Verifies that mid-chain cancellation halts subsequent steps with <see cref="OperationCanceledException"/>
    /// and that unhandled step failures wrap the failing step name in <see cref="PipelineExecutionException"/>.
    /// </summary>
    [Fact]
    public async Task StartFluent_MidChainCancellationAndFault_HaltDownstreamStepsAndPreserveContext()
    {
        IReadOnlyList<(string GroupName, CurveSegment[] Segments)> groups = await AcceptanceTestHelpers.LoadSegmentGroupsForPrefixAsync("FungRelaxationOnly", useSharedFiles: false);
        CurveSegment[] inputSegments = groups[0].Segments;

        // 1. Mid-chain cooperative cancellation
        using CancellationTokenSource cts = new();
        List<string> executedBeforeCancel = [];

        await using IFluentPipeline<CurveSegment[], CurveSegment[]> cancelPipeline = _factory
            .StartFluent<CurveSegment[]>()
            .AddStep("CancelStep1", segments =>
            {
                executedBeforeCancel.Add("CancelStep1");
                cts.Cancel();
                return segments;
            })
            .AddStep("CancelStep2_ShouldBeSkipped", async (CurveSegment[] segments, CancellationToken ct) =>
            {
                await Task.Yield();
                ct.ThrowIfCancellationRequested();
                executedBeforeCancel.Add("CancelStep2_ShouldBeSkipped");
                return segments;
            })
            .Build(NullLogger.Instance);

        await Assert.ThrowsAsync<OperationCanceledException>(() => cancelPipeline.ExecuteAsync(inputSegments, cts.Token));
        Assert.Equal(["CancelStep1"], executedBeforeCancel);

        // 2. Mid-chain step fault wrapping
        List<string> executedBeforeFault = [];

        await using IFluentPipeline<CurveSegment[], CurveSegment[]> faultPipeline = _factory
            .StartFluent<CurveSegment[]>()
            .AddStep("FaultStep1", segments =>
            {
                executedBeforeFault.Add("FaultStep1");
                return segments;
            })
            .AddStep<CurveSegment[]>("FaultStep2_InvalidData", _ =>
            {
                executedBeforeFault.Add("FaultStep2_InvalidData");
                throw new InvalidOperationException("Simulated Fluent pipeline error.");
            })
            .AddStep("FaultStep3_Unreachable", segments =>
            {
                executedBeforeFault.Add("FaultStep3_Unreachable");
                return segments;
            })
            .Build(NullLogger.Instance);

        PipelineExecutionException ex = await Assert.ThrowsAsync<PipelineExecutionException>(() => faultPipeline.ExecuteAsync(inputSegments));
        Assert.Equal("FaultStep2_InvalidData", ex.StepName);
        Assert.IsType<InvalidOperationException>(ex.InnerException);
        Assert.Equal(["FaultStep1", "FaultStep2_InvalidData"], executedBeforeFault);
    }
}
