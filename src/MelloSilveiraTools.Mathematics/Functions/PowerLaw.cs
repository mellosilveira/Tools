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

    /// <inheritdoc/>
    public override double Calculate(double variableValue) => Coefficients[0] * Math.Pow(variableValue, -Coefficients[1]);

    /// <summary>
    /// Creates the analytical derivative of the Power Law function:
    /// f'(x) = -a_0 * a_1 * x^(-(a_1 + 1)).
    /// </summary>
    /// <returns>The analytical derivative function.</returns>
    protected override Function CreateDerivative()
    {
        double[] derivativeCoefficients = [-Coefficients[0] * Coefficients[1], Coefficients[1] + 1];
        return new PowerLaw(derivativeCoefficients, InitialVariableValue, FinalVariableValue);
    }

    /// <summary>
    /// Creates the analytical antiderivative of the Power Law function:
    /// F(x) = (a_0 / (1 - a_1)) * x^(-(a_1 - 1)) when a_1 != 1, or F(x) = a_0 * ln(x) when a_1 = 1.
    /// </summary>
    /// <returns>The analytical integral function.</returns>
    protected override Function CreateIntegral()
    {
        if (Math.Abs(Coefficients[1] - 1) <= MathematicConstants.MachineEpsilonFloor)
            return new LogarithmicFunction([0, Coefficients[0], 1], InitialVariableValue, FinalVariableValue);

        double[] integralCoefficients = [Coefficients[0] / (1 - Coefficients[1]), Coefficients[1] - 1];
        return new PowerLaw(integralCoefficients, InitialVariableValue, FinalVariableValue);
    }
}
