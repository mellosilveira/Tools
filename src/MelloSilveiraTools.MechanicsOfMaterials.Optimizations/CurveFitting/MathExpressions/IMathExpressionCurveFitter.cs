using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Models;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.MathExpressions;

/// <summary>
/// Decorator for curve fitting that uses MathExpressions.
/// </summary>
public interface IMathExpressionCurveFitter
{
    /// <summary>
    /// Fit the curve using the provided input.
    /// </summary>
    CurveFitOutput Fit(MathematicalCurveFitInput input);
}
