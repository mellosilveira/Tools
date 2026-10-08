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
            double sumOfSquaresError = CalculateObjectiveFunction(input, workingParameters);
            double rSquared = CalculateRSquared(input, workingParameters);
            return new CurveFitOutput(workingParameters, sumOfSquaresError, rSquared, optimizerReport.iterationscount);
        }

        logger.LogWarning("Failed to fit curve using ALGLIB (mindf GDEMO). Report: {@Report}", optimizerReport);
        throw new InvalidOperationException($"Failed to fit curve using ALGLIB (mindf GDEMO). Termination type: {optimizerReport.terminationtype}. Iterations: {optimizerReport.iterationscount}");
    }
}
