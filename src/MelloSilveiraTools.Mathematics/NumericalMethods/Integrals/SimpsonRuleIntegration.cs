using MelloSilveiraTools.Mathematics.Models.NumericalMethods;

namespace MelloSilveiraTools.Mathematics.NumericalMethods.Integrals;

/// <summary>
/// Numerical integration using Simpson's quadrature rules:
/// composite 1/3 rule when divisions are even, composite 3/8 rule when divisible by 3,
/// and 1/3 rule with halved step (doubled divisions) as fallback.
/// </summary>
public class SimpsonRuleIntegration : IIntegration
{
    /// <inheritdoc/>
    public double Calculate(Func<double, double> equation, IntegralInput integralInput)
    {
        int divisions = Convert.ToInt32((integralInput.FinalPoint - integralInput.InitialPoint) / integralInput.Step);

        (double step, int numberOfDivisions, Func<int, int, int> integralFactorFunc, double simpsonRuleFactor) = divisions % 2 == 0
            ? (integralInput.Step, divisions, (Func<int, int, int>)GetOneThirdFactor, 1.0 / 3.0)
            : divisions % 3 == 0
                ? (integralInput.Step, divisions, GetThreeEighthsFactor, 3.0 / 8.0)
                : (integralInput.Step / 2.0, divisions * 2, GetOneThirdFactor, 1.0 / 3.0);

        double sum = 0;
        for (int i = 0; i <= numberOfDivisions; i++)
        {
            double equationValue = equation(integralInput.InitialPoint + i * step);
            if (equationValue == 0)
                continue;

            sum += integralFactorFunc(i, numberOfDivisions) * equationValue;
        }

        return sum * step * simpsonRuleFactor;
    }

    public static int GetOneThirdFactor(int index, int numberOfDivisions) => index == 0 || index == numberOfDivisions ? 1 : index % 2 != 0 ? 4 : 2;

    private static int GetThreeEighthsFactor(int index, int numberOfDivisions) => index == 0 || index == numberOfDivisions ? 1 : index % 3 == 0 ? 2 : 3;
}
