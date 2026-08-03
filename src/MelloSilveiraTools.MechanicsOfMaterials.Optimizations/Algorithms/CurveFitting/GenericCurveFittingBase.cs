using MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Mappers;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Models.CurveFitting;
using System;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Algorithms.CurveFitting;

public abstract class GenericCurveFittingBase(
    IMechanicalModelCalculatorFacade calculatorFacade,
    IOptimizationMapper mapper)
    : ICurveFitter
{
    protected IMechanicalModelCalculatorFacade CalculatorFacade { get; } = calculatorFacade;
    protected IOptimizationMapper Mapper { get; } = mapper;

    // 1. Método para calcular o erro total (todos os segmentos)
    protected double CalculateObjectiveFunction(CurveFitInput input, double[] currentParameters, bool applyConstraints)
    {
        GenericMechanicalModelInput currentInput = input.InitialInput with { ConstitutiveParameters = Mapper.MapToConstitutiveParameters(currentParameters) };

        double sumOfSquares = 0;

        for (int i = 0; i < input.Segments.Length; i++)
        {
            sumOfSquares += CalculateSegmentError(currentInput, input.Segments[i]);
        }

        sumOfSquares += EvaluateConstraintsAndPenalties(currentInput, input.EvaluateConstraintsAndPenalties, applyConstraints);
        return sumOfSquares;
    }

    // 2. Método para calcular o erro de um segmento em específico
    protected double CalculateObjectiveFunction(CurveFitInput input, double[] currentParameters, CurveSegment segment, bool applyConstraints)
    {
        GenericMechanicalModelInput currentInput = input.InitialInput with { ConstitutiveParameters = Mapper.MapToConstitutiveParameters(currentParameters) };
        return CalculateSegmentError(currentInput, segment)
            + EvaluateConstraintsAndPenalties(currentInput, input.EvaluateConstraintsAndPenalties, applyConstraints);
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

    private double CalculateSegmentError(GenericMechanicalModelInput currentInput, CurveSegment segment)
    {
        double sumOfSquares = 0;

        for (int j = 0; j < segment.TimePoints.Length; j++)
        {
            double predictedStress = CalculatorFacade.CalculateStress(currentInput, segment.TimePoints[j], segment.ExperimentalStrain[j]);
            double diff = segment.ExperimentalStress[j] - predictedStress;
            sumOfSquares += diff * diff;
        }

        return sumOfSquares;
    }

    private static double EvaluateConstraintsAndPenalties(GenericMechanicalModelInput currentInput, Func<GenericMechanicalModelInput, double>? evaluateConstraintsAndPenalties, bool applyConstraints)
        => applyConstraints && evaluateConstraintsAndPenalties != null ? evaluateConstraintsAndPenalties(currentInput) : 0;

    public abstract CurveFitResult Fit(CurveFitInput input);
}