using MelloSilveiraTools.Mathematics.Functions;
using MelloSilveiraTools.Mathematics.Models;

namespace MelloSilveiraTools.Mathematics.Factories.Functions;

public class FunctionFactory
{
    /// <inheritdoc/>
    public Function Create(FunctionType functionType, double? initialVariableValue, double? finalVariableValue, double[] coefficients) => functionType switch
    {
        FunctionType.Constant => new ConstantFunction(initialVariableValue, finalVariableValue, coefficients[0]),
        FunctionType.Polynomial => new PolynomialFunction(initialVariableValue, finalVariableValue, coefficients),
        FunctionType.Exponential => new ExponencialFunction(initialVariableValue, finalVariableValue, coefficients),
        FunctionType.Sine => new SineFunction(initialVariableValue, finalVariableValue, coefficients),
        FunctionType.Cosine => new CosineFunction(initialVariableValue, finalVariableValue, coefficients),
        FunctionType.PowerLaw => new PowerLaw(initialVariableValue, finalVariableValue, coefficients),
        _ => throw new ArgumentOutOfRangeException(nameof(functionType))
    };
}
