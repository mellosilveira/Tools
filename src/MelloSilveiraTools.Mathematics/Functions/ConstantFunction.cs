using MelloSilveiraTools.Mathematics.Models;

namespace MelloSilveiraTools.Mathematics.Functions;

/// <summary>
/// Represents a constant function.
/// f(x) = c
/// </summary>
public sealed record ConstantFunction : Function
{
    private readonly double _coefficient;

    /// <summary>
    /// Initializes a new instance of <see cref="ConstantFunction"/>.
    /// </summary>
    /// <param name="initialVariableValue"></param>
    /// <param name="finalVariableValue"></param>
    /// <param name="coefficient"></param>
    public ConstantFunction(
        double? initialVariableValue,
        double? finalVariableValue,
        double coefficient = 0)
        : base(FunctionType.Constant, initialVariableValue, finalVariableValue, [coefficient]) 
    {
        _coefficient = coefficient;
    }

    /// <inheritdoc/>
    public override double Calculate(double variableValue) => _coefficient;

    /// <inheritdoc/>
    /// <remarks>Derivative of a constant function is always zero.</remarks>
    protected override Function CreateDerivative() => new ConstantFunction(InitialVariableValue, FinalVariableValue);

    /// <inheritdoc/>
    protected override Function CreateIntegral() => _coefficient == 0
        // Integral of zero is zero.
        ? new ConstantFunction(InitialVariableValue, FinalVariableValue, _coefficient)
        // Integral of a constant function is always a polynomial function with degree 1.
        // f(x) = c
        // F(x) = c * x
        : new PolynomialFunction(InitialVariableValue, FinalVariableValue, [0, _coefficient]);
}
