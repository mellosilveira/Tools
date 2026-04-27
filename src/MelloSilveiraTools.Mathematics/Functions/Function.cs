using MelloSilveiraTools.Mathematics.Models;

namespace MelloSilveiraTools.Mathematics.Functions;

/// <summary>
/// Represents an unique dimension mathematical function, f(x).
/// </summary>
/// <param name="functionType"></param>
/// <param name="initialVariableValue"></param>
/// <param name="finalVariableValue"></param>
/// <param name="coefficients"></param>
/// <exception cref="ArgumentNullException">When <paramref name="coefficients"/> is null.</exception>
public abstract class Function(FunctionType functionType, double? initialVariableValue, double? finalVariableValue, double[] coefficients)
{
    private Function? _derivative;
    private Function? _integral;

    /// <summary>
    /// Represents the function's derivative.
    /// </summary>
    public Function Derivative => _derivative ??= CreateDerivative();

    /// <summary>
    /// Represents the function's integral.
    /// </summary>
    public Function Integral => _integral ??= CreateIntegral();

    /// <inheritdoc cref="Models.FunctionType"/>
    public FunctionType FunctionType { get; } = functionType;

    /// <summary>
    /// Initial value for variable.
    /// This property must be 'protected set' due to an implementation for binary search.
    /// </summary>
    public double InitialVariableValue { get; } = initialVariableValue ?? double.NegativeInfinity;

    /// <summary>
    /// Final value for variable.
    /// </summary>
    public double FinalVariableValue { get; } = finalVariableValue ?? double.PositiveInfinity;

    /// <summary>
    /// Represents the scaling or proportionality factor of the variable in the expression.
    /// </summary>
    public double[] Coefficients { get; } = coefficients;

    /// <summary>
    /// Calculates the value for the function.
    /// </summary>
    /// <param name="variableValue">Dependency variable for equation.</param>
    /// <returns></returns>
    public abstract double Calculate(double variableValue);

    /// <summary>
    /// Creates the function's derivative.
    /// </summary>
    protected abstract Function CreateDerivative();

    /// <summary>
    /// Creates the function's integral.
    /// </summary>
    protected abstract Function CreateIntegral();
}
