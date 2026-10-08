using MelloSilveiraTools.Mathematics.Functions;
using MelloSilveiraTools.Mathematics.Models;

namespace MelloSilveiraTools.Mathematics.Factories.Functions;

/// <summary>
/// Centralized factory for constructing analytical mathematical curves and behavior models.
/// Dynamically creates mathematical profiles (polynomial, exponential relaxation, periodic patterns)
/// used to fit experimental data and simulate material response under mechanical stress.
/// </summary>
public class FunctionFactory
{
    /// <summary>
    /// Creates the appropriate mathematical curve based on the requested model type, validity domain, and calibrated parameters.
    /// </summary>
    /// <param name="functionType">Curve family type (constant, polynomial, exponential, sine, cosine, etc.).</param>
    /// <param name="initialVariableValue">Starting boundary of the observation interval (e.g. initial time or strain).</param>
    /// <param name="finalVariableValue">Ending boundary of the observation interval.</param>
    /// <param name="coefficients">Fitted numerical parameters defining curve shape and magnitude.</param>
    /// <returns>A configured function instance ready for numeric simulation and evaluation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when coefficients array is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the requested function type is unrecognized.</exception>
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
