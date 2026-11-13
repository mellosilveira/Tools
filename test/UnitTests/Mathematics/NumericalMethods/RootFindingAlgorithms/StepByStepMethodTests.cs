using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Mathematics.Models.NumericalMethods;
using MelloSilveiraTools.Mathematics.NumericalMethods.RootFindingAlgorithms;
using Microsoft.Extensions.Logging;
using Moq;

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

    [Fact]
    public void TryFindRoot_ShouldReturnSuccess_WhenConvergenceSucceeds()
    {
        StepByStepMethod method = new();
        Func<double, double> func = x => Math.Abs(x - 3.0);
        RootFindingInput input = new(InitialPoint: 0.0, FinalPoint: 6.0, Tolerance: 0.1, MaxIterations: 60);

        Result<(double Root, double Error)> result = method.TryFindRoot(input, func);

        Assert.True(result.Success);
        Assert.Equal(3.0, result.Data.Root, precision: 1);
        Assert.True(result.Data.Error < 0.1);
    }

    [Fact]
    public void TryFindRoot_ShouldReturnFailure_WhenNoPointMeetsTolerance()
    {
        StepByStepMethod method = new();
        Func<double, double> func = x => 10.0 + x;
        RootFindingInput input = new(InitialPoint: 0.0, FinalPoint: 5.0, Tolerance: 0.01, MaxIterations: 10);

        Result<(double Root, double Error)> result = method.TryFindRoot(input, func);

        Assert.False(result.Success);
    }

    [Fact]
    public void TryFindRoot_WhenExceptionOccurs_ShouldLogExceptionAndReturnFailure()
    {
        Mock<ILogger<StepByStepMethod>> loggerMock = new();
        StepByStepMethod method = new(loggerMock.Object);
        Func<double, double> func = x => throw new InvalidOperationException("Simulation calculation failed");
        RootFindingInput input = new(InitialPoint: 0.0, FinalPoint: 5.0, Tolerance: 0.01, MaxIterations: 10);

        Result<(double Root, double Error)> result = method.TryFindRoot(input, func);

        Assert.False(result.Success);
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            Times.Once);
    }
}
