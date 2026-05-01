namespace MelloSilveiraTools.Mathematics.NumericalMethods.Derivative;

/// <inheritdoc/>
public class Derivative : IDerivative
{
    /// <inheritdoc/>
    public double Calculate(Func<double, double> equation, double timeStep, double time)
    {
        double previous = equation(time - timeStep);
        double nextValue = equation(time + timeStep);

        return (nextValue - previous) / (2 * timeStep);
    }

    /// <inheritdoc/>
    public double Calculate(double initialPoint, double finalPoint, double step)
        => (finalPoint - initialPoint) / step;
}
