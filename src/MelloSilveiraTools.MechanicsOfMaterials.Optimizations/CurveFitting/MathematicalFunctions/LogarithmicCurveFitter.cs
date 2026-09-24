using MelloSilveiraTools.Core.Pipelines.Models;
using MelloSilveiraTools.Mathematics.Functions;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Algorithms;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Models;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.MathematicalFunctions;

public sealed class LogarithmicCurveFitter(ICurveFitter curveFitter) : IMathematicalFunctionCurveFitter
{
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
                    ? (p, xValues) => new LogarithmicFunction([0, .. p], minX, maxX).Calculate(xValues[0])
                    : (p, xValues) => new LogarithmicFunction(p, minX, maxX).Calculate(xValues[0]),
                LowerBounds = input.LowerBounds ?? [.. Enumerable.Repeat(double.MinValue, parameterCount)],
                UpperBounds = input.UpperBounds ?? [.. Enumerable.Repeat(double.MaxValue, parameterCount)],
                InitialParameters = input.InitialParameters ?? [.. Enumerable.Repeat(1.0, parameterCount)],
                MaxIterations = input.MaxIterations,
                Tolerance = input.Tolerance,
                EvaluateConstraintsAndPenalties = input.EvaluateConstraintsAndPenalties
            };
            return curveFitter.Fit(fitInput);
        }
        catch (Exception ex)
        {
            return (nameof(LogarithmicCurveFitter), fitInput, ex);
        }
    }
}

