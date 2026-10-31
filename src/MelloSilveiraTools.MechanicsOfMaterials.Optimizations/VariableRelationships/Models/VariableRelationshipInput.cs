namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.VariableRelationships.Models;

/// <summary>
/// Input specification for exploring relationships and combinations between constitutive parameters
/// that satisfy a designated stress criterion at a specific evaluation time.
/// </summary>
/// <param name="MechanicalModelName">The registered name of the constitutive mechanical model (e.g., "Fung", "SimplifiedFung").</param>
/// <param name="TargetStress">The target mechanical stress value to be matched.</param>
/// <param name="TargetTime">The specific simulation time at which stress is evaluated.</param>
/// <param name="Tolerance">The allowable absolute tolerance threshold for stress deviation.</param>
/// <param name="ParameterBounds">The collection of parameter ranges and steps defining the parameter search space.</param>
public sealed record VariableRelationshipInput(
    string MechanicalModelName,
    double TargetStress,
    double TargetTime,
    double Tolerance,
    IReadOnlyList<ParameterBound> ParameterBounds);
