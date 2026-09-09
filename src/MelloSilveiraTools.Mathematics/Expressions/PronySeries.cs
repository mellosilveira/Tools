using MelloSilveiraTools.Mathematics.Functions;

namespace MelloSilveiraTools.Mathematics.Expressions;

/// <summary>
/// Represents a prony series.
/// f(x) = c + [a_0 * e^(a_1 * x) + ... a_n-1 * e^(a_n * x)]
/// </summary>
/// <param name="initialVariableValue">Initial value for variable.</param>
/// <param name="finalVariableValue">Final value for variable.</param>
/// <param name="independentParameter">Independent parameter represented by c.</param>
/// <param name="iteratorCoefficients">Coefficients for iterations represented by a_n.</param>
public sealed class PronySeries(double? initialVariableValue, double? finalVariableValue, double independentParameter, double[] iteratorCoefficients)
    : MathExpression(
        initialVariableValue,
        finalVariableValue,
        [
            new PolynomialFunction(initialVariableValue, finalVariableValue, [independentParameter]),
            new ExponencialFunction(initialVariableValue, finalVariableValue, iteratorCoefficients)
        ])
{
    /// <summary>
    /// Independent parameter represented by c.
    /// </summary>
    public double IndependentParameter { get; } = independentParameter;

    /// <summary>
    /// Coefficients for iterations represented by a_n.
    /// </summary>
    public double[] IteratorCoefficients { get; } = iteratorCoefficients;
}
