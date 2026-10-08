namespace MelloSilveiraTools.Mathematics.NumericalMethods.Differentiations;

/// <summary>
/// Numerical differentiation engine that evaluates rate-of-change metrics from time-series and functions.
/// Delivers robust, fast mathematical derivation to drive segmentation and regime detection in physical pipelines.
/// </summary>
public class Differentiation : IDifferentiation
{
    /// <inheritdoc/>
    public double Calculate(Func<double, double> equation, double timeStep, double time)
    {
        double previous = equation(time - timeStep);
        double nextValue = equation(time + timeStep);

        return (nextValue - previous) / (2 * timeStep);
    }

    /// <inheritdoc/>
    public double Calculate(double initialPoint, double finalPoint, double step) => (finalPoint - initialPoint) / step;
}
