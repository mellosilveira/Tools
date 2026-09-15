using MathNet.Numerics.Optimization;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Mappers;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Models.CurveFitting;
using Microsoft.Extensions.Logging;
using MathNetNumerics = MathNet.Numerics.LinearAlgebra;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Algorithms.CurveFitting;

public class MathNetCurveFitter<TConstitutiveParameters>(ILogger<MathNetCurveFitter<TConstitutiveParameters>> logger, IOptimizationMapper mapper) : CurveFitterBase<TConstitutiveParameters>(mapper)
    where TConstitutiveParameters : ConstitutiveParameters
{
    public override Result<CurveFitResultData<TConstitutiveParameters>> Fit(CurveFitInput<TConstitutiveParameters> input)
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

            var initialGuesses = Mapper.ExtractOptimizableParameters(input.InitialMechanicalModelInput.ConstitutiveParameters);
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

            var constitutiveParameters = Mapper.MapToConstitutiveParameters<TConstitutiveParameters>([.. resultStage2.MinimizingPoint]);
            return Result.CreateSuccessOk(new CurveFitResultData<TConstitutiveParameters>(constitutiveParameters, resultStage2.FunctionInfoAtMinimum.Value, resultStage2.Iteration));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fit curve using MathNet. Input: {@Input}", input);
            return Result.CreateUnknownError(ex.Message);
        }
    }
}
