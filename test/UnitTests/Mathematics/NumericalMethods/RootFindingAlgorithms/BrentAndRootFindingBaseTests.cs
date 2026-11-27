using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Mathematics.Models.NumericalMethods;
using MelloSilveiraTools.Mathematics.NumericalMethods;
using MelloSilveiraTools.Mathematics.NumericalMethods.RootFindingAlgorithms;
using Microsoft.Extensions.Logging.Abstractions;

namespace UnitTests.Mathematics.NumericalMethods.RootFindingAlgorithms;

/// <summary>
/// Unit tests for <see cref="BrentMethod"/>, <see cref="RootFinding"/>, and <see cref="NumericalMethodOutput"/>.
/// </summary>
public sealed class BrentAndRootFindingBaseTests
{
    private sealed class TestRootFinder : RootFinding
    {
        public override (double Root, double Error) FindRoot(RootFindingInput input, Func<double, double> function)
            => (42.0, 0.0);

        public Exception BuildNonConvergence(double point, double value) => GetNonConvergenceException(point, value);
    }

    [Fact]
    public void BrentMethod_FindRootAndTryFindRoot_ConvergeForNonlinearFunctions()
    {
        BrentMethod brent = new(NullLogger<BrentMethod>.Instance);
        // f(x) = x^3 - x - 2, unique real root near 1.5213797068045676
        Func<double, double> cubic = x => x * x * x - x - 2.0;
        RootFindingInput input = new(1.0, 2.0, 1e-10, 100);

        (double root, double error) = brent.FindRoot(input, cubic);
        Assert.Equal(1.5213797068045676, root, precision: 9);
        Assert.True(Math.Abs(error) < 1e-9);

        // Linear root and endpoint root edge cases.
        Result<(double Root, double Error)> linearResult = brent.TryFindRoot(new RootFindingInput(0.0, 3.0, 1e-6, 100), x => x * x - 4.0);
        Assert.True(linearResult.Success);
        Assert.Equal(2.0, linearResult.Data.Root, precision: 6);

        (double endpointRoot, double endpointError) = brent.FindRoot(new RootFindingInput(2.0, 5.0, 1e-6, 10), x => x - 2.0);
        Assert.Equal(2.0, endpointRoot);
        Assert.Equal(0.0, endpointError);
    }

    [Fact]
    public void BrentMethod_WhenSignsDoNotDifferOrMaxIterationsExceeded_ThrowsAndTryFindRootFails()
    {
        BrentMethod brent = new(NullLogger<BrentMethod>.Instance);
        Func<double, double> func = x => x * x - 4.0;

        Assert.Throws<InvalidOperationException>(() => brent.FindRoot(new RootFindingInput(3.0, 5.0, 1e-6, 100), func));

        Result<(double Root, double Error)> sameSignResult = brent.TryFindRoot(new RootFindingInput(3.0, 5.0, 1e-6, 100), func);
        Assert.False(sameSignResult.Success);

        Assert.Throws<NonConvergenceException>(() => brent.FindRoot(new RootFindingInput(0.0, 3.0, 1e-12, 2), func));

        Result<(double Root, double Error)> nonConvResult = brent.TryFindRoot(new RootFindingInput(0.0, 3.0, 1e-12, 2), func);
        Assert.False(nonConvResult.Success);
    }

    [Fact]
    public void RootFinding_TryFindRootSuccessAndNonConvergenceHelper_BehaveCorrectly()
    {
        TestRootFinder finder = new();
        Result<(double Root, double Error)> result = finder.TryFindRoot(new RootFindingInput(0, 1, 1e-6, 10), x => x);

        Assert.True(result.Success);
        Assert.Equal((42.0, 0.0), result.Data);

        Exception ex = finder.BuildNonConvergence(1.25, 0.5);
        NonConvergenceException nonConv = Assert.IsType<NonConvergenceException>(ex);
        Assert.Contains("TestRootFinder", nonConv.Message);
    }

    [Fact]
    public void NumericalMethodOutput_ConstructorsAndToString_FormatInvariantCsv()
    {
        NumericalMethodOutput empty = new();
        Assert.Empty(empty.Displacement);

        NumericalMethodOutput output = new(2)
        {
            Time = 0.5,
            Displacement = [1.25, 2.5],
            Velocity = [0.1, 0.2],
            Acceleration = [9.8, 4.9],
            EquivalentForce = [10.0, 20.0]
        };

        Assert.Equal("0.5,1.25,2.5,0.1,0.2,9.8,4.9,10,20", output.ToString());
    }
}
