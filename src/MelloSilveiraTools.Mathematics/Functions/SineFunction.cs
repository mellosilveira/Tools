using MelloSilveiraTools.Mathematics.Models;

namespace MelloSilveiraTools.Mathematics.Functions;

/// <summary>
/// Represents a sine function.
/// f(x) = a_0 * sin[a_1 * (x - a_2)] + ... + a_n-2 * sin[a_n-1 * (x - a_n)]
/// </summary>
/// <param name="initialVariableValue"></param>
/// <param name="finalVariableValue"></param>
/// <param name="coefficients"></param>
public sealed class SineFunction(
    double? initialVariableValue,
    double? finalVariableValue,
    double[] coefficients) : Function(FunctionType.Sine, initialVariableValue, finalVariableValue, coefficients)
{

    /// <inheritdoc/>
    public override double Calculate(double variableValue)
    {
        double result = 0;
        for (int i = 0; i < Coefficients.Length / 3; i++)
        {
            result += Coefficients[3 * i] * Math.Sin(Coefficients[3 * i + 1] * (variableValue + Coefficients[3 * i + 2]));
        }
        return result;
    }

    /// <inheritdoc/>
    protected override Function CreateDerivative()
    {
        int coefficientsLength = Coefficients.Length;
        var derivativeCoefficients = new double[coefficientsLength];

        for (int i = 0; i < coefficientsLength / 3; i++)
        {
            derivativeCoefficients[3 * i] = Coefficients[3 * i] * Coefficients[3 * i + 1];
            derivativeCoefficients[3 * i + 1] = Coefficients[3 * i + 1];
            derivativeCoefficients[3 * i + 2] = Coefficients[3 * i + 2];
        }

        return new CosineFunction(InitialVariableValue, FinalVariableValue, derivativeCoefficients);
    }

    /// <inheritdoc/>
    protected override Function CreateIntegral()
    {
        int coefficientsLength = Coefficients.Length;
        var integralCoefficients = new double[coefficientsLength];

        for (int i = 0; i < coefficientsLength / 3; i++)
        {
            integralCoefficients[3 * i] = -Coefficients[3 * i] / Coefficients[3 * i + 1];
            integralCoefficients[3 * i + 1] = Coefficients[3 * i + 1];
            integralCoefficients[3 * i + 2] = Coefficients[3 * i + 2];
        }

        return new CosineFunction(InitialVariableValue, FinalVariableValue, integralCoefficients);
    }
}
