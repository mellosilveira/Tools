namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Models.CurveFitting;

/// <summary>
/// Represents an experimental data point containing time, stress, and strain.
/// Uses 'record struct' to avoid Heap allocations during massive reading.
/// </summary>
public readonly record struct ExperimentalDataPoint(double Time, double Stress, double Strain);