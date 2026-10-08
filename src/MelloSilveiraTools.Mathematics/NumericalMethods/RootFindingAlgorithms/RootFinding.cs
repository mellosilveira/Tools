using MelloSilveiraTools.Mathematics.Models.NumericalMethods;

namespace MelloSilveiraTools.Mathematics.NumericalMethods.RootFindingAlgorithms;

/// <summary>
/// Base class providing standardized error reporting and execution scaffolding for root-finding algorithms.
/// Solves for balance points, zero-crossing thresholds, or parameter calibrations where opposing terms reach equilibrium.
/// </summary>
public abstract class RootFinding : IRootFinding
{
    /// <inheritdoc/>
    public abstract (double Root, double Error) FindRoot(RootFindingInput input, Func<double, double> function);

    /// <summary>
    /// Creates an exception when the algorithm cannot reach the desired convergence criteria within limits.
    /// </summary>
    /// <param name="point">The last evaluated candidate point.</param>
    /// <param name="value">The residual value at the candidate point.</param>
    /// <returns>A configured <see cref="NonConvergenceException"/>.</returns>
    protected Exception GetNonConvergenceException(double point, double value) => new NonConvergenceException(GetType().Name, point, value);
}