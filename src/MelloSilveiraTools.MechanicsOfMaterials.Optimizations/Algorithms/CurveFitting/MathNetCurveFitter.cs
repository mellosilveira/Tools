using MathNet.Numerics.Optimization;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Models.CurveFitting;
using MathNetNumerics = MathNet.Numerics.LinearAlgebra;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Algorithms.CurveFitting;

public class MathNetCurveFitter : CurveFitterBase
{
    public override CurveFitOutput Fit(CurveFitInput input)
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
            input.Tolerance,
            input.Tolerance,
            input.Tolerance,
            input.MaxIterations);

        var initialVector = MathNetNumerics.Vector<double>.Build.Dense(input.InitialParameters);
        var resultStage1 = solverStage1.FindMinimum(objStage1, initialVector);

        // ========================================================================
        // ESTÁGIO 2: Refinamento Fino (Constrained) via Nelder-Mead Simplex
        // Objetivo: Partindo do ponto encontrado, forçar a aderência às leis da física
        // ========================================================================
        var initialVectorStage2 = resultStage1.MinimizingPoint; // Warm-start

        var objStage2 = ObjectiveFunction.Value(v => CalculateObjectiveFunction(input, [.. v], applyConstraints: true));
        var solverStage2 = new NelderMeadSimplex(input.Tolerance, input.MaxIterations);
        var resultStage2 = solverStage2.FindMinimum(objStage2, initialVectorStage2);

        return new CurveFitOutput([.. resultStage2.MinimizingPoint], resultStage2.FunctionInfoAtMinimum.Value, resultStage2.Iterations);
    }
}
