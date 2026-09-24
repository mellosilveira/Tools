using MelloSilveiraTools.Mathematics.Models;

namespace MelloSilveiraTools.Mathematics.Functions;

/// <summary>
/// Represents the Power Law function.
/// f(x) = a_0 * x^(-a_1)
/// </summary>
public class PowerLaw : Function
{
    /// <summary>
    /// Initializes a new instance of <see cref="PowerLaw"/>.
    /// </summary>
    /// <param name="coefficients"></param>
    /// <param name="initialVariableValue"></param>
    /// <param name="finalVariableValue"></param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public PowerLaw(
        double[] coefficients,
        double? initialVariableValue = null,
        double? finalVariableValue = null)
        : base(FunctionType.PowerLaw, coefficients, initialVariableValue, finalVariableValue)
    {
        if (coefficients.Length != 2)
            throw new ArgumentOutOfRangeException(nameof(coefficients), $"'{nameof(PowerLaw)}' must contain exactly 2 coefficients.");
    }

    /// <summary>
    /// Initializes a new instance of <see cref="PowerLaw"/>.
    /// </summary>
    /// <param name="initialVariableValue">Initial value for variable.</param>
    /// <param name="finalVariableValue">Final value for variable.</param>
    /// <param name="coefficients">The coefficients.</param>
    public PowerLaw(
        double? initialVariableValue,
        double? finalVariableValue,
        double[] coefficients)
        : this(coefficients, initialVariableValue, finalVariableValue)
    {
    }

    /// <inheritdoc/>
    public override double Calculate(double variableValue) => Coefficients[0] * Math.Pow(variableValue, -Coefficients[1]);

    /// <inheritdoc/>
    protected override Function CreateDerivative()
    {
        double[] derivativeCoefficients = [-Coefficients[0] * Coefficients[1], Coefficients[1] - 1];
        return new PowerLaw(derivativeCoefficients, InitialVariableValue, FinalVariableValue);
    }

    /// <inheritdoc/>
    protected override Function CreateIntegral()
    {
        double[] integralCoefficients = [Coefficients[0] / (Coefficients[1] + 1), Coefficients[1] + 1];
        return new PowerLaw(integralCoefficients, InitialVariableValue, FinalVariableValue);
    }
}
