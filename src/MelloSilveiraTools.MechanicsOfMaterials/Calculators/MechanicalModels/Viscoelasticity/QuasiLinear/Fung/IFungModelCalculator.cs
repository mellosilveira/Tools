using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.QuasiLinear;

namespace MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels.Viscoelasticity.QuasiLinear.Fung;

/// <summary>
/// Defines a calculator for the classic Fung Quasi-Linear Viscoelastic (QLV) model.
/// </summary>
/// <remarks>
/// This calculator implements the exact numerical evaluation of Fung's continuous relaxation spectrum, 
/// avoiding the discrete approximations used in the simplified model.
/// </remarks>
public interface IFungModelCalculator : IQuasiLinearModelCalculator<FungConstitutiveParameters, ReducedRelaxationFunction>
{
    /// <summary>
    /// Calculates the integral function I(t), which resolves the continuous relaxation spectrum between the fast and slow relaxation times.
    /// </summary>
    /// <remarks>
    /// Mathematically, this evaluates the exponential integral commonly represented as ∫(e^(-t/τ) / τ) dτ from τ2 to τ1.
    /// </remarks>
    /// <param name="slowRelaxationTime">The upper bound of the relaxation spectrum (τ1). Unit: s (second).</param>
    /// <param name="fastRelaxationTime">The lower bound of the relaxation spectrum (τ2). Unit: s (second).</param>
    /// <param name="timeStep">The numerical integration step. Unit: s (second).</param>
    /// <param name="time">The current elapsed time. Unit: s (second).</param>
    /// <returns>Unit: dimensionless.</returns>
    double CalculateI(double slowRelaxationTime, double fastRelaxationTime, double timeStep, double time);
}