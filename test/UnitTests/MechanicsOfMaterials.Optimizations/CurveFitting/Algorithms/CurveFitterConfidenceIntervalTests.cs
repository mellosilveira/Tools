using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Algorithms;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace UnitTests.MechanicsOfMaterials.Optimizations.CurveFitting.Algorithms;

public class CurveFitterConfidenceIntervalTests
{
    [Fact]
    public void Fit_CalculatesConfidenceIntervals_SymmetricAndCorrectLevel()
    {
        // Arrange: linear model y = p0 * x + p1 with slight deviations
        AlglibCurveFitter fitter = new(NullLogger<AlglibCurveFitter>.Instance, new CurveFitterSettings());

        double[] xValues = [1.0, 2.0, 3.0, 4.0, 5.0, 6.0, 7.0, 8.0, 9.0, 10.0];
        double[] yValues = [7.1, 8.9, 11.2, 12.8, 15.1, 16.9, 19.2, 20.8, 23.1, 24.9];

        CurveFitInput input = new()
        {
            IndependentVariables = [xValues],
            DependentVariable = yValues,
            InitialParameters = [1.0, 1.0],
            LowerBounds = [-100.0, -100.0],
            UpperBounds = [100.0, 100.0],
            Calculate = (p, x) => p[0] * x[0] + p[1],
            Profile = CurveFitProfile.Standard,
            ConfidenceLevel = 0.95
        };

        // Act
        CurveFitOutput output = fitter.Fit(input);

        // Assert: unified Parameters property contains both optimized values and intervals
        Assert.NotNull(output.Parameters);
        Assert.Equal(2, output.Parameters.Count);
        Assert.Equal(output.Parameters.Count, output.OptimizedParameters.Length);

        for (int i = 0; i < output.Parameters.Count; i++)
        {
            ParameterConfidenceInterval parameter = output.Parameters[i];
            Assert.Equal(i, parameter.Index);
            Assert.Equal(output.OptimizedParameters[i], parameter.Value, precision: 6);
            Assert.Equal(0.95, parameter.ConfidenceLevel, precision: 4);
            Assert.True(parameter.StandardError > 0.0);
            Assert.True(parameter.LowerBound < parameter.Value);
            Assert.True(parameter.UpperBound > parameter.Value);

            double marginBelow = parameter.Value - parameter.LowerBound;
            double marginAbove = parameter.UpperBound - parameter.Value;
            Assert.Equal(marginBelow, marginAbove, precision: 6);
        }
    }

    [Fact]
    public void Fit_HigherConfidenceLevel_ProducesWiderIntervalsMonotonically()
    {
        // Arrange
        AlglibCurveFitter fitter = new(NullLogger<AlglibCurveFitter>.Instance, new CurveFitterSettings());

        double[] xValues = [1.0, 2.0, 3.0, 4.0, 5.0, 6.0, 7.0, 8.0];
        double[] yValues = [2.2, 3.8, 6.1, 8.2, 10.1, 11.9, 14.2, 15.8];

        CurveFitInput input90 = new()
        {
            IndependentVariables = [xValues],
            DependentVariable = yValues,
            InitialParameters = [1.0, 1.0],
            LowerBounds = [-100.0, -100.0],
            UpperBounds = [100.0, 100.0],
            Calculate = (p, x) => p[0] * x[0] + p[1],
            Profile = CurveFitProfile.Standard,
            ConfidenceLevel = 0.90
        };

        CurveFitInput input95 = input90 with { ConfidenceLevel = 0.95 };
        CurveFitInput input99 = input90 with { ConfidenceLevel = 0.99 };

        // Act
        CurveFitOutput output90 = fitter.Fit(input90);
        CurveFitOutput output95 = fitter.Fit(input95);
        CurveFitOutput output99 = fitter.Fit(input99);

        // Assert: for each parameter, margin of error widens as confidence increases
        for (int i = 0; i < 2; i++)
        {
            double margin90 = output90.Parameters[i].UpperBound - output90.Parameters[i].Value;
            double margin95 = output95.Parameters[i].UpperBound - output95.Parameters[i].Value;
            double margin99 = output99.Parameters[i].UpperBound - output99.Parameters[i].Value;

            Assert.True(margin95 > margin90, $"Expected 95% margin ({margin95}) > 90% margin ({margin90}) for parameter {i}");
            Assert.True(margin99 > margin95, $"Expected 99% margin ({margin99}) > 95% margin ({margin95}) for parameter {i}");

            // The 99% interval strictly encloses the 95% interval, which encloses the 90% interval
            Assert.True(output99.Parameters[i].LowerBound < output95.Parameters[i].LowerBound);
            Assert.True(output95.Parameters[i].LowerBound < output90.Parameters[i].LowerBound);
            Assert.True(output99.Parameters[i].UpperBound > output95.Parameters[i].UpperBound);
            Assert.True(output95.Parameters[i].UpperBound > output90.Parameters[i].UpperBound);
        }
    }

