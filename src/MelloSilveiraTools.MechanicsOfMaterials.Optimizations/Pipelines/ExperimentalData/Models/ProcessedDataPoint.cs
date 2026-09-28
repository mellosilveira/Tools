namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Models;

public readonly record struct ProcessedDataPoint(
    double Time,
    double Strain,
    double StrainRate,
    double StrainAcceleration,
    double Stress,
    double StressRate,
    double StressAcceleration);
