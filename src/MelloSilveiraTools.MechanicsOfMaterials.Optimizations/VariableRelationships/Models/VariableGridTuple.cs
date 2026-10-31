namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.VariableRelationships.Models;

/// <summary>
/// Intermediate pipeline envelope encapsulating a discretized tuple of free parameters,
/// the designated dependent parameter bound, and input configuration for 1D root-finding.
/// </summary>
/// <param name="FreeParameters">The dictionary of fixed free parameter values evaluated in the current grid coordinate.</param>
/// <param name="DependentBound">The boundary definition of the dependent parameter to be resolved via root-finding.</param>
/// <param name="AllBounds">The complete collection of parameter bounds defining the search space.</param>
/// <param name="Input">The root variable relationship input criteria.</param>
public sealed record VariableGridTuple(
    IReadOnlyDictionary<string, double> FreeParameters,
    ParameterBound DependentBound,
    IReadOnlyList<ParameterBound> AllBounds,
    VariableRelationshipInput Input);
