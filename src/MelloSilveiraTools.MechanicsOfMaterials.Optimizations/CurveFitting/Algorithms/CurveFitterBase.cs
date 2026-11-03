using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Models;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Algorithms;

public abstract class CurveFitterBase : ICurveFitter
{
    protected double CalculateObjectiveFunction(CurveFitInput input, double[] currentParameters)
    {
        double sumOfSquaresError = 0;
        for (int i = 0; i < input.IndependentVariables[0].Length; i++)
        {
            double[] x = [.. input.IndependentVariables.Select(point => point[i])];
            double predictedValue = input.Calculate(currentParameters, x);
            double residual = input.DependentVariable[i] - predictedValue;
            sumOfSquaresError += residual * residual;
        }

        return sumOfSquaresError;
    }

    /// <summary>
    /// Computes the numerical gradient of the objective function (Sum of Squared Residuals)
    /// using central finite differences with an scale-adaptive step size.
    /// </summary>
    /// <param name="input">The curve fitting problem input definition.</param>
    /// <param name="currentParameters">The parameter vector at which the gradient is evaluated.</param>
    /// <returns>The gradient vector containing partial derivatives with respect to each parameter.</returns>
    /// <remarks>
    /// The finite difference step size <c>h</c> is chosen adaptively as <c>Math.Max(1e-6, 1e-4 * |p_i|)</c>:
    /// <list type="bullet">
    /// <item>
    /// <description>
    /// <b>For small parameters (<c>|p_i| &lt;= 0.01</c>):</b> The absolute floor <c>1e-6</c> prevents underflow,
    /// division by zero, and numerical instability near the origin.
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// <b>For large parameters (<c>|p_i| &gt;&gt; 1</c>, e.g. elastic modulus E ~ 1e4 or slow relaxation time tau_2 ~ 1e3 s):</b>
    /// A fixed step like <c>1e-6</c> represents an infinitesimal relative perturbation (<c>1e-9</c> or smaller),
    /// which causes catastrophic cancellation in double-precision floating-point arithmetic and leads to zero gradients,
    /// causing gradient-based optimizers (such as L-BFGS) to terminate prematurely. The proportional component (<c>1e-4 * |p_i|</c>)
    /// maintains a 0.01% perturbation, ensuring robust and smooth gradient estimation across diverse parameter magnitudes.
    /// </description>
    /// </item>
    /// </list>
    /// </remarks>
    protected double[] CalculateNumericalGradient(CurveFitInput input, double[] currentParameters)
    {
        double[] gradient = new double[currentParameters.Length];
        double[] tempParams = (double[])currentParameters.Clone();

        for (int i = 0; i < currentParameters.Length; i++)
        {
            // Scale-adaptive perturbation: balances truncation error O(h^2) against floating-point cancellation
            double h = Math.Max(1e-6, 1e-4 * Math.Abs(currentParameters[i]));
            tempParams[i] += h;
            double forwardCost = CalculateObjectiveFunction(input, tempParams);

            tempParams[i] -= 2 * h;
            double backwardCost = CalculateObjectiveFunction(input, tempParams);

            gradient[i] = (forwardCost - backwardCost) / (2 * h);

            tempParams[i] += h;
        }

        return gradient;
    }

    protected double CalculateRSquared(CurveFitInput input, double[] currentParameters)
    {
        double sumOfSquaresResiduals = 0;
        double sumOfSquaresTotal = 0;
        double meanDependentVariable = input.DependentVariable.Average();

        for (int i = 0; i < input.IndependentVariables[0].Length; i++)
        {
            double[] x = [.. input.IndependentVariables.Select(point => point[i])];
            double predictedValue = input.Calculate(currentParameters, x);
            double actualValue = input.DependentVariable[i];

            double residual = actualValue - predictedValue;
            sumOfSquaresResiduals += residual * residual;

            double deviation = actualValue - meanDependentVariable;
            sumOfSquaresTotal += deviation * deviation;
        }

        return sumOfSquaresTotal == 0 ? 1.0 : 1.0 - (sumOfSquaresResiduals / sumOfSquaresTotal);
    }

    public abstract CurveFitOutput Fit(CurveFitInput input);
}
