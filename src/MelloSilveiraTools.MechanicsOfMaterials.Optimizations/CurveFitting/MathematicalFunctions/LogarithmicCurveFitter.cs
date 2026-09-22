using MelloSilveiraTools.Core.Pipelines.Models;
using MelloSilveiraTools.Mathematics.Functions;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Algorithms;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Models;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.MathematicalFunctions;

public sealed class LogarithmicCurveFitter(ICurveFitter innerFitter) : IMathematicalFunctionCurveFitter
{
    public SafeResult<CurveFitInput, CurveFitOutput> TryFit(int numberOfParameters, double[] independentVariable, double[] dependentVariable, bool zeroBased = false, double tolerance = CurveFittingConstants.Tolerance, int maxIterations = CurveFittingConstants.MaxIterations)
    {
        CurveFitInput? input = null;
        try
        {
            int parameterCount = zeroBased ? 2 * numberOfParameters : 2 * numberOfParameters - 1;
            if (parameterCount > independentVariable.Length)
                throw new ArgumentException("The number of constants to calculate cannot be greater than the number of points.");

            double minX = independentVariable[0];
            double maxX = independentVariable[^1];

            input = new()
            {
                IndependentVariables = [independentVariable],
                DependentVariable = dependentVariable,
                Calculate = zeroBased 
                    ? (p, xValues) => new LogarithmicFunction(minX, maxX, [0, ..p]).Calculate(xValues[0])
                    : (p, xValues) => new LogarithmicFunction(minX, maxX, p).Calculate(xValues[0]),
                LowerBounds = [.. Enumerable.Repeat(double.MinValue, parameterCount)],
                UpperBounds = [.. Enumerable.Repeat(double.MaxValue, parameterCount)],
                InitialParameters = [.. Enumerable.Repeat(1.0, parameterCount)],
                MaxIterations = maxIterations,
                Tolerance = tolerance
            };
            return innerFitter.Fit(input);
        }
        catch (Exception ex)
        {
            return (nameof(LogarithmicCurveFitter), input, ex);
        }
    }
}

