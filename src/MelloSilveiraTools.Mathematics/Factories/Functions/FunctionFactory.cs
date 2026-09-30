using MelloSilveiraTools.Mathematics.Functions;
using MelloSilveiraTools.Mathematics.Models;

namespace MelloSilveiraTools.Mathematics.Factories.Functions;

public class FunctionFactory
{
    /// <inheritdoc/>
    public Function Create(FunctionType functionType, double? initialVariableValue, double? finalVariableValue, double[] coefficients) => functionType switch
    {
        FunctionType.Constant => new ConstantFunction(coefficients[0], initialVariableValue, finalVariableValue),
        FunctionType.Polynomial => new PolynomialFunction(coefficients, initialVariableValue, finalVariableValue),
        FunctionType.Exponential => new ExponentialFunction(coefficients, initialVariableValue, finalVariableValue),
        FunctionType.Sine => new SineFunction(coefficients, initialVariableValue, finalVariableValue),
        FunctionType.Cosine => new CosineFunction(coefficients, initialVariableValue, finalVariableValue),
        FunctionType.PowerLaw => new PowerLaw(coefficients, initialVariableValue, finalVariableValue),
        FunctionType.Logarithmic => new LogarithmicFunction(coefficients, initialVariableValue, finalVariableValue),
        _ => throw new ArgumentOutOfRangeException(nameof(functionType))
    };
}
