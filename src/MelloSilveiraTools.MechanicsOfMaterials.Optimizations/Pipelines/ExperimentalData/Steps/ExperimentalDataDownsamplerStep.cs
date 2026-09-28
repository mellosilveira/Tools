using System.Runtime.CompilerServices;
using MelloSilveiraTools.Core.Pipelines.Steps;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Models;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Steps;

/// <summary>
/// Pipeline step responsible for downsampling a stream of <see cref="SegmentedDataPoint"/> by skipping points that are too close in time.
/// </summary>
public sealed class ExperimentalDataDownsamplerStep(double skipTimeStep) : IAsyncEnumerablePipelineStep<SegmentedDataPoint, SegmentedDataPoint>
{
    private double? _lastTime = null;

    /// <inheritdoc/>
    public string Name => nameof(ExperimentalDataDownsamplerStep);

    /// <inheritdoc/>
    public async IAsyncEnumerable<SegmentedDataPoint> ExecuteAsync(SegmentedDataPoint input, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Always pass through the dummy Unknown end-of-stream point
        if (input.SegmentType == SegmentType.Unknown)
        {
            yield return input;
            yield break;
        }

        if (_lastTime is null || (input.ProcessedDataPoint.Time - _lastTime.Value) >= skipTimeStep)
        {
            _lastTime = input.ProcessedDataPoint.Time;
            yield return input;
        }

        await Task.CompletedTask;
    }

    /// <inheritdoc/>
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
