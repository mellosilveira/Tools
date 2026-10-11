namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.Models;

/// <summary>
/// Holds the raw numerical outputs of the three parallel mathematical calculations.
/// </summary>
public record SimulationMetrics(
    double InitialStress,
    double FinalStress,
    double AsymptoteTime,
    double AsymptoteStress);
