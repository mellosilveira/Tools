using MathNet.Numerics.Distributions;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using MathNet.Numerics.Optimization;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Models;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Algorithms;

/// <summary>
/// Numerical curve fitting solver utilizing the MathNet.Numerics library.
/// Executes two-stage optimization (unconstrained BFGS followed by Nelder-Mead simplex local search)
/// and calculates parameter confidence intervals via MathNet linear algebra and Student-t distribution routines.
/// </summary>
public class MathNetCurveFitter : CurveFitterBase
{
    /// <inheritdoc />
    public override CurveFitOutput Fit(CurveFitInput input)
    {
        IObjectiveFunction objectiveFunction = ObjectiveFunction.Gradient(
            (vector) => CalculateObjectiveFunction(input, [.. vector]),
            (vector) => DenseVector.OfArray(CalculateNumericalGradient(input, [.. vector]))
        );

        DenseVector initialGuess = DenseVector.OfArray(input.InitialParameters);

        // Stage 1: Unconstrained BFGS to find approximate minimum
        BfgsMinimizer solverBfgs = new(input.Tolerance, input.Tolerance, input.Tolerance, input.MaxIterations);
        MinimizationResult resultBfgs = solverBfgs.FindMinimum(objectiveFunction, initialGuess);

        // Stage 2: Nelder-Mead simplex search starting from the BFGS minimizing point
        IObjectiveFunction unconstrainedObjectiveFunction = ObjectiveFunction.Value(
            (vector) => CalculateObjectiveFunction(input, [.. vector])
        );
        NelderMeadSimplex solverNelderMead = new(input.Tolerance, input.MaxIterations);
        MinimizationResult finalResult = solverNelderMead.FindMinimum(unconstrainedObjectiveFunction, resultBfgs.MinimizingPoint);

        double[] finalParameters = [.. finalResult.MinimizingPoint];
        double rSquared = CalculateRSquared(input, finalParameters);
        ParameterConfidenceInterval[] parameters = CalculateConfidenceIntervals(input, finalParameters, finalResult.FunctionInfoAtMinimum.Value);

        return new CurveFitOutput(parameters, finalResult.FunctionInfoAtMinimum.Value, rSquared, finalResult.Iterations);
    }

    /// <summary>
    /// Computes statistical parameter confidence intervals using MathNet matrix operations and Student-t distribution routines.
    /// </summary>
    private ParameterConfidenceInterval[] CalculateConfidenceIntervals(
        CurveFitInput input,
        double[] optimizedParameters,
        double finalError)
    {
        int parameterCount = optimizedParameters.Length;
        int sampleCount = input.DependentVariable.Length;
        int degreesOfFreedom = sampleCount - parameterCount;
        double confidenceLevel = input.ConfidenceLevel;

        if (degreesOfFreedom <= 0 || sampleCount == 0)
        {
            ParameterConfidenceInterval[] fallback = new ParameterConfidenceInterval[parameterCount];
            for (int i = 0; i < parameterCount; i++)
            {
                fallback[i] = new ParameterConfidenceInterval(
                    i,
                    optimizedParameters[i],
                    optimizedParameters[i],
                    optimizedParameters[i],
                    0.0,
                    confidenceLevel);
            }

            return fallback;
        }

        double[,] jacobian = CalculateNumericalJacobian(input, optimizedParameters);
        Matrix<double> jMatrix = DenseMatrix.OfArray(jacobian);
        Matrix<double> jtj = jMatrix.TransposeThisAndMultiply(jMatrix);
        Matrix<double> jtjInv = jtj.PseudoInverse();

        double sSquared = Math.Max(0.0, finalError / degreesOfFreedom);
        double alpha = 1.0 - confidenceLevel;
        double pStudent = 1.0 - (alpha / 2.0);
        double tCrit = StudentT.InvCDF(0.0, 1.0, degreesOfFreedom, pStudent);

        ParameterConfidenceInterval[] intervals = new ParameterConfidenceInterval[parameterCount];
        for (int j = 0; j < parameterCount; j++)
        {
            double variance = Math.Max(0.0, jtjInv[j, j] * sSquared);
            double standardError = Math.Sqrt(variance);
            double marginOfError = tCrit * standardError;
            double paramVal = optimizedParameters[j];

            intervals[j] = new ParameterConfidenceInterval(
                j,
                paramVal,
                paramVal - marginOfError,
                paramVal + marginOfError,
                standardError,
                confidenceLevel);
        }

        return intervals;
    }
}
