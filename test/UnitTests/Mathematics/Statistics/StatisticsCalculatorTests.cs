using MelloSilveiraTools.Mathematics.Models.Statistics;
using MelloSilveiraTools.Mathematics.Statistics;

namespace UnitTests.Mathematics.Statistics;

public class StatisticsCalculatorTests
{
    [Fact]
    public void Calculate_ShouldComputeAccurateCentralTendencyMetrics()
    {
        StatisticsCalculator calculator = new();
        double[] values = [2.0, 3.0, 4.0, 4.0, 5.0, 6.0];

        StatisticalData data = calculator.Calculate(values);

        Assert.Equal(4.0, data.Mean);
        Assert.Equal(4.0, data.Median);
        Assert.Equal(4.0, data.Mode);
        Assert.Equal(2.0, data.Minimum);
        Assert.Equal(6.0, data.Maximum);
        Assert.True(data.StandardDeviation > 0);
        Assert.Empty(data.Outliers);
    }

    [Fact]
    public void Calculate_ShouldIsolateExtremeOutliers()
    {
        StatisticsCalculator calculator = new();
        // 100.0 is an extreme outlier
        double[] values = [10.0, 10.2, 9.8, 10.1, 10.0, 100.0];

        StatisticalData data = calculator.Calculate(values, threshold: 3.5);

        Assert.Single(data.Outliers);
        Assert.Equal(100.0, data.Outliers[0]);
        Assert.True(100.0 > data.UpperLimit);
    }

    [Fact]
    public void Calculate_ShouldNotMutateCallerArray()
    {
        StatisticsCalculator calculator = new();
        double[] original = [5.0, 1.0, 3.0];
        double[] copy = (double[])original.Clone();

        calculator.Calculate(original);

        Assert.Equal(copy, original);
    }
}
