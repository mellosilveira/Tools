using MathNet.Numerics.LinearAlgebra.Double;
using MathNet.Numerics.Optimization;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Models;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Algorithms;

public class MathNetCurveFitter : CurveFitterBase
{
    public override CurveFitOutput Fit(CurveFitInput input)
    {
        var objectiveFunction = ObjectiveFunction.Gradient(
            (vector) => CalculateObjectiveFunction(input, vector.ToArray()),
            (vector) => DenseVector.OfArray(CalculateNumericalGradient(input, vector.ToArray()))
        );

        var initialGuess = DenseVector.OfArray(input.InitialParameters);

        // Estágio 1: BFGS (sem restrições) para encontrar o mínimo aproximado
        var solverBfgs = new BfgsMinimizer(input.Tolerance, input.Tolerance, input.Tolerance, input.MaxIterations);
        var resultBfgs = solverBfgs.FindMinimum(objectiveFunction, initialGuess);

        // Estágio 2: Nelder-Mead a partir do resultado do BFGS
        var unconstrainedObjectiveFunction = ObjectiveFunction.Value(
            (vector) => CalculateObjectiveFunction(input, vector.ToArray())
        );
        var solverNelderMead = new NelderMeadSimplex(input.Tolerance, input.MaxIterations);
        var finalResult = solverNelderMead.FindMinimum(unconstrainedObjectiveFunction, resultBfgs.MinimizingPoint);

        double[] finalParameters = finalResult.MinimizingPoint.ToArray();
        double rSquared = CalculateRSquared(input, finalParameters);

        return new CurveFitOutput(finalParameters, finalResult.FunctionInfoAtMinimum.Value, rSquared, finalResult.Iterations);
    }
}
