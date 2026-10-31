namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.VariableRelationships.Models;

/// <summary>
/// Defines numerical search boundaries and discretization step for a constitutive model parameter
/// within variable relationship analyses.
/// </summary>
/// <param name="ParameterName">The unique name or key of the constitutive parameter (e.g., "C1", "C2", "E").</param>
/// <param name="MinValue">The inclusive lower bound value for parameter exploration.</param>
/// <param name="MaxValue">The inclusive upper bound value for parameter exploration.</param>
/// <param name="Step">The positive step increment used to discretize the search interval.</param>
public sealed record ParameterBound(string ParameterName, double MinValue, double MaxValue, double Step);
