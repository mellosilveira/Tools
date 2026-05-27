using MelloSilveiraTools.Mathematics.Functions;

namespace MelloSilveiraTools.Mathematics.Expressions;

/// <summary>
/// Represents an unique dimension mathematical expression.
/// </summary>
public class Expression : List<Function>
{
    private Expression? _derivative;
    private Expression? _integral;

    /// <summary>
    /// Initializes a new instance of <see cref="Expression"/>.
    /// </summary>
    /// <param name="initialVariableValue"></param>
    /// <param name="finalVariableValue"></param>
    /// <param name="functions"></param>
    /// <exception cref="ArgumentNullException">When <paramref name="functions"/> is null or empty.</exception>
    public Expression(double? initialVariableValue, double? finalVariableValue, List<Function> functions) : base(functions)
    {
        InitialVariableValue = initialVariableValue ?? double.NegativeInfinity;
        FinalVariableValue = finalVariableValue ?? double.PositiveInfinity;

        // Build an internal sorted copy so the caller's list is never mutated.
        var sorted = new List<Function>(functions);
        sorted.Sort((f1, f2) => f1.InitialVariableValue.CompareTo(f2.InitialVariableValue));
        Functions = sorted;
    }

    /// <summary>
    /// Represents the expression's derivative.
    /// </summary>
    public Expression Derivative => _derivative ??= CreateDerivative();

    /// <summary>
    /// Represents the expression's integral.
    /// </summary>
    public Expression Integral => _integral ??= CreateIntegral();

    /// <summary>
    /// Initial value for variable.
    /// </summary>
    public double InitialVariableValue { get; }

    /// <summary>
    /// Final value for variable.
    /// </summary>
    public double FinalVariableValue { get; }

    /// <summary>
    /// List of functions.
    /// </summary>
    public List<Function> Functions { get; }

    /// <summary>
    /// Calculates the value for the expression.
    /// </summary>
    /// <param name="variableValue">Dependency variable for equation.</param>
    /// <returns></returns>
    public double Calculate(double variableValue)
    {
        double value = 0;
        foreach (var function in Functions)
        {
            if (function.InitialVariableValue <= variableValue && variableValue <= function.FinalVariableValue)
                value += function.Calculate(variableValue);
        }

        return value;
    }

    /// <summary>
    /// Creates the expression's derivative.
    /// </summary>
    protected Expression CreateDerivative()
    {
        var derivativeFunctions = new List<Function>();
        foreach (var function in Functions)
        {
            derivativeFunctions.Add(function.Derivative);
        }

        return new Expression(InitialVariableValue, FinalVariableValue, derivativeFunctions);
    }

    /// <summary>
    /// Creates the expression's integral.
    /// </summary>
    protected Expression CreateIntegral()
    {
        var integralFunctions = new List<Function>();
        foreach (var function in Functions)
        {
            integralFunctions.Add(function.Integral);
        }

        return new Expression(InitialVariableValue, FinalVariableValue, integralFunctions);
    }
}