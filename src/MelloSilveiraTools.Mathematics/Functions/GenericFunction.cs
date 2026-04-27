using MelloSilveiraTools.Mathematics.Models;

namespace MelloSilveiraTools.Mathematics.Functions;

/// <summary>
/// Represents a generic mathematical function which can be represented by any equation.
/// </summary>
public sealed record GenericFunction : Function
{
    private readonly Func<double, double> _function;
    private readonly Func<double, double> _derivativeFunction;
    private readonly Func<double, double> _integralFunction;

    /// <summary>
    /// Initializes a new instance of <see cref="GenericFunction"/>.
    /// </summary>
    /// <param name="initialVariableValue"></param>
    /// <param name="finalVariableValue"></param>
    /// <param name="function"></param>
    /// <param name="derivativeFunction"></param>
    /// <param name="integralFunction"></param>
    public GenericFunction(
        double? initialVariableValue,
        double? finalVariableValue,
        Func<double, double> function,
        Func<double, double> derivativeFunction,
        Func<double, double> integralFunction)
        : base(FunctionType.Generic, initialVariableValue, finalVariableValue, null)
    {
        _function = function ?? throw new ArgumentNullException(nameof(function));
        _derivativeFunction = derivativeFunction;
        _integralFunction = integralFunction;
    }

    /// <inheritdoc/>
    public override double Calculate(double variableValue) => _function(variableValue);

    /// <inheritdoc/>
    protected override Function CreateDerivative()
    {
        return new GenericFunction(InitialVariableValue, FinalVariableValue, _derivativeFunction, null, null);
    }

    /// <inheritdoc/>
    protected override Function CreateIntegral()
    {
        return new GenericFunction(InitialVariableValue, FinalVariableValue, _integralFunction, null, null);
    }
}
