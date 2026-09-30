using MathNet.Numerics.LinearAlgebra.Double;
using MathNet.Numerics.Optimization;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Models;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Algorithms;

public class MathNetCurveFitter : CurveFitterBase
{
    public override CurveFitOutput Fit(CurveFitInput input)
    {
        IObjectiveFunction objectiveFunction = ObjectiveFunction.Gradient(
            (vector) => CalculateObjectiveFunction(input, vector.ToArray()),
            (vector) => DenseVector.OfArray(CalculateNumericalGradient(input, vector.ToArray()))
        );

        DenseVector initialGuess = DenseVector.OfArray(input.InitialParameters);

        // Estagio 1: BFGS (sem restricoes) para encontrar o minimo aproximado
        BfgsMinimizer solverBfgs = new(input.Tolerance, input.Tolerance, input.Tolerance, input.MaxIterations);
        MinimizationResult resultBfgs = solverBfgs.FindMinimum(objectiveFunction, initialGuess);

        // Estagio 2: Nelder-Mead a partir do resultado do BFGS
        IObjectiveFunction unconstrainedObjectiveFunction = ObjectiveFunction.Value(
            (vector) => CalculateObjectiveFunction(input, vector.ToArray())
        );
        NelderMeadSimplex solverNelderMead = new(input.Tolerance, input.MaxIterations);
        MinimizationResult finalResult = solverNelderMead.FindMinimum(unconstrainedObjectiveFunction, resultBfgs.MinimizingPoint);

        double[] finalParameters = finalResult.MinimizingPoint.ToArray();
        double rSquared = CalculateRSquared(input, finalParameters);

        return new CurveFitOutput(finalParameters, finalResult.FunctionInfoAtMinimum.Value, rSquared, finalResult.Iterations);
    }
}
