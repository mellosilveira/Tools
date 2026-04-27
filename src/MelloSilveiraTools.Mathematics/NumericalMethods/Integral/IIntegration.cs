using MelloSilveiraTools.Mathematics.Models.NumericalMethods;

namespace MelloSilveiraTools.Mathematics.NumericalMethods.Integral
{
    /// <summary>
    /// Represents a numerical integration rule.
    /// </summary>
    public interface IIntegration
    {
        /// <summary>
        /// Gets the multiply factor for integration steps.
        /// </summary>
        /// <param name="index"></param>
        /// <param name="numberOfDivisions"></param>
        /// <returns></returns>
        int GetMultiplyFactor(int index, int numberOfDivisions);

        /// <summary>
        /// Calculates the integral of a function.
        /// </summary>
        /// <param name="equation">The equation to be integrated.</param>
        /// <param name="integralInput">The inputs for integral.</param>
        /// <returns>The value of integration.</returns>
        double Calculate(Func<double, double> equation, IntegralInput integralInput);

        /// <summary>
        /// Calculates the integral of a function asynchronously.
        /// </summary>
        /// <param name="equation">The equation to be integrated.</param>
        /// <param name="integralInput">The inputs for integral.</param>
        /// <returns>The value of integration.</returns>
        Task<double> CalculateAsync(Func<double, Task<double>> equation, IntegralInput integralInput);
    }
}
