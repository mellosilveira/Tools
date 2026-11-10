using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Models;
using Microsoft.Extensions.Logging;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Algorithms;

/// <summary>
/// Numerical curve fitting solver utilizing the ALGLIB mathematical library.
/// Supports bounded L-BFGS gradient-based optimization (<c>minbleic</c>) and derivative-free
/// adaptive Differential Evolution (<c>mindf</c> GDEMO) with deterministic pseudo-random seeding.
/// </summary>
/// <param name="logger">The logger instance for diagnostics and reporting.</param>
/// <param name="settings">Curve fitter configuration settings including deterministic seed.</param>
public class AlglibCurveFitter(ILogger<AlglibCurveFitter> logger, CurveFitterSettings settings) : CurveFitterBase
{
    /// <inheritdoc />
    public override CurveFitOutput Fit(CurveFitInput input) => (input.Profile, input.ValidateParameters) switch
    {
        (CurveFitProfile.Automatic, null) or (CurveFitProfile.Standard, _) => FitStandard(input),
        (CurveFitProfile.Automatic, _) or (CurveFitProfile.RuleConstrained, _) => FitRuleConstrained(input),
    };

    /// <summary>
    /// Executes local gradient-based optimization using ALGLIB's bounded L-BFGS solver (<c>minbleic</c>).
    /// </summary>
    private CurveFitOutput FitStandard(CurveFitInput input)
    {
        double[] workingParameters = (double[])input.InitialParameters.Clone();

        alglib.minbleiccreate(workingParameters, out alglib.minbleicstate optimizerState);
        alglib.minbleicsetbc(optimizerState, input.LowerBounds, input.UpperBounds);
        alglib.minbleicsetcond(optimizerState, input.Tolerance, input.Tolerance, input.Tolerance, input.MaxIterations);
        alglib.minbleicoptimize(optimizerState, (double[] currentParameters, ref double objectiveFunctionValue, double[] gradientVector, object callbackContext) =>
        {
            objectiveFunctionValue = CalculateObjectiveFunction(input, currentParameters);
            double[] computedGradientVector = CalculateNumericalGradient(input, currentParameters);
            Array.Copy(computedGradientVector, gradientVector, currentParameters.Length);
        }, null, null);
        alglib.minbleicresults(optimizerState, out workingParameters, out alglib.minbleicreport optimizerReport);

        if (optimizerReport.terminationtype > 0)
        {
            double rSquared = CalculateRSquared(input, workingParameters);
            ParameterConfidenceInterval[] parameters = CalculateConfidenceIntervals(input, workingParameters, optimizerState.f);
            return new CurveFitOutput(parameters, optimizerState.f, rSquared, optimizerReport.iterationscount);
        }

        logger.LogWarning("Failed to fit curve using ALGLIB (minbleic). Report: {@Report}", optimizerReport);
        throw new InvalidOperationException($"Failed to fit curve using ALGLIB (minbleic). Termination type: {optimizerReport.terminationtype}. Iterations: {optimizerReport.iterationscount}");
    }

    /// <summary>
    /// Executes derivative-free global optimization using ALGLIB's GDEMO (adaptive Differential Evolution / SHADE, <c>mindf</c>)
    /// with box bounds and short-circuit rejection (penalty barrier 1e12) for arbitrary non-linear parameter rules,
    /// followed by a secondary local gradient polish (<c>FitStandard</c>) from the best found solution.
    /// </summary>
    private CurveFitOutput FitRuleConstrained(CurveFitInput input)
    {
        double[] workingParameters = (double[])input.InitialParameters.Clone();
        int parameterCount = workingParameters.Length;

        alglib.mindfcreate(workingParameters, out alglib.mindfstate optimizerState);
        alglib.mindfsetbc(optimizerState, input.LowerBounds, input.UpperBounds);

        int seed = settings.Seed;
        alglib.mindfsetseed(optimizerState, seed);

        double[] parameterScales = [.. Enumerable.Repeat(1.0, parameterCount)];
        alglib.mindfsetscale(optimizerState, parameterScales);

        int populationSize = input.PopulationMultiplier * parameterCount;
        alglib.mindfsetalgogdemo(optimizerState, input.MaxIterations, populationSize);

        alglib.mindfoptimize(optimizerState, (double[] currentParameters, double[] objectiveFunctionValues, object callbackContext) =>
        {
            objectiveFunctionValues[0] = input.ValidateParameters != null && !input.ValidateParameters(currentParameters) ? 1e12 : CalculateObjectiveFunction(input, currentParameters);
        }, null, null);

        alglib.mindfresults(optimizerState, out workingParameters, out alglib.mindfreport optimizerReport);

        if (optimizerReport.terminationtype > 0)
        {
            double gdemoError = CalculateObjectiveFunction(input, workingParameters);
            try
            {
                // Secondary local polish phase using L-BFGS initialized at the global optimum found by GDEMO.
                CurveFitInput polishInput = input with { InitialParameters = (double[])workingParameters.Clone() };
                CurveFitOutput polished = FitStandard(polishInput);

                // Strictly ensure polished parameters satisfy validation constraints and improve objective function error
                if ((input.ValidateParameters == null || input.ValidateParameters(polished.OptimizedParameters)) && polished.FinalError < gdemoError)
                    return polished;
            }
            // Fall back to GDEMO global result if local gradient polish faults
            catch { }

            double rSquared = CalculateRSquared(input, workingParameters);
            ParameterConfidenceInterval[] parameters = CalculateConfidenceIntervals(input, workingParameters, gdemoError);
            return new CurveFitOutput(parameters, gdemoError, rSquared, optimizerReport.iterationscount);
        }

        logger.LogWarning("Failed to fit curve using ALGLIB (mindf GDEMO). Report: {@Report}", optimizerReport);
        throw new InvalidOperationException($"Failed to fit curve using ALGLIB (mindf GDEMO). Termination type: {optimizerReport.terminationtype}. Iterations: {optimizerReport.iterationscount}");
    }

    /// <summary>
    /// Computes statistical parameter confidence intervals using ALGLIB matrix inversion and Student-t distribution routines.
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
        double[,] jtj = new double[parameterCount, parameterCount];

        for (int r = 0; r < parameterCount; r++)
        {
            for (int c = r; c < parameterCount; c++)
            {
                double sum = 0.0;
                for (int i = 0; i < sampleCount; i++)
                {
                    sum += jacobian[i, r] * jacobian[i, c];
                }

                jtj[r, c] = sum;
                jtj[c, r] = sum;
            }
        }

        double[,] invJtj = (double[,])jtj.Clone();
        alglib.rmatrixinverse(invJtj, parameterCount, out alglib.matinvreport invReport);

        double sSquared = Math.Max(0.0, finalError / degreesOfFreedom);
        double alpha = 1.0 - confidenceLevel;
        double pStudent = 1.0 - (alpha / 2.0);
        double tCrit = alglib.invstudenttdistribution(degreesOfFreedom, pStudent);

        ParameterConfidenceInterval[] intervals = new ParameterConfidenceInterval[parameterCount];
        for (int j = 0; j < parameterCount; j++)
        {
            double variance = invReport.terminationtype > 0
                ? Math.Max(0.0, invJtj[j, j] * sSquared)
                : 0.0;

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
