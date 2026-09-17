namespace MelloSilveiraTools.Mathematics.Models.Statistics;

/// <summary>
/// Represents the statistical data of some information.
/// </summary>
/// <param name="Median">
/// Median is the middle number. It is found by ordering all data points and picking out the one in the middle (or 
/// if there are two middle numbers, taking the mean of those two numbers).
/// </param>
/// <param name="Mean"></param>
/// <param name="Minimum"></param>
/// <param name="Maximum"></param>
/// <param name="LowerLimit"></param>
/// <param name="UpperLimit"></param>
/// <param name="StandardDeviation"></param>
/// <param name="Outliers"></param>
/// <param name="Values"></param>
public record StatisticalData(
    double Median,
    // TODO: ESTUDAR COMO CALCULAR MODA.
    // double Mode,
    double Mean,
    double Minimum,
    double Maximum,
    double LowerLimit,
    double UpperLimit,
    double StandardDeviation,
    double[] Outliers,
    double[] Values
);
