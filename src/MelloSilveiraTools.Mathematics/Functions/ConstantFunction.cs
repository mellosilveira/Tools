using MelloSilveiraTools.Mathematics.Models;

namespace MelloSilveiraTools.Mathematics.Functions;

/// <summary>
/// Represents a constant function.
/// f(x) = c
/// </summary>
/// <param name="coefficient"></param>
/// <param name="initialVariableValue"></param>
/// <param name="finalVariableValue"></param>
public sealed class ConstantFunction(
    double coefficient = 0,
    double? initialVariableValue = null,
    double? finalVariableValue = null)
    : Function(FunctionType.Constant, [coefficient], initialVariableValue, finalVariableValue)
{
    /// <inheritdoc/>
    public override double Calculate(double variableValue) => coefficient;

    /// <inheritdoc/>
    /// <remarks>Derivative of a constant function is always zero.</remarks>
    protected override Function CreateDerivative() => new ConstantFunction(InitialVariableValue, FinalVariableValue);

    /// <inheritdoc/>
    protected override Function CreateIntegral() => coefficient == 0
        // Integral of zero is zero.
        ? new ConstantFunction(coefficient, InitialVariableValue, FinalVariableValue)
        // Integral of a constant function is always a polynomial function with degree 1.
        // f(x) = c
        // F(x) = c * x
        : new PolynomialFunction([0, coefficient], InitialVariableValue, FinalVariableValue);
}
