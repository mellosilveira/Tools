using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.NonLinear.ModifiedSuperpositionMethod;

namespace MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels.Viscoelasticity.NonLinear.ModifiedSuperpositionMethod;

/// <summary>
/// Defines a calculator for the Modified Superposition Method (MSM), a non-linear viscoelastic model based on the Boltzmann superposition principle.
/// </summary>
public interface IModifiedSuperpositionMethodCalculator : IViscoelasticModelCalculator<ModifiedSuperpositionMethodConstitutiveParameters>
{
    /// <summary>
    /// Calculates the initial Young's Modulus based on the applied strain.
    /// </summary>
    /// <param name="input">The mechanical model's input data.</param>
    /// <param name="strain">Unit: dimensionless.</param>
    /// <returns>Unit: dimensionless.</returns>
    double CalculateInitialYoungModulus(MechanicalModelInput<ModifiedSuperpositionMethodConstitutiveParameters> input, double strain);

    /// <summary>
    /// Calculates the stress relaxation rate based on the applied strain.
    /// </summary>
    /// <param name="input">The mechanical model's input data.</param>
    /// <param name="strain">Unit: dimensionless.</param>
    /// <returns>Unit: MPa (Mega-Pascal).</returns>
    double CalculateStressRelaxationRate(MechanicalModelInput<ModifiedSuperpositionMethodConstitutiveParameters> input, double strain);
}