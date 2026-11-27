namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Models;

/// <summary>
/// Output data structure resulting from numerical curve fitting optimization.
/// Unifies optimized parameter values and their statistical confidence intervals in a single collection.
/// </summary>
/// <param name="Parameters">The list of optimized parameters with their point estimates and confidence intervals.</param>
/// <param name="FinalError">The final sum of squared residuals (SSR) achieved by the optimizer.</param>
/// <param name="RSquared">The coefficient of determination (R²) of the fit.</param>
/// <param name="Iterations">The total number of iterations executed by the optimizer.</param>
public record CurveFitOutput(
    IReadOnlyList<ParameterConfidenceInterval> Parameters,
    double FinalError,
    double RSquared,
    int Iterations)
{
    /// <summary>
    /// Gets the raw array of optimized parameter point estimates extracted from <see cref="Parameters"/>.
    /// </summary>
    public double[] OptimizedParameters => [.. Parameters.Select(parameter => parameter.Value)];
}
