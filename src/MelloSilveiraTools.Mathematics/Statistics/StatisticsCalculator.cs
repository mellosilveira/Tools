using MelloSilveiraTools.Mathematics.Models.Statistics;
using MelloSilveiraTools.Mathematics.Extensions;

namespace MelloSilveiraTools.Mathematics.Statistics;

/// <inheritdoc cref="IStatisticsCalculator"/>
public class StatisticsCalculator : IStatisticsCalculator
{
    private const double ZScoreModifiedConstant = 0.6745;

    /// <inheritdoc/>
    public StatisticalData Calculate(double[] values, double threshold = 3.5)
    {
        // Clone before sorting so the caller's array is never mutated.
        var sorted = (double[])values.Clone();
        Array.Sort(sorted);

        var (valuesWithoutOutliers, outliers, lowerLimit, upperLimit) = CalculateOutliers(sorted, threshold);
        double mean = CalculateMean(valuesWithoutOutliers);
        return new StatisticalData
        {
            Median = CalculateMedian(valuesWithoutOutliers),
            Mean = mean,
            Minimum = valuesWithoutOutliers[0],
            Maximum = valuesWithoutOutliers[^1],
            LowerLimit = lowerLimit,
            UpperLimit = upperLimit,
            StandardDeviation = CalculateStandardDeviation(valuesWithoutOutliers, mean),
            Outliers = outliers,
            Values = values,
        };
    }

    private static double CalculateMedian(double[] sortedValues)
    {
        int midIndex = sortedValues.Length / 2;
        return sortedValues.Length % 2 == 0
            ? (sortedValues[midIndex - 1] + sortedValues[midIndex]) / 2.0
            : sortedValues[midIndex];
    }

    private static double CalculateMean(double[] values)
    {
        double sum = 0;
        for (int i = 0; i < values.Length; i++)
            sum += values[i];
        return sum / values.Length;
    }

    private static double CalculateStandardDeviation(double[] values, double mean)
    {
        double sumSquaredDifferences = 0;
        for (int i = 0; i < values.Length; i++)
            sumSquaredDifferences += (values[i] - mean).Squared();

        return Math.Sqrt(sumSquaredDifferences / values.Length);
    }

    private static (double[] ValuesWithoutOutliers, double[] Outliers, double LowerLimit, double UpperLimit) CalculateOutliers(double[] sortedValues, double threshold)
    {
        if (sortedValues.Length == 0)
            return ([], [], double.NaN, double.NaN);

        double median = CalculateMedian(sortedValues);

        // Compute absolute deviations without LINQ.
        var absoluteDeviations = new double[sortedValues.Length];
        for (int i = 0; i < sortedValues.Length; i++)
            absoluteDeviations[i] = Math.Abs(sortedValues[i] - median);
        Array.Sort(absoluteDeviations);

        double mad = CalculateMedian(absoluteDeviations);
        double lowerLimit = median - (threshold * mad) / ZScoreModifiedConstant;
        double upperLimit = median + (threshold * mad) / ZScoreModifiedConstant;

        // Pre-allocate arrays at max capacity, then slice to actual count.
        var withoutOutliers = new double[sortedValues.Length];
        var outliersArr = new double[sortedValues.Length];
        int withoutCount = 0, outliersCount = 0;

        foreach (double value in sortedValues)
        {
            if (value >= lowerLimit && value <= upperLimit)
                withoutOutliers[withoutCount++] = value;
            else
                outliersArr[outliersCount++] = value;
        }

        return (
            withoutOutliers[..withoutCount],
            outliersArr[..outliersCount],
            lowerLimit,
            upperLimit);
    }
}
