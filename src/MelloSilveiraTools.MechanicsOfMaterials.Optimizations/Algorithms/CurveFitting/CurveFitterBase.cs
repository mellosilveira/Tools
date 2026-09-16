using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Models.CurveFitting;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Algorithms.CurveFitting;

public abstract class CurveFitterBase : ICurveFitter
{
    protected double CalculateObjectiveFunction(CurveFitInput input, double[] currentParameters, bool applyConstraints)
    {
        double sumOfSquares = 0;
        for (int i = 0; i < input.XPoints[0].Length; i++)
        {
            double[] x = [.. input.XPoints.Select(point => point[i])];
            double predictedStress = input.Calculate(currentParameters, x);
            double diff = input.YPoints[i] - predictedStress;
            sumOfSquares += diff * diff;
        }

        sumOfSquares += applyConstraints && input.EvaluateConstraintsAndPenalties != null ? input.EvaluateConstraintsAndPenalties(currentParameters) : 0;
        return sumOfSquares;
    }

    protected double[] CalculateNumericalGradient(CurveFitInput input, double[] currentParameters, bool applyConstraints)
    {
        var gradient = new double[currentParameters.Length];
        double h = 1e-6;

        var tempParams = (double[])currentParameters.Clone();

        for (int i = 0; i < currentParameters.Length; i++)
        {
            tempParams[i] += h;
            double forwardCost = CalculateObjectiveFunction(input, tempParams, applyConstraints);

            tempParams[i] -= 2 * h;
            double backwardCost = CalculateObjectiveFunction(input, tempParams, applyConstraints);

            gradient[i] = (forwardCost - backwardCost) / (2 * h);

            tempParams[i] += h;
        }

        return gradient;
    }

    public abstract CurveFitOutput Fit(CurveFitInput input);
}