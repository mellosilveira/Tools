using MelloSilveiraTools.Mathematics.Models.NumericalMethods;

namespace MelloSilveiraTools.Mathematics.NumericalMethods.Integrals;

/// <summary>
/// Represents a numerical integration rule.
/// </summary>
public interface IIntegration
{
    /// <summary>
    /// Calculates the integral of a function.
    /// </summary>
    /// <param name="equation">The equation to be integrated.</param>
    /// <param name="integralInput">The inputs for integral.</param>
    /// <returns>The value of integration.</returns>
    double Calculate(Func<double, double> equation, IntegralInput integralInput);
}
