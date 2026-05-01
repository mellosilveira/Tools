using MelloSilveiraTools.Mathematics.Models;

namespace MelloSilveiraTools.Mathematics.Functions;

/// <summary>
/// Represents a generic mathematical function which can be represented by any equation.
/// </summary>
/// <param name="initialVariableValue"></param>
/// <param name="finalVariableValue"></param>
/// <param name="function"></param>
/// <param name="derivativeFunction"></param>
/// <param name="integralFunction"></param>
public sealed class GenericFunction(
    double? initialVariableValue,
    double? finalVariableValue,
    Func<double, double> function,
    Func<double, double>? derivativeFunction,
    Func<double, double>? integralFunction) : Function(FunctionType.Generic, initialVariableValue, finalVariableValue, [])
{
    /// <inheritdoc/>
    public override double Calculate(double variableValue) => function(variableValue);

    /// <inheritdoc/>
    protected override Function CreateDerivative() => derivativeFunction is not null
        ? new GenericFunction(InitialVariableValue, FinalVariableValue, derivativeFunction, null, null)
        : throw new ArgumentNullException(nameof(derivativeFunction));

    /// <inheritdoc/>
    protected override Function CreateIntegral() => integralFunction is not null
        ? new GenericFunction(InitialVariableValue, FinalVariableValue, integralFunction, null, null)
        : throw new ArgumentNullException(nameof(integralFunction));
}
