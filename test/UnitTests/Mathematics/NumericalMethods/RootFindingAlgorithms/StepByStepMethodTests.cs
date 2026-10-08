using MelloSilveiraTools.Mathematics.Models.NumericalMethods;
using MelloSilveiraTools.Mathematics.NumericalMethods.RootFindingAlgorithms;

namespace UnitTests.Mathematics.NumericalMethods.RootFindingAlgorithms;

public class StepByStepMethodTests
{
    [Fact]
    public void FindRoot_ShouldFindPointMeetingTolerance()
    {
        StepByStepMethod method = new();
        // Target zero at x = 3.0
        Func<double, double> func = x => Math.Abs(x - 3.0);
        RootFindingInput input = new(InitialPoint: 0.0, FinalPoint: 6.0, Tolerance: 0.1, MaxIterations: 60);

        (double root, double error) = method.FindRoot(input, func);

        Assert.Equal(3.0, root, precision: 1);
        Assert.True(error < 0.1);
    }

    [Fact]
    public void FindRoot_WhenNoPointMeetsTolerance_ShouldThrowNonConvergenceException()
    {
        StepByStepMethod method = new();
        // Values are all >= 10.0
        Func<double, double> func = x => 10.0 + x;
        RootFindingInput input = new(InitialPoint: 0.0, FinalPoint: 5.0, Tolerance: 0.01, MaxIterations: 10);

        Assert.Throws<NonConvergenceException>(() =>
        {
            method.FindRoot(input, func);
        });
    }
}
