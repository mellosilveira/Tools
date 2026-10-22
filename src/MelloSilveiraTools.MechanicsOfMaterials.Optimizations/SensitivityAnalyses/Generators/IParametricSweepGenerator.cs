using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.Models;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.Generators;

/// <summary>
/// Core contract for specific constitutive mechanical model sweep generators.
/// Inheritors must implement the One-At-A-Time (OAAT) variation logic specific to their parameter type.
/// </summary>
public interface IParametricSweepGenerator
{
    /// <summary>
    /// Generates combinations iterating over parameters one-at-a-time (OAAT) against a static baseline.
    /// </summary>
    /// <param name="input">The parametric sweep input defining ranges and limits.</param>
    /// <returns>A stream of generated simulation envelopes.</returns>
    IEnumerable<SweepSimulationEnvelope> GenerateCombinations(ParametricSweepInput input);
}
