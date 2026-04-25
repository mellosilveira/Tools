using MelloSilveiraTools.Mathematics.Domain.Models;
using MelloSilveiraTools.Mathematics.Domain.NumericalMethods;

namespace MelloSilveiraTools.Mathematics.Domain.NumericalMethods.DifferentialEquation;

/// <summary>
/// Executes numerical method to solve Differential Equation
/// </summary>
public interface IDifferentialEquationMethod
{
    /// <summary>
    /// The method type identifying this implementation.
    /// </summary>
    DifferentialEquationMethodType Type { get; }

    /// <summary>
    /// Advances the dynamic system by one time step, computing the state (displacement, velocity,
    /// acceleration and equivalent force) at the requested instant <paramref name="time"/>.
    /// </summary>
    /// <param name="input">
    /// The system data: time step Δt (s), mass matrix [M] (kg), stiffness matrix [K] (N/m),
    /// damping matrix [C] (N·s/m), the equivalent force vector at <paramref name="time"/> (N) and the
    /// number of boundary conditions. Δt is read from <c>input.TimeStep</c> and must be consistent
    /// across calls so that the integration constants remain valid.
    /// </param>
    /// <param name="time">The current instant, in seconds, at which the result must be evaluated. Must be greater than or equal to the configured initial time.</param>
    /// <param name="previousResult">
    /// The state at the previous instant <c>time − Δt</c>. Must not be <see langword="null"/> for any
    /// step beyond the initial one. At the initial instant the implementation returns the initial
    /// state directly and ignores this argument.
    /// </param>
    /// <returns>The numerical state of the system at <paramref name="time"/>.</returns>
    /// <remarks>
    /// Implementations integrate a second-order ODE of the form [M]ẍ + [C]ẋ + [K]x = F(t).
    /// The classic <see cref="NewmarkMethod"/> uses γ = 1/2, β = 1/4 (constant-average-acceleration)
    /// which is unconditionally stable and second-order accurate. The <see cref="NewmarkBetaMethod"/>
    /// variant uses γ = 1/2, β = 1/6 (linear-acceleration), which is conditionally stable
    /// (Δt must be small relative to the highest natural period) but provides better phase accuracy.
    /// </remarks>
    NumericalMethodResult CalculateResult(NumericalMethodInput input, double time, NumericalMethodResult previousResult);
}