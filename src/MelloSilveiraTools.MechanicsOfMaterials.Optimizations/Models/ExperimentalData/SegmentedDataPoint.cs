using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Models.CurveFitting;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Models.ExperimentalData;

public readonly record struct SegmentedDataPoint(SegmentType SegmentType, ProcessedDataPoint ProcessedDataPoint)
{
    public static implicit operator ExperimentalDataPoint(SegmentedDataPoint point) => point.ProcessedDataPoint;
    public static implicit operator ProcessedDataPoint(SegmentedDataPoint point) => point.ProcessedDataPoint;
}
