using MathNet.Numerics.Optimization;
using MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels;
using Microsoft.Extensions.Logging;
using SoftTissue.Domain.Models.Optimization;
using SoftTissue.Domain.Optimizations.CurveFitting.Mappers;
using MathNetNumerics = MathNet.Numerics.LinearAlgebra;

namespace SoftTissue.Domain.Optimizations.CurveFitting;

public class MathNetCurveFitter(
    ILogger<MathNetCurveFitter> logger,
    IMechanicalModelCalculatorFacade calculatorFacade,
    IModelParameterMapper mapper)
    : GenericCurveFittingBase(calculatorFacade, mapper)
{
    public override CurveFitResult Fit(CurveFitInput input)
    {
        try
        {
            // ========================================================================
            // ESTÁGIO 1: Busca Global/Rápida (Unconstrained) via BFGS
            // Objetivo: Achar o formato geral da curva sem esbarrar nas penalidades
            // ========================================================================
            var objStage1 = ObjectiveFunction.Gradient(
                v => CalculateObjectiveFunction(input, [.. v], applyConstraints: false),
                v => MathNetNumerics.Vector<double>.Build.Dense(CalculateNumericalGradient(input, [.. v], applyConstraints: false))
            );

            // BFGS é altamente eficiente para Least Squares quando as derivadas estão disponíveis
            var solverStage1 = new BfgsMinimizer(
                input.Options.Tolerance,
                input.Options.Tolerance,
                input.Options.Tolerance,
                input.Options.MaxIterations);

            var initialGuesses = Mapper.ExtractOptimizableParameters(input.InitialInput);
            var initialVector = MathNetNumerics.Vector<double>.Build.Dense(initialGuesses);
            var resultStage1 = solverStage1.FindMinimum(objStage1, initialVector);

            // ========================================================================
            // ESTÁGIO 2: Refinamento Fino (Constrained) via Nelder-Mead Simplex
            // Objetivo: Partindo do ponto encontrado, forçar a aderência às leis da física
            // ========================================================================
            var initialVectorStage2 = resultStage1.MinimizingPoint; // Warm-start

            var objStage2 = ObjectiveFunction.Value(v => CalculateObjectiveFunction(input, [.. v], applyConstraints: true));
            var solverStage2 = new NelderMeadSimplex(input.Options.Tolerance, input.Options.MaxIterations);
            var resultStage2 = solverStage2.FindMinimum(objStage2, initialVectorStage2);

            return new CurveFitResult(
                true,
                [.. resultStage2.MinimizingPoint],
                resultStage2.FunctionInfoAtMinimum.Value,
                $"Sucesso. Estágio 1 (BFGS): {resultStage1.Iterations} iter. Estágio 2 (Simplex): {resultStage2.Iterations} iter."
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fit curve using MathNet. Input: {@Input}", input);
            return new CurveFitResult(false, [], double.MaxValue, ex.Message);
        }
    }
}
