using MelloSilveiraTools.Mathematics.Models;

namespace MelloSilveiraTools.Mathematics.Functions;

/// <summary>
/// Represents an exponencial function.
/// f(x) = a_0 * e^(a_1 * x) + ... + a_n-1 * e^(a_n * x)
/// </summary>
public sealed record ExponencialFunction : Function
{
    /// <summary>
    /// Initializes a new instance of <see cref="ExponencialFunction"/>.
    /// </summary>
    /// <param name="initialVariableValue"></param>
    /// <param name="finalVariableValue"></param>
    /// <param name="coefficients"></param>
    public ExponencialFunction(
        double? initialVariableValue,
        double? finalVariableValue,
        double[] coefficients) 
        : base(FunctionType.Exponential, initialVariableValue, finalVariableValue, coefficients) { }

    /// <inheritdoc/>
    public override double Calculate(double variableValue)
    {
        double result = 0;
        for (int i = 0; i < Coefficients.Length / 2; i++)
        {
            result += Coefficients[2 * i] * Math.Exp(Coefficients[2 * i + 1] * variableValue);
        }
        return result;
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

        return new ExponencialFunction(InitialVariableValue, FinalVariableValue, derivativeCoefficients);
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

        return new ExponencialFunction(InitialVariableValue, FinalVariableValue, integralCoefficients);
    }
}
