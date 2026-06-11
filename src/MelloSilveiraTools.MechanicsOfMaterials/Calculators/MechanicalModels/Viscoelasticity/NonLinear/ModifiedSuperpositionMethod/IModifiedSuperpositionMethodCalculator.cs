using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.NonLinear.ModifiedSuperpositionMethod;

namespace MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels.Viscoelasticity.NonLinear.ModifiedSuperpositionMethod;

/// <summary>
/// Modified Superposition Method, a non-linear viscoelastic model based on Boltzmann superposition method.
/// </summary>
public interface IModifiedSuperpositionMethodCalculator : IViscoelasticModelCalculator<ModifiedSuperpositionMethodInput>
{
    /// <summary>
    /// Calculates the initial young modulus.
    /// </summary>
    /// <param name="input">The mechanical model's input.</param>
    /// <param name="strain">Unit: dimensionless.</param>
    /// <returns>Unit: dimensionless.</returns>
    double CalculateInitialYoungModulus(ModifiedSuperpositionMethodInput input, double strain);

    /// <summary>
    /// Calculates the stress relaxation rate.
    /// </summary>
    /// <param name="input">The mechanical model's input.</param>
    /// <param name="strain">Unit: dimensionless.</param>
    /// <returns>Unit: MPa (Mega-Pascal).</returns>
    double CalculateStressRelaxationRate(ModifiedSuperpositionMethodInput input, double strain);
}