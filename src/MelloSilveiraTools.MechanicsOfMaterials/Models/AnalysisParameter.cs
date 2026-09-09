namespace MelloSilveiraTools.MechanicsOfMaterials.Models;

/// <summary>
/// Represents an analysis parameter consisting of an initial value for a dependent mechanical parameter 
/// and an independent mechanical parameter.
/// </summary>
public record AnalysisParameter
{
    /// <summary>
    /// Initial value for the dependent mechanical parameter.
    /// </summary>
    public double InitialDependentMechanicalParameterValue { get; init; }

    /// <summary>
    /// Independent mechanical parameter.
    /// </summary>
    public MechanicalParameter IndependentMechanicalParameter { get; init; } = new();
}
