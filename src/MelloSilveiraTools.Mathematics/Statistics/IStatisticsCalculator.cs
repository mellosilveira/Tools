using MelloSilveiraTools.Mathematics.Models.Statistics;

namespace MelloSilveiraTools.Mathematics.Statistics;

/// <summary>
/// Service contract for evaluating statistical distribution, central tendencies, and anomalous readings across datasets.
/// Enables business intelligence and quality teams to assess process stability, identify trends,
/// and isolate extreme outliers that could distort reporting and decision-making.
/// </summary>
public interface IStatisticsCalculator
{
    /// <summary>
    /// Computes summary metrics (mean, median, mode, spread) and isolates statistical outliers from the dataset.
    /// </summary>
    /// <param name="values">Collection of numerical observations or sample data points.</param>
    /// <param name="threshold">Sensitivity threshold multiplier for identifying extreme atypical readings.</param>
    /// <returns>A comprehensive summary containing central metrics, dispersion boundaries, and identified outliers.</returns>
    StatisticalData Calculate(double[] values, double threshold = 3.5);
}