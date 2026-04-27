using MelloSilveiraTools.Mathematics.Models.NumericalMethods;

namespace MelloSilveiraTools.Mathematics.NumericalMethods.Integral
{
    /// <inheritdoc/>
    public class SimpsonRuleIntegration : IIntegration
    {
        /// <inheritdoc/>
        public int GetMultiplyFactor(int index, int numberOfDivisions)
        {
            if (index == 0 || index == numberOfDivisions)
                return 1;

            return index % 2 != 0 ? 4 : 2;
        }
        
        /// <inheritdoc/>
        public double Calculate(Func<double, double> equation, IntegralInput integralInput)
        {
            int numberOfDivisions = Convert.ToInt32((integralInput.FinalPoint - integralInput.InitialPoint) / integralInput.Step);
            double sum = 0;

            for (int i = 0; i <= numberOfDivisions; i++)
            {
                double equationResult = equation(integralInput.InitialPoint + i * integralInput.Step);
                if (equationResult == 0)
                    continue;

                sum += GetMultiplyFactor(i, numberOfDivisions) * equationResult;
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
                double equationResult = await equation(integralInput.InitialPoint + i * integralInput.Step);
                if (equationResult == 0)
                    continue;

                sum += GetMultiplyFactor(i, numberOfDivisions) * equationResult;
            }

            return sum * integralInput.Step / 3;
        }
    }
}
