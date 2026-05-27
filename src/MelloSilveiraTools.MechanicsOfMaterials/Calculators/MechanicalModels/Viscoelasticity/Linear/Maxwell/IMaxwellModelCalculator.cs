using MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels.Viscoelasticity.Linear;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.Linear.Maxwell;

namespace MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels.Viscoelasticity.Linear.Maxwell;

/// <summary>
/// Maxwell's linear viscoelastic model. It has a poor precision for soft tissues, but a good applicability for metals.
/// The model represents a spring and dashpot in series, where μ is the stiffness and η is the viscosity.
/// </summary>
public interface IMaxwellModelCalculator : ILinearModelCalculator<MaxwellModelInput>
{
    /// <summary>
    /// Calculates the relaxation time: τ = η / μ (Projeto Final, Eq. 26).
    /// </summary>
    /// <param name="input">The mechanical's model input.</param>
    /// <returns>Unit: s (second).</returns>
    double CalculateRelaxationTime(MaxwellModelInput input);
}