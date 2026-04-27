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
    /// <param name="initialVariableValue"></param>
    /// <param name="finalVariableValue"></param>
    /// <param name="coefficients"></param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public PowerLaw(
        double? initialVariableValue,
        double? finalVariableValue,
        double[] coefficients)
        : base(FunctionType.PowerLaw, initialVariableValue, finalVariableValue, coefficients)
    {
        if (coefficients.Length != 2)
            throw new ArgumentOutOfRangeException(nameof(coefficients), $"'{nameof(PowerLaw)}' must contain exactly 2 coefficients.");
    }

    /// <inheritdoc/>
    public override double Calculate(double variableValue)
    {
        return Coefficients[0] * Math.Pow(variableValue, -Coefficients[1]);
    }

    /// <inheritdoc/>
    protected override Function CreateDerivative()
    {
        double[] derivativeCoefficients = [-Coefficients[0] * Coefficients[1], Coefficients[1] - 1];
        return new PowerLaw(InitialVariableValue, FinalVariableValue, derivativeCoefficients);
    }

    /// <inheritdoc/>
    protected override Function CreateIntegral()
    {
        double[] integralCoefficients = [Coefficients[0] / (Coefficients[1] + 1), Coefficients[1] + 1];
        return new PowerLaw(InitialVariableValue, FinalVariableValue, integralCoefficients);
    }
}
