namespace MelloSilveiraTools.Mathematics.NumericalMethods.Differentiations;

/// <summary>
/// Represents the numerical derivative.
/// </summary>
public interface IDifferentiation
{
    /// <summary>
    /// Calculates the derivative of a function.
    /// </summary>
    /// <param name="equation">The equation to be derivated.</param>
    /// <param name="timeStep">Unit: s (second).</param>
    /// <param name="time">Unit: s (second).</param>
    /// <returns>The value of derivative.</returns>
    double Calculate(Func<double, double> equation, double timeStep, double time);

    /// <summary>
    /// Calculates the derivative between two points.
    /// </summary>
    /// <param name="initialPoint">The initial point of equation.</param>
    /// <param name="finalPoint">The final point of equation.</param>
    /// <param name="step">The step to be used in derivative.</param>
    /// <returns>The value of derivative.</returns>
    double Calculate(double initialPoint, double finalPoint, double step);
}