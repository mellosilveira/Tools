namespace MelloSilveiraTools.Mathematics.Models.Statistics;

/// <summary>
/// Represents the statistical data of some information.
/// </summary>
public record StatisticalData
{
    /// <summary>
    /// Median is the middle number. It is found by ordering all data points and picking out the one in the middle (or 
    /// if there are two middle numbers, taking the mean of those two numbers).
    /// </summary>
    public double Median { get; init; }

    // TODO: ESTUDAR COMO CALCULAR MODA.
    //public double Mode { get; init; }

    public double Mean { get; init; }

    public double Minimum { get; init; }

    public double Maximum { get; init; }

    public double LowerLimit { get; init; }

    public double UpperLimit { get; init; }

    public double StandardDeviation { get; init; }

    public double[] Outliers { get; init; }

    public double[] Values { get; init; }
}
