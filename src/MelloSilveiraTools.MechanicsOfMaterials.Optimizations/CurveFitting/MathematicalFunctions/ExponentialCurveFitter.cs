using MelloSilveiraTools.Core.Pipelines.Models;
using MelloSilveiraTools.Mathematics.Functions;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Algorithms;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Models;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.MathematicalFunctions;

public sealed class ExponentialCurveFitter(ICurveFitter innerFitter) : IMathematicalFunctionCurveFitter
{
    public SafeResult<CurveFitInput, CurveFitOutput> TryFit(int numberOfParameters, double[] independentVariable, double[] dependentVariable, bool zeroBased = false, double tolerance = CurveFittingConstants.Tolerance, int maxIterations = CurveFittingConstants.MaxIterations)
    {
        CurveFitInput? input = null;
        try
        {
            int parameterCount = 2 * numberOfParameters;
            if (parameterCount > independentVariable.Length)
                throw new ArgumentException("The number of constants to calculate cannot be greater than the number of points.");

            double minX = independentVariable[0];
            double maxX = independentVariable[^1];

            double[] expInitial = new double[parameterCount];
            for (int i = 0; i < numberOfParameters; i++) expInitial[2 * i] = 1.0;

            input = new()
            {
                IndependentVariables = [independentVariable],
                DependentVariable = dependentVariable,
                Calculate = (p, xValues) => new ExponentialFunction(minX, maxX, p).Calculate(xValues[0]),
                LowerBounds = [.. Enumerable.Repeat(double.MinValue, parameterCount)],
                UpperBounds = [.. Enumerable.Repeat(double.MaxValue, parameterCount)],
                InitialParameters = expInitial,
                MaxIterations = maxIterations,
                Tolerance = tolerance
            };

            return innerFitter.Fit(input);
        }
        catch (Exception ex)
        {
            return (nameof(TryFit), input!, ex);
        }
    }
}

