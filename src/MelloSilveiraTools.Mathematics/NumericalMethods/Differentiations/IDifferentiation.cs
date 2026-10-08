namespace MelloSilveiraTools.Mathematics.NumericalMethods.Differentiations;

/// <summary>
/// Contract for calculating rates of change and trend dynamics in experimental and time-series data.
/// Measures how fast physical metrics and operational indicators fluctuate over time,
/// enabling systems to detect velocities, accelerations, and deformation regime transitions.
/// </summary>
public interface IDifferentiation
{
    /// <summary>
    /// Calculates the instantaneous rate of change of a behavior at a specific point in time.
    /// </summary>
    /// <param name="equation">Mathematical function or behavior model to differentiate.</param>
    /// <param name="timeStep">Observation time step interval (in seconds).</param>
    /// <param name="time">Target timestamp where the rate of change is measured (in seconds).</param>
    /// <returns>The calculated instantaneous rate of change.</returns>
    double Calculate(Func<double, double> equation, double timeStep, double time);

    /// <summary>
    /// Computes the average rate of change between two successive observation values.
    /// </summary>
    /// <param name="initialPoint">Measurement value at the starting point.</param>
    /// <param name="finalPoint">Measurement value at the ending point.</param>
    /// <param name="step">Interval elapsed between the two observation points.</param>
    /// <returns>The average rate of change across the interval.</returns>
    double Calculate(double initialPoint, double finalPoint, double step);
}