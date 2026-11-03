using MelloSilveiraTools.Core.Pipelines.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Algorithms;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.MathematicalFunctions;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Models;
using Moq;

namespace UnitTests.MechanicsOfMaterials.Optimizations.CurveFitting.MathematicalFunctions;

public class PolynomialCurveFitterTests
{
    [Fact]
    public void TryFit_UnconstrainedPolynomial_ShouldUseAnalyticalVandermondeSolutionWithoutCallingInnerFitter()
    {
        // y = 2 + 3x + 1.5x^2
        double[] x = [0.0, 1.0, 2.0, 3.0, 4.0];
        double[] y = [2.0, 6.5, 14.0, 24.5, 38.0];

        Mock<ICurveFitter> mockInnerFitter = new(MockBehavior.Strict);
        PolynomialCurveFitter fitter = new(mockInnerFitter.Object);

        MathematicalCurveFitInput input = new()
        {
            NumberOfParameters = 3,
            IndependentVariable = x,
            DependentVariable = y,
            Profile = CurveFitProfile.Automatic
        };

        SafeResult<CurveFitInput, CurveFitOutput> result = fitter.TryFit(input);

        Assert.True(result.Success);
        Assert.NotNull(result.Output);
        Assert.Equal(1, result.Output.Iterations);
        Assert.True(result.Output.RSquared >= 0.9999);
        Assert.Equal(2.0, result.Output.OptimizedParameters[0], precision: 4);
        Assert.Equal(3.0, result.Output.OptimizedParameters[1], precision: 4);
        Assert.Equal(1.5, result.Output.OptimizedParameters[2], precision: 4);
        mockInnerFitter.Verify(f => f.Fit(It.IsAny<CurveFitInput>()), Times.Never);
    }

    [Fact]
    public void TryFit_WhenBoundsViolated_ShouldFallbackToInnerFitterWithVandermondeInitialParameters()
    {
        // y = 2 + 3x, but UpperBounds forces slope <= 1.0
        double[] x = [0.0, 1.0, 2.0, 3.0];
        double[] y = [2.0, 5.0, 8.0, 11.0];

        CurveFitOutput expectedOutput = new([2.0, 1.0], 5.0, 0.85, 10);
        Mock<ICurveFitter> mockInnerFitter = new();
        mockInnerFitter.Setup(f => f.Fit(It.IsAny<CurveFitInput>())).Returns(expectedOutput);

        PolynomialCurveFitter fitter = new(mockInnerFitter.Object);

        MathematicalCurveFitInput input = new()
        {
            NumberOfParameters = 2,
            IndependentVariable = x,
            DependentVariable = y,
            LowerBounds = [0.0, 0.0],
            UpperBounds = [10.0, 1.0], // slope constraint violated by analytical 3.0
            Profile = CurveFitProfile.Standard
        };

        SafeResult<CurveFitInput, CurveFitOutput> result = fitter.TryFit(input);

        Assert.True(result.Success);
        Assert.Same(expectedOutput, result.Output);
        mockInnerFitter.Verify(f => f.Fit(It.Is<CurveFitInput>(c =>
            Math.Abs(c.InitialParameters[0] - 2.0) < 1e-4 &&
            Math.Abs(c.InitialParameters[1] - 3.0) < 1e-4)), Times.Once);
    }

    [Fact]
    public void TryFit_NumberOfParametersGreaterThanPoints_ShouldReturnFailure()
    {
        double[] x = [1.0, 2.0];
        double[] y = [3.0, 4.0];

        Mock<ICurveFitter> mockInnerFitter = new();
        PolynomialCurveFitter fitter = new(mockInnerFitter.Object);

        MathematicalCurveFitInput input = new()
        {
            NumberOfParameters = 3,
            IndependentVariable = x,
            DependentVariable = y
        };

        SafeResult<CurveFitInput, CurveFitOutput> result = fitter.TryFit(input);

        Assert.False(result.Success);
        Assert.NotNull(result.FailedPayload);
        Assert.IsType<ArgumentException>(result.FailedPayload.Value.Exception);
    }
}
