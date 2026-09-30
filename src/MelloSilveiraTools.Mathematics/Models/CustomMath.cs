namespace MelloSilveiraTools.Mathematics.Models;

/// <summary>
/// Provides constants and static methods for trigonometric, logarithmic, and other common mathematical functions.
/// </summary>
public static class CustomMath
{
    /// <summary>
    /// Calculates the cosine.
    /// </summary>
    /// <param name="angle">Unit: rad (radians).</param>
    /// <returns>
    /// A <see cref="Point3D"/> with the value of cosine foreach axis on <see cref="Vector3D"/>. 
    /// Unit: dimensionless.
    /// </returns>
    public static Point3D Cos(Vector3D angle) => Point3D.Create(Math.Cos(angle.X), Math.Cos(angle.Y), Math.Cos(angle.Z));

    /// <summary>
    /// Sums the <paramref name="values"/>.
    /// </summary>
    /// <param name="values"></param>
    /// <returns>The sum of values. Unit: depends on unit of values.</returns>
    public static double Sum(params IReadOnlyCollection<double> values)
    {
        double sum = 0;
        foreach (var value in values)
        {
            sum += value;
        }

        return sum;
    }

    /// <summary>
    /// Calculates the average between the <paramref name="values"/>.
    /// </summary>
    /// <param name="values"></param>
    /// <returns></returns>
    public static double Average(params IReadOnlyCollection<double> values) => Sum(values) / values.Count;

    /// <summary>
    /// Calculates the average time step (&Delta;t) across multiple arrays of time points.
    /// </summary>
    /// <param name="timePointsArrays">The arrays containing ordered time points.</param>
    /// <returns>The average interval between consecutive time points across all arrays, or 0 if no intervals exist.</returns>
    public static double CalculateAverageTimeStep(double[][] timePointsArrays)
    {
        double sum = 0;
        int count = 0;

        for (int i = 0; i < timePointsArrays.Length; i++)
        {
            double[] timePoints = timePointsArrays[i];
            for (int j = 1; j < timePoints.Length; j++)
            {
                sum += timePoints[j] - timePoints[j - 1];
                count++;
            }
        }

        return count > 0 ? sum / count : 0;
    }
}
