using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Mathematics.Models.NumericalMethods;
using Microsoft.Extensions.Logging;

namespace MelloSilveiraTools.Mathematics.NumericalMethods.RootFindingAlgorithms;

/// <summary>
/// Base class providing standardized error reporting and execution scaffolding for root-finding algorithms.
/// Solves for balance points, zero-crossing thresholds, or parameter calibrations where opposing terms reach equilibrium.
/// </summary>
/// <param name="logger">Optional logger for telemetry and failure reporting.</param>
public abstract class RootFinding(ILogger? logger = null) : IRootFinding
{
    /// <inheritdoc/>
    public abstract (double Root, double Error) FindRoot(RootFindingInput input, Func<double, double> function);

    /// <inheritdoc/>
    public Result<(double Root, double Error)> TryFindRoot(RootFindingInput input, Func<double, double> function)
    {
        try
        {
            (double Root, double Error) result = FindRoot(input, function);
            return Result.CreateSuccessOk(result);
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Failed to converge to root for function. Algorithm: {Algorithm}, InitialPoint: {InitialPoint}, FinalPoint: {FinalPoint}.", GetType().Name, input.InitialPoint, input.FinalPoint);
            return Result.CreateUnknownError(ex.Message);
        }
    }

    /// <summary>
    /// Creates an exception when the algorithm cannot reach the desired convergence criteria within limits.
    /// </summary>
    /// <param name="point">The last evaluated candidate point.</param>
    /// <param name="value">The residual value at the candidate point.</param>
    /// <returns>A configured <see cref="NonConvergenceException"/>.</returns>
    protected Exception GetNonConvergenceException(double point, double value) => new NonConvergenceException(GetType().Name, point, value);
}
