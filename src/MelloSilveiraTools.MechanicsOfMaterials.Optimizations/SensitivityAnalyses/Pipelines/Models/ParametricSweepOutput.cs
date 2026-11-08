namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.Pipelines.Models;

/// <summary>
/// Final structured payload after mathematical derivations (Absolute and Percentage Deltas).
/// Consumed by the I/O layer for CSV generation.
/// </summary>
public record ParametricSweepOutput(
    string VariableName,
    double VariableValue,
    double InitialStress,
    double FinalStress,
    double AsymptoteTime,
    double AsymptoteError,
    double AsymptoteStress,
    double AbsoluteDeltaStress,
    double PercentageDeltaStress);
