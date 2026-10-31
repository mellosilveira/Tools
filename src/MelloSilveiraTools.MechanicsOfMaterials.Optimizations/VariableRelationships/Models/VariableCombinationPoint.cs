namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.VariableRelationships.Models;

/// <summary>
/// Represents an evaluated parameter combination point and its corresponding stress response and residual
/// with respect to a target state.
/// </summary>
/// <param name="Parameters">The mapped dictionary of parameter names and their evaluated values.</param>
/// <param name="CalculatedStress">The forward-simulated stress value obtained from the constitutive model.</param>
/// <param name="Residual">The residual difference between the calculated stress and target stress.</param>
public sealed record VariableCombinationPoint(
    IReadOnlyDictionary<string, double> Parameters,
    double CalculatedStress,
    double Residual);
