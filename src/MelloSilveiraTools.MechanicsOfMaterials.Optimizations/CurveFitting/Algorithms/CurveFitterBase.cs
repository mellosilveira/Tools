using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Models;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Algorithms;

/// <summary>
/// Abstract base class for numerical curve fitting algorithms.
/// Provides core numerical methods for objective function evaluation, numerical gradient estimation,
/// Jacobian matrix computation, and coefficient of determination (R²).
/// </summary>
public abstract class CurveFitterBase : ICurveFitter
{
    /// <summary>
    /// Computes the sum of squared residuals (SSR) of the model for a given parameter vector.
    /// </summary>
    /// <param name="input">The curve fitting problem input definition.</param>
    /// <param name="currentParameters">The parameter vector to evaluate.</param>
    /// <returns>The sum of squared residuals.</returns>
    protected double CalculateObjectiveFunction(CurveFitInput input, double[] currentParameters)
    {
        double sumOfSquaresError = 0.0;
        int sampleCount = input.IndependentVariables[0].Length;

        for (int i = 0; i < sampleCount; i++)
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
    /// using central finite differences with a scale-adaptive step size.
    /// </summary>
    /// <param name="input">The curve fitting problem input definition.</param>
    /// <param name="currentParameters">The parameter vector at which the gradient is evaluated.</param>
    /// <returns>The gradient vector containing partial derivatives with respect to each parameter.</returns>
    protected double[] CalculateNumericalGradient(CurveFitInput input, double[] currentParameters)
    {
        int parameterCount = currentParameters.Length;
        double[] gradient = new double[parameterCount];
        double[] tempParams = (double[])currentParameters.Clone();

        for (int i = 0; i < parameterCount; i++)
        {
            // Scale-adaptive perturbation: balances truncation error O(h^2) against floating-point cancellation
            double h = Math.Max(1e-6, 1e-4 * Math.Abs(currentParameters[i]));
            tempParams[i] += h;
            double forwardCost = CalculateObjectiveFunction(input, tempParams);

            tempParams[i] -= 2.0 * h;
            double backwardCost = CalculateObjectiveFunction(input, tempParams);

            gradient[i] = (forwardCost - backwardCost) / (2.0 * h);

            tempParams[i] += h;
        }

        return gradient;
    }

    /// <summary>
    /// Computes the numerical Jacobian matrix of the model function using central finite differences.
    /// Dimensions: N x P, where N is the number of data points and P is the number of parameters.
    /// </summary>
    /// <param name="input">The curve fitting problem input definition.</param>
    /// <param name="currentParameters">The parameter vector at which the Jacobian is evaluated.</param>
    /// <returns>A 2D array of size [N, P] representing partial derivatives of the model with respect to each parameter.</returns>
    protected double[,] CalculateNumericalJacobian(CurveFitInput input, double[] currentParameters)
    {
        int sampleCount = input.DependentVariable.Length;
        int parameterCount = currentParameters.Length;
        int varCount = input.IndependentVariables.Count;

        double[][] xValues = new double[sampleCount][];
        for (int i = 0; i < sampleCount; i++)
        {
            double[] x = new double[varCount];
            for (int v = 0; v < varCount; v++)
            {
                x[v] = input.IndependentVariables[v][i];
            }

            xValues[i] = x;
        }

        double[,] jacobian = new double[sampleCount, parameterCount];
        double[] tempParams = (double[])currentParameters.Clone();

        for (int j = 0; j < parameterCount; j++)
        {
            double originalParam = tempParams[j];
            double h = Math.Max(1e-6, 1e-4 * Math.Abs(originalParam));

            tempParams[j] = originalParam + h;
            double[] fForward = new double[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                fForward[i] = input.Calculate(tempParams, xValues[i]);
            }

            tempParams[j] = originalParam - h;
            double[] fBackward = new double[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                fBackward[i] = input.Calculate(tempParams, xValues[i]);
            }

            double twoH = 2.0 * h;
            for (int i = 0; i < sampleCount; i++)
            {
                jacobian[i, j] = (fForward[i] - fBackward[i]) / twoH;
            }

            tempParams[j] = originalParam;
        }

        return jacobian;
    }

    /// <summary>
    /// Calculates the coefficient of determination (R²) for the given parameter set.
    /// </summary>
    /// <param name="input">The curve fitting problem input definition.</param>
    /// <param name="currentParameters">The parameter vector to evaluate.</param>
    /// <returns>The calculated R² value.</returns>
    protected double CalculateRSquared(CurveFitInput input, double[] currentParameters)
    {
        double sumOfSquaresResiduals = 0.0;
        double sumOfSquaresTotal = 0.0;
        double meanDependentVariable = input.DependentVariable.Average();
        int sampleCount = input.IndependentVariables[0].Length;

        for (int i = 0; i < sampleCount; i++)
        {
            double[] x = [.. input.IndependentVariables.Select(point => point[i])];
            double predictedValue = input.Calculate(currentParameters, x);
            double actualValue = input.DependentVariable[i];

            double residual = actualValue - predictedValue;
            sumOfSquaresResiduals += residual * residual;

            double deviation = actualValue - meanDependentVariable;
            sumOfSquaresTotal += deviation * deviation;
        }

        return sumOfSquaresTotal == 0.0 ? 1.0 : 1.0 - (sumOfSquaresResiduals / sumOfSquaresTotal);
    }

    /// <inheritdoc />
    public abstract CurveFitOutput Fit(CurveFitInput input);
}
