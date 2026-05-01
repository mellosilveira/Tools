using MelloSilveiraTools.Mathematics.Models.Statistics;

namespace MelloSilveiraTools.Mathematics.Statistics;

/// <summary>
/// Performs statistics calculations.
/// </summary>
public interface IStatisticsCalculator
{
    /// <summary>
    /// Calculates the statistics data.
    /// </summary>
    /// <param name="values"></param>
    /// <param name="threshold"></param>
    /// <returns></returns>
    StatisticalData Calculate(double[] values, double threshold = 3.5);
}