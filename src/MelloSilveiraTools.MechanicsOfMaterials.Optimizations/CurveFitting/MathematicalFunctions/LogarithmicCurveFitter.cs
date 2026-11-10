using MelloSilveiraTools.Core.Pipelines.Models;
using MelloSilveiraTools.Mathematics.Functions;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Algorithms;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Models;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.MathematicalFunctions;

/// <summary>
/// Implements <see cref="IFunctionCurveFitter"/> for logarithmic functions.
/// </summary>
/// <param name="curveFitter">The underlying iterative curve fitting engine.</param>
public sealed class LogarithmicCurveFitter(ICurveFitter curveFitter) : IFunctionCurveFitter
{
    /// <inheritdoc/>
    public SafeResult<CurveFitInput, CurveFitOutput> TryFit(MathematicalCurveFitInput input)
    {
        CurveFitInput? fitInput = null;
        try
        {
            int parameterCount = input.ZeroBased ? 2 * input.NumberOfParameters : 2 * input.NumberOfParameters - 1;
            if (parameterCount > input.IndependentVariable.Length)
                throw new ArgumentException("The number of constants to calculate cannot be greater than the number of points.");

            double minX = input.IndependentVariable[0];
            double maxX = input.IndependentVariable[^1];

            fitInput = new()
            {
                IndependentVariables = [input.IndependentVariable],
                DependentVariable = input.DependentVariable,
                Calculate = input.ZeroBased
                    ? (p, xValues) => new LogarithmicFunction([0.0, .. p], minX, maxX).Calculate(xValues[0])
                    : (p, xValues) => new LogarithmicFunction(p, minX, maxX).Calculate(xValues[0]),
                LowerBounds = input.LowerBounds ?? [.. Enumerable.Repeat(double.MinValue, parameterCount)],
                UpperBounds = input.UpperBounds ?? [.. Enumerable.Repeat(double.MaxValue, parameterCount)],
                InitialParameters = input.InitialParameters ?? [.. Enumerable.Repeat(1.0, parameterCount)],
                MaxIterations = input.MaxIterations,
                Tolerance = input.Tolerance,
                Profile = input.Profile,
                ValidateParameters = input.ValidateParameters,
                TargetRSquared = input.TargetRSquared,
                ConfidenceLevel = input.ConfidenceLevel
            };

            CurveFitOutput output = curveFitter.Fit(fitInput);

            if (input.ZeroBased)
            {
                ParameterConfidenceInterval zeroParam = new(0, 0.0, 0.0, 0.0, 0.0, input.ConfidenceLevel);
                List<ParameterConfidenceInterval> shifted = [zeroParam];
                for (int i = 0; i < output.Parameters.Count; i++)
                {
                    ParameterConfidenceInterval p = output.Parameters[i];
                    shifted.Add(new ParameterConfidenceInterval(i + 1, p.Value, p.LowerBound, p.UpperBound, p.StandardError, p.ConfidenceLevel));
                }

                return output with { Parameters = shifted };
            }

            return output;
        }
        catch (Exception ex)
        {
            return (nameof(LogarithmicCurveFitter), fitInput, ex);
        }
    }
}