    [Fact]
    public void Fit_MathNetFitter_CalculatesConfidenceIntervalsSuccessfully()
    {
        // Arrange
        MathNetCurveFitter fitter = new();

        double[] xValues = [1.0, 2.0, 3.0, 4.0, 5.0, 6.0];
        double[] yValues = [3.1, 4.9, 7.2, 8.8, 11.1, 13.0];

        CurveFitInput input = new()
        {
            IndependentVariables = [xValues],
            DependentVariable = yValues,
            InitialParameters = [1.0, 1.0],
            LowerBounds = [-100.0, -100.0],
            UpperBounds = [100.0, 100.0],
            Calculate = (p, x) => p[0] * x[0] + p[1],
            ConfidenceLevel = 0.95
        };

        // Act
        CurveFitOutput output = fitter.Fit(input);

        // Assert
        Assert.NotNull(output.Parameters);
        Assert.Equal(2, output.Parameters.Count);
        Assert.True(output.Parameters[0].StandardError > 0.0);
        Assert.True(output.Parameters[1].StandardError > 0.0);
        Assert.True(output.Parameters[0].LowerBound < output.Parameters[0].UpperBound);
    }

    [Fact]
    public void Fit_InsufficientDegreesOfFreedom_ReturnsFallbackIntervalsWithoutFaulting()
    {
        // Arrange: 2 points for 2 parameters (degrees of freedom = 0)
        AlglibCurveFitter fitter = new(NullLogger<AlglibCurveFitter>.Instance, new CurveFitterSettings());

        double[] xValues = [1.0, 2.0];
        double[] yValues = [3.0, 5.0];

        CurveFitInput input = new()
        {
            IndependentVariables = [xValues],
            DependentVariable = yValues,
            InitialParameters = [1.0, 1.0],
            LowerBounds = [-100.0, -100.0],
            UpperBounds = [100.0, 100.0],
            Calculate = (p, x) => p[0] * x[0] + p[1],
            Profile = CurveFitProfile.Standard,
            ConfidenceLevel = 0.95
        };

        // Act
        CurveFitOutput output = fitter.Fit(input);

        // Assert: should return non-null fallback intervals with bounds equal to parameter values
        Assert.NotNull(output.Parameters);
        Assert.Equal(2, output.Parameters.Count);
        for (int i = 0; i < 2; i++)
        {
            Assert.Equal(output.OptimizedParameters[i], output.Parameters[i].Value);
            Assert.Equal(output.OptimizedParameters[i], output.Parameters[i].LowerBound);
            Assert.Equal(output.OptimizedParameters[i], output.Parameters[i].UpperBound);
        }
    }

    [Fact]
    public void PolynomialCurveFitter_AnalyticalFit_CalculatesConfidenceIntervals()
    {
        // y = 2 + 3x with slight residual variation
        double[] x = [0.0, 1.0, 2.0, 3.0, 4.0, 5.0];
        double[] y = [2.1, 4.9, 8.2, 10.9, 14.2, 16.8];

        Moq.Mock<ICurveFitter> mockInner = new(Moq.MockBehavior.Strict);
        MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.MathematicalFunctions.PolynomialCurveFitter fitter = new(mockInner.Object);

        MathematicalCurveFitInput input = new()
        {
            NumberOfParameters = 2,
            IndependentVariable = x,
            DependentVariable = y,
            Profile = CurveFitProfile.Automatic,
            ConfidenceLevel = 0.95
        };

        MelloSilveiraTools.Core.Pipelines.Models.SafeResult<CurveFitInput, CurveFitOutput> result = fitter.TryFit(input);

        Assert.True(result.Success);
        Assert.NotNull(result.Output);
        Assert.Equal(2, result.Output.Parameters.Count);
        Assert.All(result.Output.Parameters, p =>
        {
            Assert.True(p.StandardError > 0.0);
            Assert.True(p.LowerBound < p.Value);
            Assert.True(p.UpperBound > p.Value);
            Assert.Equal(0.95, p.ConfidenceLevel);
        });
    }
}
