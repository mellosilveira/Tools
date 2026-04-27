using MelloSilveiraTools.Mathematics.Models;

namespace MelloSilveiraTools.Mathematics.Functions;

/// <summary>
/// Represents a constant function.
/// f(x) = c
/// </summary>
/// <param name="initialVariableValue"></param>
/// <param name="finalVariableValue"></param>
/// <param name="coefficient"></param>
public sealed class ConstantFunction(
    double? initialVariableValue,
    double? finalVariableValue,
    double coefficient = 0) : Function(FunctionType.Constant, initialVariableValue, finalVariableValue, [coefficient])
{
    /// <inheritdoc/>
    public override double Calculate(double variableValue) => coefficient;

    /// <inheritdoc/>
    /// <remarks>Derivative of a constant function is always zero.</remarks>
    protected override Function CreateDerivative() => new ConstantFunction(InitialVariableValue, FinalVariableValue);

    /// <inheritdoc/>
    protected override Function CreateIntegral() => coefficient == 0
        // Integral of zero is zero.
        ? new ConstantFunction(InitialVariableValue, FinalVariableValue, coefficient)
        // Integral of a constant function is always a polynomial function with degree 1.
        // f(x) = c
        // F(x) = c * x
        : new PolynomialFunction(InitialVariableValue, FinalVariableValue, [0, coefficient]);
}
