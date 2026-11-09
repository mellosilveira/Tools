using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Mathematics.Models.NumericalMethods;
using MelloSilveiraTools.Mathematics.NumericalMethods.RootFindingAlgorithms;

namespace UnitTests.Mathematics.NumericalMethods.RootFindingAlgorithms;

public class BisectionMethodTests
{
    [Fact]
    public void FindRoot_ShouldConvergeToKnownRoot()
    {
        BisectionMethod bisection = new();
        // f(x) = x^2 - 4, root at x = 2
        Func<double, double> func = x => x * x - 4.0;
        RootFindingInput input = new(InitialPoint: 0.0, FinalPoint: 3.0, Tolerance: 1e-5, MaxIterations: 100);

        (double root, double error) = bisection.FindRoot(input, func);

        Assert.Equal(2.0, root, precision: 4);
        Assert.True(Math.Abs(error) < 1e-4);
    }

    [Fact]
    public void FindRoot_WhenSignsDoNotDiffer_ShouldThrowInvalidOperationException()
    {
        BisectionMethod bisection = new();
        // f(x) = x^2 - 4, on [3, 5] both points yield positive values (5 and 21)
        Func<double, double> func = x => x * x - 4.0;
        RootFindingInput input = new(InitialPoint: 3.0, FinalPoint: 5.0, Tolerance: 1e-5, MaxIterations: 100);

        Assert.Throws<InvalidOperationException>(() =>
        {
            bisection.FindRoot(input, func);
        });
    }

    [Fact]
    public void FindRoot_WhenMaxIterationsExceeded_ShouldThrowNonConvergenceException()
    {
        BisectionMethod bisection = new();
        Func<double, double> func = x => x * x - 4.0;
        RootFindingInput input = new(InitialPoint: 0.0, FinalPoint: 3.0, Tolerance: 1e-12, MaxIterations: 2);

        Assert.Throws<NonConvergenceException>(() =>
        {
            bisection.FindRoot(input, func);
        });
    }

    [Fact]
    public void TryFindRoot_ShouldReturnSuccess_WhenConvergenceSucceeds()
    {
        BisectionMethod bisection = new();
        Func<double, double> func = x => x * x - 4.0;
        RootFindingInput input = new(InitialPoint: 0.0, FinalPoint: 3.0, Tolerance: 1e-5, MaxIterations: 100);

        Result<(double Root, double Error)> result = bisection.TryFindRoot(input, func);

        Assert.True(result.Success);
        Assert.Equal(2.0, result.Data.Root, precision: 4);
        Assert.True(Math.Abs(result.Data.Error) < 1e-4);
    }

    [Fact]
    public void TryFindRoot_ShouldReturnFailure_WhenSignsDoNotDiffer()
    {
        BisectionMethod bisection = new();
        Func<double, double> func = x => x * x - 4.0;
        RootFindingInput input = new(InitialPoint: 3.0, FinalPoint: 5.0, Tolerance: 1e-5, MaxIterations: 100);

        Result<(double Root, double Error)> result = bisection.TryFindRoot(input, func);

        Assert.False(result.Success);
    }

    [Fact]
    public void TryFindRoot_ShouldReturnFailure_WhenMaxIterationsExceeded()
    {
        BisectionMethod bisection = new();
        Func<double, double> func = x => x * x - 4.0;
        RootFindingInput input = new(InitialPoint: 0.0, FinalPoint: 3.0, Tolerance: 1e-12, MaxIterations: 2);

        Result<(double Root, double Error)> result = bisection.TryFindRoot(input, func);

        Assert.False(result.Success);
    }
}
