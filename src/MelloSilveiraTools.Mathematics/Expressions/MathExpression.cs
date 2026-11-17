using MelloSilveiraTools.Mathematics.Functions;
using MelloSilveiraTools.Mathematics.Models;
using System.Text.Json.Serialization;

namespace MelloSilveiraTools.Mathematics.Expressions;

/// <summary>
/// Represents an unique dimension mathematical expression.
/// </summary>
public class MathExpression
{
    private readonly List<Function> _functions;
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
    public MathExpression(MathExpressionType type, List<Function> functions, double? initialVariableValue = null, double? finalVariableValue = null)
    {
        Type = type;
        InitialVariableValue = initialVariableValue ?? double.NegativeInfinity;
        FinalVariableValue = finalVariableValue ?? double.PositiveInfinity;

        // Build an internal sorted copy so the caller's list is never mutated.
        List<Function> sorted = [.. functions];
        sorted.Sort((f1, f2) => f1.InitialVariableValue.CompareTo(f2.InitialVariableValue));
        _functions = sorted;
    }

    /// <summary>
    /// Represents the expression's derivative.
    /// </summary>
    [JsonIgnore]
    public MathExpression Derivative => _derivative ??= CreateDerivative();

    /// <summary>
    /// Represents the expression's integral.
    /// </summary>
    [JsonIgnore]
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
    /// Gets the <see cref="Function"/> at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index of the function to retrieve.</param>
    /// <returns>The <see cref="Function"/> at the specified index.</returns>
    public Function this[int index] => _functions[index];

    /// <summary>
    /// Calculates the value for the expression.
    /// </summary>
    /// <param name="variableValue">Dependency variable for equation.</param>
    /// <returns></returns>
    public double Calculate(double variableValue)
    {
        double value = 0;
        foreach (Function function in _functions)
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
        List<Function> derivativeFunctions = [];
        foreach (Function function in _functions)
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
        List<Function> integralFunctions = [];
        foreach (Function function in _functions)
        {
            integralFunctions.Add(function.Integral);
        }

        return new MathExpression(Type, integralFunctions, InitialVariableValue, FinalVariableValue);
    }

    public static implicit operator MathExpression(Function function) => new(MathExpressionType.Generic, [function], function.InitialVariableValue, function.FinalVariableValue);
}
