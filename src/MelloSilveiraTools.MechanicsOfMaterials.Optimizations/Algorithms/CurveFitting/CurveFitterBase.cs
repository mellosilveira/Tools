using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Mappers;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Models.CurveFitting;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Algorithms.CurveFitting;

public abstract class CurveFitterBase<TConstitutiveParameters>(IOptimizationMapper mapper) : ICurveFitter<TConstitutiveParameters>
    where TConstitutiveParameters : ConstitutiveParameters
{
    protected IOptimizationMapper Mapper { get; } = mapper;

    protected double CalculateObjectiveFunction(CurveFitInput<TConstitutiveParameters> input, double[] currentParameters, bool applyConstraints)
    {
        MechanicalModelInput<TConstitutiveParameters> mechanicalModelInput = input.InitialMechanicalModelInput with { ConstitutiveParameters = Mapper.MapToConstitutiveParameters<TConstitutiveParameters>(currentParameters) };

        double sumOfSquares = 0;
        for (int i = 0; i < input.TimePoints.Length; i++)
        {
            double predictedStress = input.CalculateStress(mechanicalModelInput, input.TimePoints[i], input.StrainPoints[i]);
            double diff = input.StressPoints[i] - predictedStress;
            sumOfSquares += diff * diff;
        }

        sumOfSquares += applyConstraints && input.EvaluateConstraintsAndPenalties != null ? input.EvaluateConstraintsAndPenalties(mechanicalModelInput) : 0;
        return sumOfSquares;
    }

    protected double[] CalculateNumericalGradient(CurveFitInput<TConstitutiveParameters> input, double[] currentParameters, bool applyConstraints)
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

    public abstract Result<CurveFitResultData<TConstitutiveParameters>> Fit(CurveFitInput<TConstitutiveParameters> input);
}