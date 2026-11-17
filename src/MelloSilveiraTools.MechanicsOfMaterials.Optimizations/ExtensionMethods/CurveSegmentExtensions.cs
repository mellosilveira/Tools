using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Models;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExtensionMethods;

/// <summary>
/// Provides extension methods for collections of <see cref="CurveSegment"/>.
/// </summary>
public static class CurveSegmentExtensions
{
    /// <summary>
    /// Calculates the average time step (Δt) directly across all curve segments in the collection.
    /// Utilizes the telescoping property of consecutive intervals for O(N_segments) complexity and zero heap allocations.
    /// </summary>
    /// <param name="curveSegments">The read-only collection of curve segments.</param>
    /// <returns>The average interval between consecutive time points across all segments, or 0.0 if no intervals exist.</returns>
    public static double CalculateAverageTimeStep(this IReadOnlyList<CurveSegment> curveSegments)
    {
        double sum = 0.0;
        int count = 0;

        for (int i = 0; i < curveSegments.Count; i++)
        {
            double[] timePoints = curveSegments[i].TimePoints;
            if (timePoints.Length > 1)
            {
                sum += timePoints[^1] - timePoints[0];
                count += timePoints.Length - 1;
            }
        }

        return count > 0 ? sum / count : 0.0;
    }

    /// <summary>
    /// Extracts the jagged array of time points from all curve segments without LINQ allocations.
    /// </summary>
    /// <param name="curveSegments">The read-only collection of curve segments.</param>
    /// <returns>A jagged array where each element is the time points array of the corresponding curve segment.</returns>
    public static double[][] GetTimePoints(this IReadOnlyList<CurveSegment> curveSegments)
    {
        double[][] timePointsArrays = new double[curveSegments.Count][];
        for (int i = 0; i < curveSegments.Count; i++)
        {
            timePointsArrays[i] = curveSegments[i].TimePoints;
        }
        return timePointsArrays;
    }

    /// <summary>
    /// Retrieves the elapsed time duration (Δt) of the first ramp segment found in the collection.
    /// </summary>
    /// <param name="curveSegments">The read-only collection of curve segments.</param>
    /// <returns>The duration of the first ramp segment if found and non-empty; otherwise, <see langword="null"/>.</returns>
    public static double? GetFirstRampTime(this IReadOnlyList<CurveSegment> curveSegments)
    {
        for (int i = 0; i < curveSegments.Count; i++)
        {
            if (curveSegments[i].Type == SegmentType.Ramp)
            {
                double[] timePoints = curveSegments[i].TimePoints;
                if (timePoints.Length > 0)
                {
                    return timePoints[^1] - timePoints[0];
                }
            }
        }
        return null;
    }
}
