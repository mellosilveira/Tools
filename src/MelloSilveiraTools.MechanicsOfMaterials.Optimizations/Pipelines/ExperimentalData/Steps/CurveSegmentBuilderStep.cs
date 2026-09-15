using MelloSilveiraTools.Core.Pipelines.Steps;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Models.CurveFitting;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Models.ExperimentalData;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Steps;

/// <summary>
/// Pipeline step responsible for constructing <see cref="CurveSegment"/> instances from grouped <see cref="SegmentedDataPoint"/> arrays,
/// applying downsampling thresholds to minimize redundant data points.
/// </summary>
public sealed class CurveSegmentBuilderStep() : ISyncPipelineStep<CurveSegmentBuilderInput, CurveSegment>
{
    /// <inheritdoc/>
    public string Name => "CurveSegmentBuilder";

    /// <inheritdoc/>
    public CurveSegment Execute(CurveSegmentBuilderInput input)
    {
        SegmentedDataPoint[] points = input.Points;
        if (points.Length == 0)
        {
            return new CurveSegment
            {
                Type = SegmentType.Unknown,
                TimePoints = [],
                ExperimentalStrain = [],
                ExperimentalStress = []
            };
        }

        SegmentType segmentType = points[0].SegmentType;
        List<double> timePoints = [];
        List<double> strainPoints = [];
        List<double> stressPoints = [];

        double? lastTime = null;
        for (int i = 0; i < points.Length; i++)
        {
            ProcessedDataPoint point = points[i].ProcessedDataPoint;
            if (lastTime is null || (point.Time - lastTime.Value) >= input.SkipTimeStep || i == points.Length - 1)
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
    public void Dispose() 
    {
        GC.SuppressFinalize(this);
    }
}
