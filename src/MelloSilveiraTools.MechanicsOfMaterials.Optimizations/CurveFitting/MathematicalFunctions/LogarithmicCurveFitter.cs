using MelloSilveiraTools.Core.Pipelines.Models;
using MelloSilveiraTools.Mathematics.Functions;
using MelloSilveiraTools.Mathematics.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Algorithms;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Models;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.MathematicalFunctions;

public sealed class LogarithmicCurveFitter(ICurveFitter innerFitter) : IMathematicalFunctionCurveFitter
{
    public SafeResult<CurveFitInput, CurveFitOutput> TryFit(int numberOfParameters, double[] independentVariable, double[] dependentVariable, bool zeroBased = false, double tolerance = MathematicConstants.Tolerance, int maxIterations = 1_000_000)
    {
        CurveFitInput? input = null;
        try
        {
            int parameterCount = 2 * numberOfParameters - 1;
            if (parameterCount > independentVariable.Length)
                throw new ArgumentException("The number of constants to calculate cannot be greater than the number of points.");

            double minX = independentVariable[0];
            double maxX = independentVariable[^1];

            double[] lowerBounds = [.. Enumerable.Repeat(double.MinValue, parameterCount)];
            double[] upperBounds = [.. Enumerable.Repeat(double.MaxValue, parameterCount)];
            double[] initialParameters = [.. Enumerable.Repeat(1.0, parameterCount)];

            if (zeroBased)
            {
                lowerBounds[0] = 0.0;
                upperBounds[0] = 0.0;
                initialParameters[0] = 0.0;
            }

            input = new()
            {
                IndependentVariables = [independentVariable],
                DependentVariable = dependentVariable,
                Calculate = (p, xValues) => new LogarithmicFunction(minX, maxX, p).Calculate(xValues[0]),
                LowerBounds = lowerBounds,
                UpperBounds = upperBounds,
                InitialParameters = initialParameters,
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
