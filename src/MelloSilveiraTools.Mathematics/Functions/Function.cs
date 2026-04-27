using MelloSilveiraTools.Mathematics.Models;

namespace MelloSilveiraTools.Mathematics.Functions;

/// <summary>
/// Represents an unique dimension mathematical function, f(x).
/// </summary>
public abstract record Function
{
    /// <summary>
    /// Represents the function's derivative.
    /// </summary>
    private Function _derivative;

    /// <summary>
    /// Represents the function's integral.
    /// </summary>
    private Function _integral;

    /// <summary>
    /// Initializes a new instance of <see cref="Function"/>.
    /// </summary>
    /// <param name="functionType"></param>
    /// <param name="initialVariableValue"></param>
    /// <param name="finalVariableValue"></param>
    /// <param name="coefficients"></param>
    /// <exception cref="ArgumentNullException">When <paramref name="coefficients"/> is null.</exception>
    public Function(FunctionType functionType, double? initialVariableValue, double? finalVariableValue, double[] coefficients)
    {
        FunctionType = functionType;
        InitialVariableValue = initialVariableValue ?? double.NegativeInfinity;
        FinalVariableValue = finalVariableValue ?? double.PositiveInfinity;
        Coefficients = coefficients;
    }

    /// <summary>
    /// Represents the function's derivative.
    /// </summary>
    public Function Derivative
    {
        get
        {
            _derivative ??= CreateDerivative();
            return _derivative;
        }
    }

    /// <summary>
    /// Represents the function's integral.
    /// </summary>
    public Function Integral
    {
        get
        {
            _integral ??= CreateIntegral();
            return _integral;
        }
    }

    /// <inheritdoc cref="SharedModules.Models.Mathematical.FunctionType"/>
    public FunctionType FunctionType { get; }

    /// <summary>
    /// Initial value for variable.
    /// This property must be 'protected set' due to an implementation for binary search.
    /// </summary>
    public double InitialVariableValue { get; protected set; }

    /// <summary>
    /// Final value for variable.
    /// </summary>
    public double FinalVariableValue { get; }

    /// <summary>
    /// Represents the scaling or proportionality factor of the variable in the expression.
    /// </summary>
    public double[] Coefficients { get; }

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
