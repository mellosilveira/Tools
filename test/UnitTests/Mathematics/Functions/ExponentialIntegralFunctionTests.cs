using MelloSilveiraTools.Mathematics.Functions;
using MelloSilveiraTools.Mathematics.Models;

namespace UnitTests.Mathematics.Functions;

public class ExponentialIntegralFunctionTests
{
    private const double Precision = 1e-10;

    [Theory]
    [InlineData(0.1, 1.82292395841939)]
    [InlineData(0.5, 0.55977359477616)]
    [InlineData(1.0, 0.21938393439552)]
    [InlineData(2.0, 0.04890051070806)]
    [InlineData(10.0, 4.156968929685e-6)]
    public void Calculate_WithStandardPoints_ShouldMatchKnownAnalyticalValues(double x, double expected)
    {
        ExponentialIntegralFunction func = new();

        double actual = func.Calculate(x);

        Assert.Equal(expected, actual, 8);
    }

    [Fact]
    public void Calculate_WithZeroOrNegative_ShouldHandleBoundaryCorrectly()
    {
        ExponentialIntegralFunction func = new();

        double zeroResult = func.Calculate(0.0);
        double nearZeroResult = func.Calculate(1e-16);
        double negativeResult = func.Calculate(-1.0);

        Assert.True(double.IsPositiveInfinity(zeroResult));
        Assert.True(double.IsPositiveInfinity(nearZeroResult));
        Assert.True(double.IsNaN(negativeResult));
    }

    [Fact]
    public void Constructor_WithTolerance_ShouldCalculateMaximumAcceptedVariableValue()
    {
        const double tolerance = 1e-6;
        ExponentialIntegralFunction func = new(tolerance);

        Assert.Equal(-Math.Log(tolerance), func._maximumAcceptedVariableValue, 8);
    }

    [Fact]
    public void Calculate_WhenExceedingMaximumAcceptedVariableValue_ShouldReturnZero()
    {
        const double tolerance = 1e-6;
        ExponentialIntegralFunction func = new(tolerance);
        double maxAcceptedX = func._maximumAcceptedVariableValue;

        double valueBefore = func.Calculate(maxAcceptedX - 0.1);
        double valueAfter = func.Calculate(maxAcceptedX + 0.1);

        Assert.True(valueBefore > 0.0);
        Assert.Equal(0.0, valueAfter);
    }

    [Fact]
    public void Calculate_ShouldEvaluateSingleExponentialIntegralFunction()
    {
        ExponentialIntegralFunction func = new();

        double atOne = func.Calculate(1.0);
        double atTwoPointFive = func.Calculate(2.5);

        Assert.Equal(FunctionType.ExponentialIntegral, func.FunctionType);
        Assert.Empty(func.Coefficients);
        Assert.True(atOne > 0.0);
        Assert.True(atTwoPointFive > 0.0);
    }

    [Fact]
    public void Derivative_ShouldCalculateNegativeExpDividedByX()
    {
        ExponentialIntegralFunction func = new();
        Function derivative = func.Derivative;

        // d/dx [E1(x)] = -exp(-x) / x. At x = 1: -exp(-1) / 1 = -1/e
        double expected = -Math.Exp(-1.0);
        double actual = derivative.Calculate(1.0);

        Assert.Equal(expected, actual, Precision);
    }

    [Fact]
    public void Integral_ShouldThrowNotSupportedException()
    {
        ExponentialIntegralFunction func = new();

        Assert.Throws<NotSupportedException>(() =>
        {
            _ = func.Integral;
        });
    }

    [Fact]
    public void Calculate_WithEqualLimits_ShouldReturnZero()
    {
        ExponentialIntegralFunction func = new();

        double actual = func.Calculate(2.5, 2.5);

        Assert.Equal(0.0, actual);
    }

    [Fact]
    public void Calculate_WithInvertedLimits_ShouldReturnNegativeOfDirectOrder()
    {
        ExponentialIntegralFunction func = new();

        double forward = func.Calculate(1.0, 3.0);
        double backward = func.Calculate(3.0, 1.0);

        Assert.Equal(forward, -backward, 10);
    }

    [Fact]
    public void Calculate_WithinPowerSeriesDomain_ShouldMatchIndividualDifference()
    {
        ExponentialIntegralFunction func = new();
        double lower = 0.1;
        double upper = 0.5;

        double expected = func.Calculate(lower) - func.Calculate(upper);
        double actual = func.Calculate(lower, upper);

        Assert.Equal(expected, actual, 10);
    }

    [Fact]
    public void Calculate_AcrossThreshold_ShouldMatchIndividualDifference()
    {
        ExponentialIntegralFunction func = new();
        double lower = 0.5;
        double upper = 2.0;

        double expected = func.Calculate(lower) - func.Calculate(upper);
        double actual = func.Calculate(lower, upper);

        Assert.Equal(expected, actual, 10);
    }

    [Fact]
    public void Calculate_WithBothLimitsBeyondCutoff_ShouldReturnZero()
    {
        const double tolerance = 1e-6;
        ExponentialIntegralFunction func = new(tolerance);
        double maxAcceptedX = func._maximumAcceptedVariableValue;

        double actual = func.Calculate(maxAcceptedX + 1.0, maxAcceptedX + 5.0);

        Assert.Equal(0.0, actual);
    }

    [Fact]
    public void Calculate_WithBothLimitsBelowTolerance_ShouldAnalyticallyResolveLogarithmicRatio()
    {
        ExponentialIntegralFunction func = new();
        double lower = 1e-16;
        double upper = 1e-15;

        double expected = Math.Log(upper / lower);
        double actual = func.Calculate(lower, upper);

        Assert.Equal(expected, actual, 8);
    }

    [Fact]
    public void Calculate_WithLowerLimitZero_ShouldReturnPositiveInfinity()
    {
        ExponentialIntegralFunction func = new();

        double actual = func.Calculate(0.0, 2.0);

        Assert.True(double.IsPositiveInfinity(actual));
    }

    [Fact]
    public void Calculate_WithNegativeLimit_ShouldReturnNaN()
    {
        ExponentialIntegralFunction func = new();

        double lowerNegative = func.Calculate(-1.0, 2.0);
        double upperNegative = func.Calculate(1.0, -2.0);

        Assert.True(double.IsNaN(lowerNegative));
        Assert.True(double.IsNaN(upperNegative));
    }
}
