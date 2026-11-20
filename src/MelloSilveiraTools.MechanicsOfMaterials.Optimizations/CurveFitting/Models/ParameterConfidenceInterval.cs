namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Models;

/// <summary>
/// Represents the statistical confidence interval for an individual optimized model parameter.
/// </summary>
/// <param name="Index">The zero-based position index of the parameter in the parameter vector.</param>
/// <param name="Value">The optimized point estimate of the parameter value.</param>
/// <param name="LowerBound">The lower bound of the estimated confidence interval.</param>
/// <param name="UpperBound">The upper bound of the estimated confidence interval.</param>
/// <param name="StandardError">The estimated standard error (SE) of the parameter.</param>
/// <param name="ConfidenceLevel">The confidence level represented as a fraction in the range (0, 1) (e.g. 0.95 for 95%).</param>
public record ParameterConfidenceInterval(
    int Index,
    double Value,
    double LowerBound,
    double UpperBound,
    double StandardError,
    double ConfidenceLevel);
