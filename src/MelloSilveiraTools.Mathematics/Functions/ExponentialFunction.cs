using MelloSilveiraTools.Mathematics.Models;

namespace MelloSilveiraTools.Mathematics.Functions;

/// <summary>
/// Represents an exponencial function.
/// f(x) = a_0 * e^(a_1 * x) + ... + a_n-1 * e^(a_n * x)
/// </summary>
/// <param name="coefficients"></param>
/// <param name="initialVariableValue"></param>
/// <param name="finalVariableValue"></param>
public sealed class ExponentialFunction(
    double[] coefficients,
    double? initialVariableValue = null,
    double? finalVariableValue = null) : Function(FunctionType.Exponential, coefficients, initialVariableValue, finalVariableValue)
{
    /// <inheritdoc/>
    public override double Calculate(double variableValue)
    {
        double value = 0;
        for (int i = 0; i < Coefficients.Length / 2; i++)
        {
            value += Coefficients[2 * i] * Math.Exp(Coefficients[2 * i + 1] * variableValue);
        }
        return value;
    }

    /// <inheritdoc/>
    protected override Function CreateDerivative()
    {
        int coefficientsLength = Coefficients.Length;
        var derivativeCoefficients = new double[coefficientsLength];

        for (int i = 0; i < coefficientsLength / 2; i++)
        {
            derivativeCoefficients[2 * i] = Coefficients[2 * i] * Coefficients[2 * i + 1];
            derivativeCoefficients[2 * i + 1] = Coefficients[2 * i + 1];
        }

        return new ExponentialFunction(derivativeCoefficients, InitialVariableValue, FinalVariableValue);
    }

    /// <inheritdoc/>
    protected override Function CreateIntegral()
    {
        int coefficientsLength = Coefficients.Length;
        var integralCoefficients = new double[coefficientsLength];

        for (int i = 0; i < coefficientsLength / 2; i++)
        {
            integralCoefficients[2 * i] = Coefficients[2 * i] / Coefficients[2 * i + 1];
            integralCoefficients[2 * i + 1] = Coefficients[2 * i + 1];
        }

        return new ExponentialFunction(integralCoefficients, InitialVariableValue, FinalVariableValue);
    }
}
