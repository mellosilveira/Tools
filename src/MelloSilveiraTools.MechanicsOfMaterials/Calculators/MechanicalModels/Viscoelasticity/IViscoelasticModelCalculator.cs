using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;

namespace MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels.Viscoelasticity;

/// <summary>
/// A generic viscoelastic model and can be linear, quasi-linear and non-linear.
/// Establish an approach using viscoelasticity for stress-strain and force-displacement relation which approximate from reality.
/// Viscoelasticity is understood as the property of materials that present viscous and elastic behavior at the same time. 
/// </summary>
/// <typeparam name="TInput">Type of viscoelastic model's input.</typeparam>
public interface IViscoelasticModelCalculator<TInput> : IMechanicalModelCalculator<TInput> where TInput : MechanicalModelInput, new()
{
    /// <summary>
    /// Calculates the relaxation function, the viscous part of equation, represented by G. 
    /// For more details, see on section "Bibliographies" on file "README.MD".
    /// </summary>
    /// <param name="input">The mechanical model's input.</param>
    /// <param name="time">Unit: s (second).</param>
    /// <param name="strain">
    /// If not informed, this is calculated from the strain parameters on mechanical model's input.
    /// Unit: dimensionless.
    /// </param>
    /// <returns>Unit: MPa (Mega-Pascal).</returns>
    double CalculateRelaxationFunction(TInput input, double time, double? strain = null);

    /// <summary>
    /// Calculates the creep compliance, the viscous part of equation, represented by J. 
    /// For more details, see on section "Bibliographies" on file "README.MD".
    /// </summary>
    /// <param name="input">The mechanical model's input.</param>
    /// <param name="time">Unit: s (second).</param>
    /// <param name="stress">
    /// If not informed, this is calculated from the stress parameters on mechanical model's input.
    /// Unit: MPa (Mega-Pascal).
    /// </param>
    /// <returns>Unit: /MPa (per Mega-Pascal).</returns>
    double CalculateCreepCompliance(TInput input, double time, double? stress = null);
}