using MelloSilveiraTools.Mathematics.Expressions;

namespace MelloSilveiraTools.MechanicsOfMaterials.Models;

/// <summary>
/// Represents the behavior of a mechanical parameter over the time.
/// </summary>
public sealed record MechanicalParameter
{
    /// <summary>
    /// Initializes a new instance of <see cref="MechanicalParameter"/>.
    /// </summary>
    public MechanicalParameter() : this(0, null) { }

    /// <summary>
    /// Initializes a new instance of <see cref="MechanicalParameter"/>.
    /// </summary>
    /// <param name="initialValue"></param>
    public MechanicalParameter(double initialValue) : this(initialValue, null) { }

    /// <summary>
    /// Initializes a new instance of <see cref="MechanicalParameter"/>.
    /// </summary>
    /// <param name="initialValue"></param>
    /// <param name="expression"></param>
    public MechanicalParameter(double initialValue, MathExpression? expression)
    {
        InitialValue = initialValue;
        Expression = expression;
    }

    /// <summary>
    /// Unit: depends on mechanical parameter.
    /// </summary>
    public double InitialValue { get; private set; }

    /// <summary>
    /// Mathematical expression that represents how the mechanical parameter varies in time.
    /// </summary>
    public MathExpression? Expression { get; }

    /// <summary>
    /// Sets the initial value.
    /// </summary>
    /// <param name="initialValue"></param>
    public void SetInitialValue(double initialValue)
    {
        InitialValue = initialValue;
    }

    /// <summary>
    /// Calculates the value of a mechanical parameter.
    /// </summary>
    /// <param name="time">Unit: s (second).</param>
    /// <returns>Unit depends on which parameter is being calculated.</returns>
    public double CalculateValue(double time) => InitialValue + (Expression?.Calculate(time) ?? 0);

    /// <summary>
    /// Calculates the derivative of a mechanical parameter.
    /// </summary>
    /// <param name="time">Unit: s (second).</param>
    /// <returns>Unit depends on which parameter is being calculated.</returns>
    public double CalculateDerivative(double time) => Expression?.Derivative.Calculate(time) ?? 0;

    /// <summary>
    /// Calculates the value and its derivative of a mechanical parameter.
    /// </summary>
    /// <param name="time">Unit: s (second).</param>
    /// <returns>Unit depends on which parameter is being calculated.</returns>
    public (double Value, double Derivative) CalculateValueAndDerivative(double time) => (CalculateValue(time), CalculateDerivative(time));
}
