using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Models;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Models;

public readonly record struct SegmentedDataPoint(SegmentType SegmentType, ProcessedDataPoint ProcessedDataPoint)
{
    public static implicit operator ExperimentalDataPoint(SegmentedDataPoint point) => point.ProcessedDataPoint;
    public static implicit operator ProcessedDataPoint(SegmentedDataPoint point) => point.ProcessedDataPoint;
}
