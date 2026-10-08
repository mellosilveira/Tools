using MelloSilveiraTools.Mathematics.NumericalMethods.Differentiations;

namespace UnitTests.Mathematics.NumericalMethods.Differentiations;

public class DifferentiationTests
{
    [Fact]
    public void Calculate_WithEquation_ShouldReturnAccurateDerivative()
    {
        Differentiation diff = new();
        // f(t) = t^2 -> f'(t) = 2t. At t = 3, derivative is 6.
        Func<double, double> parabola = t => t * t;
        double time = 3.0;
        double timeStep = 0.0001;

        double result = diff.Calculate(parabola, timeStep, time);

        Assert.Equal(6.0, result, precision: 3);
    }

    [Fact]
    public void Calculate_WithTwoPoints_ShouldReturnExpectedSlope()
    {
        Differentiation diff = new();
        double initialPoint = 10.0;
        double finalPoint = 25.0;
        double step = 3.0;

        double slope = diff.Calculate(initialPoint, finalPoint, step);

        Assert.Equal(5.0, slope, precision: 5);
    }
}
