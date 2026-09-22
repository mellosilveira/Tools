using MelloSilveiraTools.Core.Pipelines.Models;
using MelloSilveiraTools.Mathematics.Functions;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Algorithms;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Models;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.MathematicalFunctions;

public sealed class PolynomialCurveFitter(ICurveFitter innerFitter) : IMathematicalFunctionCurveFitter
{
    public SafeResult<CurveFitInput, CurveFitOutput> TryFit(int numberOfParameters, double[] independentVariable, double[] dependentVariable, bool zeroBased = false, double tolerance = CurveFittingConstants.Tolerance, int maxIterations = CurveFittingConstants.MaxIterations)
    {
        CurveFitInput? input = null;
        try
        {
            if (numberOfParameters > independentVariable.Length)
                throw new ArgumentException("The number of constants to calculate cannot be greater than the number of points.");

            double minX = independentVariable[0];
            double maxX = independentVariable[^1];

            input = new()
            {
                IndependentVariables = [independentVariable],
                DependentVariable = dependentVariable,
                Calculate = zeroBased
                    ? (p, xValues) => new PolynomialFunction(minX, maxX, [0, .. p]).Calculate(xValues[0])
                    : (p, xValues) => new PolynomialFunction(minX, maxX, p).Calculate(xValues[0]),
                LowerBounds = [.. Enumerable.Repeat(double.MinValue, numberOfParameters)],
                UpperBounds = [.. Enumerable.Repeat(double.MaxValue, numberOfParameters)],
                InitialParameters = [.. Enumerable.Repeat(1.0, numberOfParameters)],
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

