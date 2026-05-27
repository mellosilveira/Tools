using MelloSilveiraTools.Mathematics.Models.NumericalMethods;

namespace MelloSilveiraTools.Mathematics.NumericalMethods.Integral;

/// <inheritdoc/>
public class SimpsonRuleIntegration : IIntegration
{
    /// <inheritdoc/>
    public double Calculate(Func<double, double> equation, IntegralInput integralInput)
    {
        int numberOfDivisions = Convert.ToInt32((integralInput.FinalPoint - integralInput.InitialPoint) / integralInput.Step);
        double sum = 0;

        for (int i = 0; i <= numberOfDivisions; i++)
        {
            double equationValue = equation(integralInput.InitialPoint + i * integralInput.Step);
            if (equationValue == 0)
                continue;

            sum += GetFactor(i, numberOfDivisions) * equationValue;
        }

        return sum * integralInput.Step / 3;
    }

    /// <inheritdoc/>
    public async Task<double> CalculateAsync(Func<double, Task<double>> equation, IntegralInput integralInput)
    {
        int numberOfDivisions = Convert.ToInt32((integralInput.FinalPoint - integralInput.InitialPoint) / integralInput.Step);
        double sum = 0;

        for (int i = 0; i <= numberOfDivisions; i++)
        {
            double equationValue = await equation(integralInput.InitialPoint + i * integralInput.Step);
            if (equationValue == 0)
                continue;

            sum += GetFactor(i, numberOfDivisions) * equationValue;
        }

        return sum * integralInput.Step / 3;
    }

    // Simpson 1/3 rule weights: 1 at endpoints, alternating 4 and 2 at interior nodes.
    public static int GetFactor(int index, int numberOfDivisions)
    {
        if (index == 0 || index == numberOfDivisions)
            return 1;

        return index % 2 != 0 ? 4 : 2;
    }
}
