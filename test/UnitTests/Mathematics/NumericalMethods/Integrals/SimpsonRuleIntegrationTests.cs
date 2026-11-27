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
    public void Calculate_WithOddNumberOfDivisions_CombinesSimpsonAndTrapezoidalRule()
    {
        SimpsonRuleIntegration integration = new();
        // Linear integral of 2x on [0, 0.03] with step 0.01 (N = 3, odd) equals 0.03^2 = 0.0009.
        double result = integration.Calculate(x => 2.0 * x, new IntegralInput(0.0, 0.03, 0.01));

        Assert.Equal(0.0009, result, precision: 10);
    }
}
