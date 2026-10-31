using MelloSilveiraTools.Core.Pipelines.Models;
using MelloSilveiraTools.Mathematics.Functions;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Algorithms;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Models;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.MathematicalFunctions;

/// <summary>
/// Implements <see cref="IFunctionCurveFitter"/> for polynomial functions,
/// leveraging analytical least-squares fitting via the Vandermonde matrix and QR decomposition
/// with fallback to the configured <see cref="ICurveFitter"/> for non-linear box-constrained optimization.
/// </summary>
/// <param name="curveFitter">The underlying iterative curve fitting engine.</param>
public sealed class PolynomialCurveFitter(ICurveFitter curveFitter) : IFunctionCurveFitter
{
    /// <inheritdoc/>
    public SafeResult<CurveFitInput, CurveFitOutput> TryFit(MathematicalCurveFitInput input)
    {
        CurveFitInput? fitInput = null;
        try
        {
            if (input.NumberOfParameters > input.IndependentVariable.Length)
                throw new ArgumentException("The number of constants to calculate cannot be greater than the number of points.");

            double minX = input.IndependentVariable[0];
            double maxX = input.IndependentVariable[^1];

            // 1. Analytical least-squares estimation via Vandermonde Matrix
            double[]? analyticalParameters = input.InitialParameters ?? VandermondeLeastSquares.Fit(input.IndependentVariable, input.DependentVariable, input.NumberOfParameters, input.ZeroBased, input.Tolerance);

            // 2. Analytical shortcut evaluation
            CurveFitOutput? analyticalOutput = TryEvaluateAnalyticalSolution(input, analyticalParameters, minX, maxX);
            if (analyticalOutput is not null)
                return analyticalOutput;

            // 3. Fallback to iterative optimization via innerFitter initialized with the analytical Vandermonde parameters
            double[] initialParameters = analyticalParameters ?? [.. Enumerable.Repeat(1.0, input.NumberOfParameters)];
            fitInput = new()
            {
                IndependentVariables = [input.IndependentVariable],
                DependentVariable = input.DependentVariable,
                Calculate = input.ZeroBased
                    ? (p, xValues) => new PolynomialFunction([0, .. p], minX, maxX).Calculate(xValues[0])
                    : (p, xValues) => new PolynomialFunction(p, minX, maxX).Calculate(xValues[0]),
                LowerBounds = input.LowerBounds ?? [.. Enumerable.Repeat(double.MinValue, input.NumberOfParameters)],
                UpperBounds = input.UpperBounds ?? [.. Enumerable.Repeat(double.MaxValue, input.NumberOfParameters)],
                InitialParameters = initialParameters,
                MaxIterations = input.MaxIterations,
                Tolerance = input.Tolerance,
                Profile = input.Profile,
                ValidateParameters = input.ValidateParameters,
                TargetRSquared = input.TargetRSquared
            };
            CurveFitOutput output = curveFitter.Fit(fitInput);
            return input.ZeroBased
                ? output with { OptimizedParameters = [0.0, .. output.OptimizedParameters] }
                : output;
        }
        catch (Exception ex)
        {
            return (nameof(PolynomialCurveFitter), fitInput, ex);
        }
    }

    private static CurveFitOutput? TryEvaluateAnalyticalSolution(MathematicalCurveFitInput input, double[]? analyticalParameters, double minX, double maxX)
    {
        if (analyticalParameters is null || input.InitialParameters is not null || input.Profile == CurveFitProfile.RuleConstrained)
            return null;

        if (input.LowerBounds is not null)
        {
            for (int i = 0; i < analyticalParameters.Length; i++)
            {
                if (analyticalParameters[i] < input.LowerBounds[i])
                    return null;
            }
        }

        if (input.UpperBounds is not null)
        {
            for (int i = 0; i < analyticalParameters.Length; i++)
            {
                if (analyticalParameters[i] > input.UpperBounds[i])
                    return null;
            }
        }

        if (input.ValidateParameters is not null && !input.ValidateParameters(analyticalParameters))
            return null;

        double ssr = 0.0;
        double sumY = 0.0;
        int n = input.IndependentVariable.Length;
        for (int i = 0; i < n; i++)
        {
            double x = input.IndependentVariable[i];
            double y = input.DependentVariable[i];
            sumY += y;
            double yPred = input.ZeroBased
                ? new PolynomialFunction([0, .. analyticalParameters], minX, maxX).Calculate(x)
                : new PolynomialFunction(analyticalParameters, minX, maxX).Calculate(x);
            double diff = y - yPred;
            ssr += diff * diff;
        }

        double meanY = sumY / n;
        double sst = 0.0;
        for (int i = 0; i < n; i++)
        {
            double diff = input.DependentVariable[i] - meanY;
            sst += diff * diff;
        }

        double rSquared = sst > input.Tolerance ? 1.0 - (ssr / sst) : 1.0;
        if (rSquared < 0.0)
            rSquared = 0.0;

        return input.TargetRSquared is not null && rSquared < input.TargetRSquared.Value
            ? null
            : new CurveFitOutput(input.ZeroBased ? [0.0, .. analyticalParameters] : analyticalParameters, ssr, rSquared, 1);
    }
}