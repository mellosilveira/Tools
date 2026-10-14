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
    /// Formula: I(t) = ∫[t/τ₂, t/τ₁] e^(-x)/x dx (Projeto Final, Eq. 43).
    /// <para>
    /// Physical Spectrum Boundaries:
    /// <list type="bullet">
    /// <item><description><paramref name="slowRelaxationTime"/> represents τ₂ (upper bound of the continuous relaxation spectrum, long-term equilibrium).</description></item>
    /// <item><description><paramref name="fastRelaxationTime"/> represents τ₁ (lower bound of the continuous relaxation spectrum, short-term response).</description></item>
    /// </list>
    /// Since τ₁ &lt; τ₂, the substitution x = t/τ transforms the integration domain from [τ₁, τ₂] to [t/τ₂, t/τ₁],
    /// so the lower integration limit is <c>initialTime = t / slowRelaxationTime</c> and the upper limit is <c>finalTime = t / fastRelaxationTime</c>.
    /// When t → 0, the integrand e^(-x)/x ≈ 1/x, yielding analytically I(0) = ln(τ₂/τ₁).
    /// </para>
    /// </remarks>
    /// <param name="slowRelaxationTime">The upper bound of the relaxation spectrum (τ2). Unit: s (second).</param>
    /// <param name="fastRelaxationTime">The lower bound of the relaxation spectrum (τ1). Unit: s (second).</param>
    /// <param name="timeStep">The numerical integration step. Unit: s (second).</param>
    /// <param name="time">The current elapsed time. Unit: s (second).</param>
    /// <returns>Unit: dimensionless.</returns>
    double CalculateI(double slowRelaxationTime, double fastRelaxationTime, double timeStep, double time);
}