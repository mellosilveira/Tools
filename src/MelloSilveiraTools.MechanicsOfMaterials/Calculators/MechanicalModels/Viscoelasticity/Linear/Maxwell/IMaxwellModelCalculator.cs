using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.Linear.Maxwell;

namespace MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels.Viscoelasticity.Linear.Maxwell;

/// <summary>
/// Defines a calculator for the Maxwell linear viscoelastic model.
/// </summary>
/// <remarks>
/// The Maxwell model is represented mechanically by a purely elastic spring (stiffness, μ) 
/// and a purely viscous dashpot (viscosity, η) connected in series. 
/// While it has limited accuracy for modeling soft biological tissues, it exhibits good applicability for metals and fluid-like materials.
/// </remarks>
public interface IMaxwellModelCalculator : IViscoelasticModelCalculator<MaxwellConstitutiveParameters>
{
    /// <summary>
    /// Calculates the relaxation time (τ), which dictates the rate at which stress decays under constant strain.
    /// Formula: τ = η / μ (Projeto Final, Eq. 26).
    /// </summary>
    /// <param name="input">The constitutive parameters for the Maxwell model.</param>
    /// <returns>Unit: s (second).</returns>
    double CalculateRelaxationTime(MechanicalModelInput<MaxwellConstitutiveParameters> input);
}