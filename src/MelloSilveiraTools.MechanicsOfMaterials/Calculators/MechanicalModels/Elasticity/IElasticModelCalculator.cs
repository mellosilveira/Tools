using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Elasticity;

namespace MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels.Elasticity;

/// <summary>
/// Assumes infinitesimal strains and linear relationship between stress and strain. 
/// Valid only for stress states that do not produce yielding.
/// </summary>
public interface IElasticModelCalculator : IMechanicalModelCalculator<ElasticModelInput>
{
    /// <summary>
    /// Calculates the stiffness through displacement.
    /// </summary>
    /// <param name="input">The mechanical model's input.</param>
    /// <param name="time">Unit: s (seconds).</param>
    /// <returns></returns>
    double CalculateStiffnessThroughDisplacement(ElasticModelInput input, double time);

    /// <summary>
    /// Calculates the stiffness through force.
    /// </summary>
    /// <param name="input">The mechanical model's input.</param>
    /// <param name="time">Unit: s (seconds).</param>
    /// <returns></returns>
    double CalculateStiffnessThroughForce(ElasticModelInput input, double time);
}
