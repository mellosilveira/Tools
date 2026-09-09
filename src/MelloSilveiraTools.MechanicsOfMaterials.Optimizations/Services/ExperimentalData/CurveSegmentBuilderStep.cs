using MelloSilveiraTools.Core.Pipelines.Steps;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Models.CurveFitting;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Models.ExperimentalData;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Services.ExperimentalData;

/// <summary>
/// Pipeline step responsible for constructing <see cref="CurveSegment"/> instances from grouped <see cref="SegmentedDataPoint"/> arrays,
/// applying downsampling thresholds to minimize redundant data points.
/// </summary>
/// <param name="skipTimeStep">The minimum time interval required between consecutive points within a segment. Defaults to 0.0 (no downsampling).</param>
public sealed class CurveSegmentBuilderStep(double skipTimeStep) : ISyncPipelineStep<SegmentedDataPoint[], CurveSegment>
{
    /// <inheritdoc/>
    public string Name => "CurveSegmentBuilder";

    /// <summary>
    /// Gets the minimum time interval threshold used for downsampling.
    /// </summary>
    public double SkipTimeStep => skipTimeStep;

    /// <inheritdoc/>
    public CurveSegment Execute(SegmentedDataPoint[] input)
    {
        ArgumentNullException.ThrowIfNull(input);

        if (input.Length == 0)
        {
            return new CurveSegment
            {
                Type = SegmentType.Unknown,
                TimePoints = [],
                ExperimentalStrain = [],
                ExperimentalStress = []
            };
        }

        SegmentType segmentType = input[0].SegmentType;
        List<double> timePoints = [];
        List<double> strainPoints = [];
        List<double> stressPoints = [];

        double? lastTime = null;
        for (int i = 0; i < input.Length; i++)
        {
            ProcessedDataPoint point = input[i].ProcessedDataPoint;
            if (lastTime is null || (point.Time - lastTime.Value) >= skipTimeStep || i == input.Length - 1)
            {
                timePoints.Add(point.Time);
                strainPoints.Add(point.Strain);
                stressPoints.Add(point.Stress);
                lastTime = point.Time;
            }
        }

        return new CurveSegment
        {
            Type = segmentType,
            TimePoints = [.. timePoints],
            ExperimentalStrain = [.. strainPoints],
            ExperimentalStress = [.. stressPoints]
        };
    }

    /// <inheritdoc/>
    public void Dispose() { }
}
