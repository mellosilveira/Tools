using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Models;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Pipelines.Models;

/// <summary>
/// Represents the statistical confidence interval for an individual optimized parameter of a mechanical constitutive model, including its property name.
/// </summary>
/// <param name="Index">The zero-based position index of the parameter in the parameter vector.</param>
/// <param name="Value">The optimized point estimate of the parameter value.</param>
/// <param name="LowerBound">The lower bound of the estimated confidence interval.</param>
/// <param name="UpperBound">The upper bound of the estimated confidence interval.</param>
/// <param name="StandardError">The estimated standard error (SE) of the parameter.</param>
/// <param name="ConfidenceLevel">The confidence level represented as a fraction in the range (0, 1) (e.g. 0.95 for 95%).</param>
/// <param name="ParameterName">The exact property name of the constitutive model parameter.</param>
public sealed record MechanicalModelParameterConfidenceInterval(
    int Index,
    double Value,
    double LowerBound,
    double UpperBound,
    double StandardError,
    double ConfidenceLevel,
    string ParameterName) : ParameterConfidenceInterval(Index, Value, LowerBound, UpperBound, StandardError, ConfidenceLevel);
