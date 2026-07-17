using MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using SoftTissue.Domain.Models.Optimization;
using SoftTissue.Domain.Optimizations.CurveFitting.Mappers;

namespace SoftTissue.Domain.Optimizations.CurveFitting;

public abstract class GenericCurveFittingBase(
    IMechanicalModelCalculatorFacade calculatorFacade,
    IModelParameterMapper mapper)
    : ICurveFitter
{
    protected IMechanicalModelCalculatorFacade CalculatorFacade { get; } = calculatorFacade;
    protected IModelParameterMapper Mapper { get; } = mapper;

    protected double CalculateObjectiveFunction(CurveFitInput input, double[] currentParameters, bool applyConstraints)
    {
        // 1. Converte o array da iteração atual para o seu Record imutável
        GenericMechanicalModelInput currentInput = Mapper.MapToInput(currentParameters);

        double sumOfSquares = 0;

        // 2. Chama a sua biblioteca de cálculo existente de forma transparente
        for (int i = 0; i < input.TimePoints.Length; i++)
        {
            double predictedStress = CalculatorFacade.CalculateStress(currentInput, input.TimePoints[i], input.Strain);
            sumOfSquares += Math.Pow(input.ExperimentalStress[i] - predictedStress, 2);
        }

        // 3. Aplica limites matemáticos condicionais (Estágio 2)
        if (applyConstraints && input.EvaluateConstraintsAndPenalties != null)
        {
            sumOfSquares += input.EvaluateConstraintsAndPenalties(input.InitialInput, currentParameters);
        }

        return sumOfSquares;
    }

    /// <summary>
    /// Calcula o gradiente da função custo numericamente via Diferença Central.
    /// Necessário para algoritmos baseados em Quase-Newton como o BFGS.
    /// </summary>
    protected double[] CalculateNumericalGradient(CurveFitInput input, double[] currentParameters, bool applyConstraints)
    {
        var gradient = new double[currentParameters.Length];
        double h = 1e-6; // Passo diferencial pequeno para aproximação da derivada

        for (int i = 0; i < currentParameters.Length; i++)
        {
            var forwardParams = (double[])currentParameters.Clone();
            forwardParams[i] += h;
            double forwardCost = CalculateObjectiveFunction(input, forwardParams, applyConstraints);

            var backwardParams = (double[])currentParameters.Clone();
            backwardParams[i] -= h;
            double backwardCost = CalculateObjectiveFunction(input, backwardParams, applyConstraints);

            // Fórmula da diferença central para maior precisão: (f(x+h) - f(x-h)) / 2h
            gradient[i] = (forwardCost - backwardCost) / (2 * h);
        }

        return gradient;
    }

    public abstract CurveFitResult Fit(CurveFitInput input);
}
