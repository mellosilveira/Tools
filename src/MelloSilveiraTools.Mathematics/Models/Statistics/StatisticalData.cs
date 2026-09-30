namespace MelloSilveiraTools.Mathematics.Models.Statistics;

/// <summary>
/// Represents the statistical data of some information.
/// </summary>
/// <param name="Median">
/// Median is the middle number. It is found by ordering all data points and picking out the one in the middle (or 
/// if there are two middle numbers, taking the mean of those two numbers).
/// </param>
/// <param name="Mode">The most frequent value in the data set, or <see cref="double.NaN"/> if no single mode exists.</param>
/// <param name="Mean">The arithmetic mean of the values without outliers.</param>
/// <param name="Minimum">The minimum value without outliers.</param>
/// <param name="Maximum">The maximum value without outliers.</param>
/// <param name="LowerLimit">The lower threshold limit for outlier detection.</param>
/// <param name="UpperLimit">The upper threshold limit for outlier detection.</param>
/// <param name="StandardDeviation">The standard deviation of the values without outliers.</param>
/// <param name="Outliers">The detected outlier values.</param>
/// <param name="Values">The original input values.</param>
public record StatisticalData(
    double Median,
    double Mode,
    double Mean,
    double Minimum,
    double Maximum,
    double LowerLimit,
    double UpperLimit,
    double StandardDeviation,
    double[] Outliers,
    double[] Values
);
