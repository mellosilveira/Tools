using MelloSilveiraTools.Mathematics.NumericalMethods;
using MelloSilveiraTools.Mathematics.NumericalMethods.DifferentialEquation;

namespace UnitTests.Mathematics.NumericalMethods.DifferentialEquation;

/// <summary>
/// Unit tests for <see cref="NewmarkMethod"/> and <see cref="NewmarkBetaMethod"/>.
/// </summary>
public sealed class NewmarkMethodsTests
{
    private static NumericalMethodInput CreateSingleDofInput() => new()
    {
        TimeStep = 0.01,
        Mass = new double[,] { { 2.0 } },
        Damping = new double[,] { { 0.4 } },
        Stiffness = new double[,] { { 100.0 } },
        EquivalentForce = [10.0],
        NumberOfBoundaryConditions = 1
    };

    private static NumericalMethodInput CreateTwoDofInput() => new()
    {
        TimeStep = 0.01,
        Mass = new double[,] { { 2.0, 0.0 }, { 0.0, 1.5 } },
        Damping = new double[,] { { 0.5, -0.1 }, { -0.1, 0.4 } },
        Stiffness = new double[,] { { 200.0, -50.0 }, { -50.0, 120.0 } },
        EquivalentForce = [10.0, 5.0],
        NumberOfBoundaryConditions = 2
    };

    [Fact]
    public void NewmarkMethod_ValidatesTimeAndComputesInitialAndTimeSteppedResponses()
    {
        NewmarkMethod method = new();
        NumericalMethodInput input = CreateTwoDofInput();
        NumericalMethodOutput initial = new(2);

        Assert.Throws<ArgumentOutOfRangeException>(() => method.Calculate(input, -0.01, initial));

        NumericalMethodOutput atZero = method.Calculate(input, 0.0, initial);
        Assert.Equal(0.0, atZero.Time);
        Assert.Equal(input.EquivalentForce, atZero.EquivalentForce);

        NumericalMethodOutput step1 = method.Calculate(input, 0.01, initial);
        NumericalMethodOutput step2 = method.Calculate(input, 0.02, step1);

        Assert.Equal(0.01, step1.Time);
        Assert.Equal(0.02, step2.Time);
        Assert.True(step1.Displacement[0] > 0);
        Assert.True(step2.Displacement[0] > step1.Displacement[0]);
        Assert.Equal(2, step2.Velocity.Length);
        Assert.Equal(2, step2.Acceleration.Length);
    }

    [Fact]
    public void NewmarkBetaMethod_ValidatesTimeAndComputesInitialAndTimeSteppedResponses()
    {
        NewmarkBetaMethod method = new();
        NumericalMethodInput input = CreateSingleDofInput();
        NumericalMethodOutput initial = new(1);

        Assert.Throws<ArgumentOutOfRangeException>(() => method.Calculate(input, -0.01, initial));

        NumericalMethodOutput atZero = method.Calculate(input, 0.0, initial);
        Assert.Equal(0.0, atZero.Time);
        Assert.Equal(input.EquivalentForce, atZero.EquivalentForce);

        NumericalMethodOutput step1 = method.Calculate(input, 0.01, initial);
        NumericalMethodOutput step2 = method.Calculate(input, 0.02, step1);

        Assert.Equal(0.01, step1.Time);
        Assert.Equal(0.02, step2.Time);
        Assert.True(step1.Displacement[0] > 0);
        Assert.True(step2.Displacement[0] > step1.Displacement[0]);
        Assert.NotEqual(0.0, step2.Velocity[0]);
        Assert.Single(step2.Acceleration);
    }
}
