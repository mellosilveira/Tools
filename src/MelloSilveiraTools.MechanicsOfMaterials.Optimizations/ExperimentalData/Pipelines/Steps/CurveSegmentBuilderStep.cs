using MelloSilveiraTools.Core.Pipelines.Steps;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Models;
using System.Runtime.CompilerServices;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Pipelines.Steps;

/// <summary>
/// Pipeline step responsible for accumulating <see cref="SegmentedDataPoint"/> sequentially and yielding a <see cref="CurveSegment"/> 
/// when the <see cref="SegmentType"/> changes.
/// </summary>
public sealed class CurveSegmentBuilderStep : IAsyncEnumerablePipelineStep<SegmentedDataPoint, CurveSegment>
{
    private SegmentType _currentType = SegmentType.Unknown;
    private readonly List<double> _timePoints = [];
    private readonly List<double> _strainPoints = [];
    private readonly List<double> _stressPoints = [];

    /// <inheritdoc/>
    public string Name => nameof(CurveSegmentBuilderStep);

    /// <inheritdoc/>
    public async IAsyncEnumerable<CurveSegment> ExecuteAsync(SegmentedDataPoint input, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (input.SegmentType != _currentType)
        {
            if (_timePoints.Count > 0)
            {
                yield return new CurveSegment
                {
                    Type = _currentType,
                    TimePoints = [.. _timePoints],
                    ExperimentalStrain = [.. _strainPoints],
                    ExperimentalStress = [.. _stressPoints]
                };

                _timePoints.Clear();
                _strainPoints.Clear();
                _stressPoints.Clear();
            }

            _currentType = input.SegmentType;
        }

        if (input.SegmentType != SegmentType.Unknown)
        {
            _timePoints.Add(input.ProcessedDataPoint.Time);
            _strainPoints.Add(input.ProcessedDataPoint.Strain);
            _stressPoints.Add(input.ProcessedDataPoint.Stress);
        }

        await Task.CompletedTask;
    }

    /// <inheritdoc/>
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
