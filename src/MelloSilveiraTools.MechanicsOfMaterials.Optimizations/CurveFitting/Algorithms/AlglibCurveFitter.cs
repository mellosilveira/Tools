using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Models;
using Microsoft.Extensions.Logging;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Algorithms;

public class AlglibCurveFitter(ILogger<AlglibCurveFitter> logger) : CurveFitterBase
{
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
            return new CurveFitOutput(workingParameters, optimizerState.f, rSquared, optimizerReport.iterationscount);
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
                {
                    return polished;
                }
            }
            catch
            {
                // Fall back to GDEMO global result if local gradient polish faults
            }

            double rSquared = CalculateRSquared(input, workingParameters);
            return new CurveFitOutput(workingParameters, gdemoError, rSquared, optimizerReport.iterationscount);
        }

        logger.LogWarning("Failed to fit curve using ALGLIB (mindf GDEMO). Report: {@Report}", optimizerReport);
        throw new InvalidOperationException($"Failed to fit curve using ALGLIB (mindf GDEMO). Termination type: {optimizerReport.terminationtype}. Iterations: {optimizerReport.iterationscount}");
    }
}
