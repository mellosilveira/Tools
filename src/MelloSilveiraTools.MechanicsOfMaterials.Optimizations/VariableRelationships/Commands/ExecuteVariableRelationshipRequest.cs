using MelloSilveiraTools.Core.Application.Commands;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.VariableRelationships.Models;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.VariableRelationships.Commands;

/// <summary>
/// Request payload for executing a Variable Relationship and Iso-Stress exploration via the Command pattern.
/// </summary>
public record ExecuteVariableRelationshipRequest : RequestBase
{
    /// <summary>
    /// Gets the registered name of the constitutive mechanical model (e.g., "Fung", "SimplifiedFung").
    /// </summary>
    public required string MechanicalModelName { get; init; }

    /// <summary>
    /// Gets the target mechanical stress value to be matched.
    /// </summary>
    public required double TargetStress { get; init; }

    /// <summary>
    /// Gets the specific simulation time at which stress is evaluated.
    /// </summary>
    public required double TargetTime { get; init; }

    /// <summary>
    /// Gets the allowable absolute tolerance threshold for stress deviation. Defaults to 1e-4.
    /// </summary>
    public double Tolerance { get; init; } = 1e-4;

    /// <summary>
    /// Gets the collection of parameter ranges and steps defining the parameter search space.
    /// </summary>
    public required IReadOnlyList<ParameterBound> ParameterBounds { get; init; }

    /// <summary>
    /// Converts this command request into a <see cref="VariableRelationshipInput"/> instance.
    /// </summary>
    /// <returns>A configured <see cref="VariableRelationshipInput"/>.</returns>
    public VariableRelationshipInput ToInput() => new(
        MechanicalModelName,
        TargetStress,
        TargetTime,
        Tolerance,
        ParameterBounds);
}
