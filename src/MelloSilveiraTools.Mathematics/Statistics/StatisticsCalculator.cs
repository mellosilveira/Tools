using MelloSilveiraTools.Mathematics.Models.Statistics;
using SoftTissue.SharedModules.ExtensionMethods;

namespace MelloSilveiraTools.Mathematics.Statistics;

/// <inheritdoc cref="IStatisticsCalculator"/>
public class StatisticsCalculator : IStatisticsCalculator
{
    private const double ZScoreModifiedConstant = 0.6745;

    /// <inheritdoc/>
    public StatisticalData Calculate(double[] values, double threshold = 3.5)
    {
        var originalValeus = (double[])values.Clone();

        Array.Sort(values);

        var (valuesWithoutOutliers, outliers, lowerLimit, upperLimit) = CalculateOutliers(values, threshold);
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
            Values = originalValeus,
        };
    }

    private static double CalculateMedian(double[] sortedValues)
    {
        int midIndex = sortedValues.Length / 2;
        return sortedValues.Length % 2 == 0
            ? (sortedValues[midIndex - 1] + sortedValues[midIndex]) / 2.0
            : sortedValues[midIndex];
    }

    private static double CalculateMean(double[] values) => values.Average();

    private static double CalculateStandardDeviation(double[] values, double mean)
    {
        double sumSquaredDifferences = 0;
        for (int i = 0; i < values.Length; i++)
        {
            sumSquaredDifferences += (values[i] - mean).Squared();
        }

        return Math.Sqrt(sumSquaredDifferences / values.Length);
    }

    private static (double[] ValuesWithoutOutliers, double[] Outliers, double LowerLimit, double UpperLimit) CalculateOutliers(double[] sortedValues, double threshold)
    {
        if (sortedValues.Length == 0)
            return ([], [], double.NaN, double.NaN);

        double median = CalculateMedian(sortedValues);
        double[] absoluteDeviations = sortedValues.Select(v => Math.Abs(v - median)).ToArray();
        Array.Sort(absoluteDeviations);

        double mad = CalculateMedian(absoluteDeviations);
        double lowerLimit = median - (threshold * mad) / ZScoreModifiedConstant;
        double upperLimit = median + (threshold * mad) / ZScoreModifiedConstant;

        List<double> valuesWithoutOutliers = new(sortedValues.Length);
        List<double> outliers = [];

        foreach (var value in sortedValues)
        {
            if (value >= lowerLimit && value <= upperLimit)
            {
                valuesWithoutOutliers.Add(value);
            }
            else
            {
                outliers.Add(value);
            }
        }

        return (valuesWithoutOutliers.ToArray(), outliers.ToArray(), lowerLimit, upperLimit);
    }
}
