namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.Models;

/// <summary>
/// Final structured payload after mathematical derivations (Absolute and Percentage Deltas).
/// Consumed by the I/O layer for CSV generation.
/// </summary>
public record ParametricSweepFinalPayload(
    string VariableName,
    double VariableValue,
    double InitialStress,
    double FinalStress,
    double AsymptoteTime,
    double AsymptoteStress,
    double AbsoluteDeltaStress,
    double PercentageDeltaStress);
