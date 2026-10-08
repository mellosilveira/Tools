using MelloSilveiraTools.Mathematics.Models.NumericalMethods;
using MelloSilveiraTools.Mathematics.NumericalMethods.Integrals;

namespace UnitTests.Mathematics.NumericalMethods.Integrals;

public class SimpsonRuleIntegrationTests
{
    [Fact]
    public void Calculate_QuadraticFunction_ShouldYieldExactIntegral()
    {
        SimpsonRuleIntegration integration = new();
        // Integral of x^2 from 0 to 3 is x^3/3 = 9.0
        Func<double, double> func = x => x * x;
        IntegralInput input = new(InitialPoint: 0.0, FinalPoint: 3.0, Step: 0.1);

        double result = integration.Calculate(func, input);

        Assert.Equal(9.0, result, precision: 3);
    }

    [Fact]
    public async Task CalculateAsync_LinearFunction_ShouldYieldExactIntegral()
    {
        SimpsonRuleIntegration integration = new();
        // Integral of 2x from 0 to 4 is x^2 = 16.0
        Func<double, Task<double>> func = x => Task.FromResult(2.0 * x);
        IntegralInput input = new(InitialPoint: 0.0, FinalPoint: 4.0, Step: 0.2);

        double result = await integration.CalculateAsync(func, input);

        Assert.Equal(16.0, result, precision: 3);
    }

    [Theory]
    [InlineData(0, 10, 1)]
    [InlineData(10, 10, 1)]
    [InlineData(1, 10, 4)]
    [InlineData(2, 10, 2)]
    public void GetFactor_ShouldFollowSimpsonWeights(int index, int numberOfDivisions, int expectedWeight)
    {
        int weight = SimpsonRuleIntegration.GetFactor(index, numberOfDivisions);

        Assert.Equal(expectedWeight, weight);
    }
}
