using MelloSilveiraTools.Core.Pipelines.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Models;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.MathExpressions;

/// <summary>
/// Decorator for curve fitting that uses MathExpressions.
/// </summary>
public interface IMathExpressionCurveFitter
{
    /// <summary>
    /// Attempts to fit the curve using the provided input, catching internal exceptions.
    /// </summary>
    SafeResult<CurveFitInput, CurveFitOutput> TryFit(MathematicalCurveFitInput input);
}
