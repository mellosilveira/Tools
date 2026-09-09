using MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Mappers;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Models.CurveFitting;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Algorithms.CurveFitting;

public abstract class CurveFitterBase(IOptimizationMapper mapper) : ICurveFitter
{
    protected IOptimizationMapper Mapper { get; } = mapper;

    protected double CalculateObjectiveFunction(CurveFitInput input, double[] currentParameters, bool applyConstraints)
    {
        GenericMechanicalModelInput mechanicalModelInput = input.InitialMechanicalModelInput with { ConstitutiveParameters = Mapper.MapToConstitutiveParameters(currentParameters) };

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

    public abstract CurveFitResult Fit(CurveFitInput input);
}