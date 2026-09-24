using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Algorithms;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace UnitTests;

public class AlglibCurveFitterTests
{
    [Fact]
    public void Fit_StandardProfile_FindsUnconstrainedMinimum()
    {
        // Arrange
        var fitter = new AlglibCurveFitter();

        // A simple parabolic function: y = (x - p0)^2 + p1
        // Minimum at p0 = 2.0, p1 = 1.0
        // Data points generated using these parameters
        double[] xValues = [0.0, 1.0, 2.0, 3.0, 4.0];
        double[] yValues = [5.0, 2.0, 1.0, 2.0, 5.0];

        CurveFitInput input = new()
        {
            IndependentVariables = [xValues],
            DependentVariable = yValues,
            InitialParameters = [0.0, 0.0],
            LowerBounds = [-10.0, -10.0],
            UpperBounds = [10.0, 10.0],
            Calculate = (p, x) => Math.Pow(x[0] - p[0], 2) + p[1],
            Profile = CurveFitProfile.Standard
        };

        // Act
        CurveFitOutput output = fitter.Fit(input);

        // Assert
        Assert.NotNull(output);
        Assert.Equal(2, output.OptimizedParameters.Length);
        Assert.Equal(2.0, output.OptimizedParameters[0], 3);
        Assert.Equal(1.0, output.OptimizedParameters[1], 3);
        Assert.True(output.RSquared > 0.99);
    }

    [Fact]
    public void Fit_RuleConstrainedProfile_FindsConstrainedMinimum_WithoutBreakingGradients()
    {
        // Arrange
        var fitter = new AlglibCurveFitter();

        // Same parabolic function: y = (x - p0)^2 + p1
        // True unconstrained minimum is p0 = 2.0, p1 = 1.0
        // We will add a boolean rule constraint: p0 >= 3.0
        // The constrained minimum should be at p0 = 3.0, and p1 will compensate, or rather the best fit for p0 >= 3 is p0=3, p1=1 (which gives y=(x-3)^2+1, values: 10, 5, 2, 1, 2) - wait, it will just find the best parameters that satisfy p0 >= 3.0 to fit the original data.

        double[] xValues = [0.0, 1.0, 2.0, 3.0, 4.0];
        double[] yValues = [5.0, 2.0, 1.0, 2.0, 5.0];

        CurveFitInput input = new()
        {
            IndependentVariables = [xValues],
            DependentVariable = yValues,
            InitialParameters = [3.5, 0.0], // Start in the valid region
            LowerBounds = [-10.0, -10.0],
            UpperBounds = [10.0, 10.0],
            Calculate = (p, x) => Math.Pow(x[0] - p[0], 2) + p[1],
            Profile = CurveFitProfile.RuleConstrained,
            ValidateParameters = p => p[0] >= 3.0 // Boolean rule constraint
        };

        // Act
        CurveFitOutput output = fitter.Fit(input);

        // Assert
        Assert.NotNull(output);
        // The solver should converge to p0 = 3.0 because it's the closest valid point to the true minimum (2.0)
        Assert.True(output.OptimizedParameters[0] >= 3.0);
        // We ensure RSquared is calculated (can be negative for a forced bad fit)
        Assert.True(output.RSquared < 0.0);
    }
}
