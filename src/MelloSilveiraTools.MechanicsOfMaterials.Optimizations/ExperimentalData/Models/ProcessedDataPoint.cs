namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Models;

public readonly record struct ProcessedDataPoint(
    double Time,
    double Strain,
    double StrainRate,
    double StrainAcceleration,
    double Stress,
    double StressRate,
    double StressAcceleration);
