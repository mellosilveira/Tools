using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Models.CurveFitting;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Models.ExperimentalData;

public readonly record struct ProcessedDataPoint(
    double Time,
    double Strain,
    double StrainRate,
    double StrainAcceleration,
    double Stress,
    double StressRate,
    double StressAcceleration)
{
    public static implicit operator ExperimentalDataPoint(ProcessedDataPoint point) => new(point.Time, point.Strain, point.Stress);
}
