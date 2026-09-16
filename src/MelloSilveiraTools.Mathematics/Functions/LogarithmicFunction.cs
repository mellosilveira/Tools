using MelloSilveiraTools.Mathematics.Models;

namespace MelloSilveiraTools.Mathematics.Functions;

/// <summary>
/// Represents a logarithmic function.
/// f(x) = a_0 + a_1 * ln(a_2 * x) + ... + a_n-1 * ln(a_n * x)
/// </summary>
/// <param name="initialVariableValue">The initial value of the variable range.</param>
/// <param name="finalVariableValue">The final value of the variable range.</param>
/// <param name="coefficients">The array containing the coefficients a_0, a_1, a_2, ..., a_n.</param>
public sealed class LogarithmicFunction(
    double? initialVariableValue,
    double? finalVariableValue,
    double[] coefficients) 
    : Function(FunctionType.Logarithmic, initialVariableValue, finalVariableValue, coefficients)
{
    /// <inheritdoc/>
    public override double Calculate(double variableValue)
    {
        double value = Coefficients[0];
        for (int i = 0; i < (Coefficients.Length - 1) / 2; i++)
        {
            value += Coefficients[2 * i + 1] * Math.Log(Math.Max(Coefficients[2 * i + 2] * variableValue, 1e-12));
        }

        return value;
    }

    /// <inheritdoc/>
    protected override Function CreateDerivative()
    {
        double sumOdd = 0;
        for (int i = 0; i < (Coefficients.Length - 1) / 2; i++)
        {
            sumOdd += Coefficients[2 * i + 1];
        }

        return new GenericFunction(InitialVariableValue, FinalVariableValue, x => sumOdd * (1.0 / x));
    }

    /// <inheritdoc/>
    protected override Function CreateIntegral() => new GenericFunction(InitialVariableValue, FinalVariableValue, x =>
    {
        double value = Coefficients[0];
        for (int i = 0; i < (Coefficients.Length - 1) / 2; i++)
        {
            value += Coefficients[2 * i + 1] * (Math.Log(Coefficients[2 * i + 2] * x) - 1.0);
        }

        return x * value;
    });
}
