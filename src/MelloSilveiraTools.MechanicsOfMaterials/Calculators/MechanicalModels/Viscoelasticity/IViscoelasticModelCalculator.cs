using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;

namespace MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels.Viscoelasticity;

/// <summary>
/// Defines a generic viscoelastic model calculator, accommodating linear, quasi-linear, and non-linear behaviors.
/// Establishes time-dependent approaches for stress-strain and force-displacement relationships.
/// Viscoelasticity is the property of models that exhibit both viscous and elastic characteristics simultaneously.
/// </summary>
/// <typeparam name="TConstitutiveParameters">The specific type of constitutive parameters governing the viscoelastic model.</typeparam>
public interface IViscoelasticModelCalculator<TConstitutiveParameters> : IMechanicalModelCalculator<TConstitutiveParameters> where TConstitutiveParameters : ConstitutiveParameters
{
    /// <summary>
    /// Calculates the relaxation function (the viscous component of the equation), represented by G. 
    /// For more details, see the "Bibliographies" section in the "README.md" file.
    /// </summary>
    /// <param name="input">The mechanical model's input data.</param>
    /// <param name="time">Unit: s (second).</param>
    /// <param name="strain">
    /// Unit: dimensionless.
    /// If not provided, this is calculated from the strain parameters on the mechanical model's input.
    /// </param>
    /// <returns>Unit: MPa (Mega-Pascal).</returns>
    double CalculateRelaxationFunction(MechanicalModelInput<TConstitutiveParameters> input, double time, double? strain = null);

    /// <summary>
    /// Calculates the creep compliance (the viscous component of the equation), represented by J. 
    /// For more details, see the "Bibliographies" section in the "README.md" file.
    /// </summary>
    /// <param name="input">The mechanical model's input data.</param>
    /// <param name="time">Unit: s (second).</param>
    /// <param name="stress">
    /// Unit: MPa (Mega-Pascal).
    /// If not provided, this is calculated from the stress parameters on the mechanical model's input.
    /// </param>
    /// <returns>Unit: /MPa (per Mega-Pascal).</returns>
    double CalculateCreepCompliance(MechanicalModelInput<TConstitutiveParameters> input, double time, double? stress = null);
}