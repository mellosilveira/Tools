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

    protected double[] CalculateNumericalGradient(CurveFitInput input, double[] currentParameters)
    {
        var gradient = new double[currentParameters.Length];
        double h = 1e-6;

        var tempParams = (double[])currentParameters.Clone();

        for (int i = 0; i < currentParameters.Length; i++)
        {
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

        if (sumOfSquaresTotal == 0) return 1.0;
        return 1.0 - (sumOfSquaresResiduals / sumOfSquaresTotal);
    }

    public abstract CurveFitOutput Fit(CurveFitInput input);
}
