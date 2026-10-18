using MelloSilveiraTools.Mathematics.Extensions;

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
    double PercentageDeltaStress)
{
    public static ParametricSweepFinalPayload Create(
        Tuple<double, double, (double AsymptoteTime, double Stress)> calculation,
        (string VariableName, double VariableValue) variable) =>
        new(
            variable.VariableName,
            variable.VariableValue,
            calculation.Item1,
            calculation.Item2,
            calculation.Item3.AsymptoteTime,
            calculation.Item3.Stress,
            calculation.Item2 - calculation.Item1,
            calculation.Item1.PercentageDifference(calculation.Item2));
}
