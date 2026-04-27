using MelloSilveiraTools.Mathematics.Models;

namespace MelloSilveiraTools.Mathematics.Functions;

/// <summary>
/// Represents a polynomial function.
/// f(x) = a_0 + a_1 * x + a_2 * x^2 + ... + a_n * x^n
/// </summary>
public sealed record PolynomialFunction : Function
{
    /// <summary>
    /// Initializes a new instance of <see cref="PolynomialFunction"/>.
    /// </summary>
    /// <param name="initialVariableValue"></param>
    /// <param name="finalVariableValue"></param>
    /// <param name="coefficients"></param>
    public PolynomialFunction(
        double? initialVariableValue,
        double? finalVariableValue,
        double[] coefficients)
        : base(FunctionType.Polynomial, initialVariableValue, finalVariableValue, coefficients) { }

    /// <inheritdoc/>
    public override double Calculate(double variableValue)
    {
        double result = Coefficients[0];
        for (int i = 1; i < Coefficients.Length; i++)
        {
            result += Coefficients[i] * Math.Pow(variableValue, i);
        }
        return result;
    }

    /// <inheritdoc/>
    protected override Function CreateDerivative()
    {
        int coefficientsLength = Coefficients.Length;

        // f(x) = a_0
        // f'(x) = 0
        if (coefficientsLength <= 1)
            return new ConstantFunction(InitialVariableValue, FinalVariableValue);

        // f(x) = a_0 + a_1 * x
        // f'(x) = a_1
        if (coefficientsLength == 2)
            return new ConstantFunction(InitialVariableValue, FinalVariableValue, Coefficients[1]);

        // f(x) = a_0 + a_1 * x + ... + a_n * x^n
        // f'(x) = a_1 + 2 * a_2 * x + ... + n * a_n * x^(n-1)
        var derivativeCoefficients = new double[coefficientsLength - 1];
        for (int i = 1; i < coefficientsLength; i++)
        {
            derivativeCoefficients[i - 1] = Coefficients[i] * i;
        }

        return new PolynomialFunction(InitialVariableValue, FinalVariableValue, derivativeCoefficients);
    }

    /// <inheritdoc/>
    protected override Function CreateIntegral()
    {
        int coefficientsLength = Coefficients.Length;
        double[] integralCoefficients;

        if (coefficientsLength == 1 && Coefficients[0] == 0)
        {
            integralCoefficients = new double[] { 0 };
        }
        else
        {
            integralCoefficients = new double[coefficientsLength + 1];
            integralCoefficients[0] = 0;

            for (int i = 0; i < coefficientsLength; i++)
            {
                integralCoefficients[i + 1] = Coefficients[i] / (i + 1);
            }
        }

        return new PolynomialFunction(InitialVariableValue, FinalVariableValue, integralCoefficients);
    }
}
