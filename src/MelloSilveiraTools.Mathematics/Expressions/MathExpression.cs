using MelloSilveiraTools.Mathematics.Functions;
using MelloSilveiraTools.Mathematics.Models;

namespace MelloSilveiraTools.Mathematics.Expressions;

/// <summary>
/// Represents an unique dimension mathematical expression.
/// </summary>
public class MathExpression : List<Function>
{
    private MathExpression? _derivative;
    private MathExpression? _integral;

    /// <summary>
    /// Initializes a new instance of <see cref="MathExpression"/>.
    /// </summary>
    /// <param name="type"></param>
    /// <param name="initialVariableValue"></param>
    /// <param name="finalVariableValue"></param>
    /// <param name="functions"></param>
    /// <exception cref="ArgumentNullException">When <paramref name="functions"/> is null or empty.</exception>
    public MathExpression(MathExpressionType type, List<Function> functions, double? initialVariableValue = null, double? finalVariableValue = null) : base(functions)
    {
        Type = type;
        InitialVariableValue = initialVariableValue ?? double.NegativeInfinity;
        FinalVariableValue = finalVariableValue ?? double.PositiveInfinity;

        // Build an internal sorted copy so the caller's list is never mutated.
        List<Function> sorted = [.. functions];
        sorted.Sort((f1, f2) => f1.InitialVariableValue.CompareTo(f2.InitialVariableValue));
        Functions = sorted;
    }

    /// <summary>
    /// Represents the expression's derivative.
    /// </summary>
    public MathExpression Derivative => _derivative ??= CreateDerivative();

    /// <summary>
    /// Represents the expression's integral.
    /// </summary>
    public MathExpression Integral => _integral ??= CreateIntegral();

    /// <inheritdoc cref="Models.MathExpressionType"/>
    public MathExpressionType Type { get; }

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
    protected MathExpression CreateDerivative()
    {
        var derivativeFunctions = new List<Function>();
        foreach (var function in Functions)
        {
            derivativeFunctions.Add(function.Derivative);
        }

        return new MathExpression(Type, derivativeFunctions, InitialVariableValue, FinalVariableValue);
    }

    /// <summary>
    /// Creates the expression's integral.
    /// </summary>
    protected MathExpression CreateIntegral()
    {
        var integralFunctions = new List<Function>();
        foreach (var function in Functions)
        {
            integralFunctions.Add(function.Integral);
        }

        return new MathExpression(Type, integralFunctions, InitialVariableValue, FinalVariableValue);
    }

    public static implicit operator MathExpression(Function function) => new(MathExpressionType.Generic, [function], function.InitialVariableValue, function.FinalVariableValue);
}