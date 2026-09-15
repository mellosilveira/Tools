using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Elasticity;

namespace MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels.Elasticity;

/// <summary>
/// Defines a calculator for linear elastic models, assuming infinitesimal strains and a linear relationship between stress and strain. 
/// Valid only for stress states that do not produce yielding.
/// </summary>
public interface IElasticModelCalculator : IMechanicalModelCalculator<ElasticConstitutiveParameters>
{
    /// <summary>
    /// Calculates the stiffness based on displacement.
    /// </summary>
    /// <param name="input">The elastic constitutive parameters.</param>
    /// <param name="time">Unit: s (second).</param>
    /// <returns>The calculated structural stiffness.</returns>
    double CalculateStiffnessThroughDisplacement(MechanicalModelInput<ElasticConstitutiveParameters> input, double time);

    /// <summary>
    /// Calculates the stiffness based on force.
    /// </summary>
    /// <param name="input">The elastic constitutive parameters.</param>
    /// <param name="time">Unit: s (second).</param>
    /// <returns>The calculated structural stiffness.</returns>
    double CalculateStiffnessThroughForce(MechanicalModelInput<ElasticConstitutiveParameters> input, double time);
}