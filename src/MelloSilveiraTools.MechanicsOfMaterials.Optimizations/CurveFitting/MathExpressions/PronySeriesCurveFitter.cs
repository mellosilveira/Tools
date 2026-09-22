using MelloSilveiraTools.Mathematics.Expressions;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Algorithms;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Models;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.MathExpressions;

/// <summary>
/// Specific curve fitter for <see cref="PronySeries"/>.
/// </summary>
public sealed class PronySeriesCurveFitter(ICurveFitter innerFitter) : IMathExpressionCurveFitter
{
    /// <inheritdoc />
    public CurveFitOutput Fit(MathematicalCurveFitInput input)
    {
        double initialValue = input.DependentVariable[0];
        CurveFitInput fitInput = new()
        {
            IndependentVariables = [input.IndependentVariable],
            DependentVariable = input.DependentVariable,
            Calculate = (parameters, xValues) =>
            {
                PronySeries prony = new(independentParameter: parameters[0], iteratorCoefficients: parameters[1..]);
                return prony.Calculate(xValues[0]);
            },
            LowerBounds = input.LowerBounds ?? [.. Enumerable.Repeat(double.MinValue, input.NumberOfParameters)],
            UpperBounds = input.UpperBounds ?? [.. Enumerable.Repeat(double.MaxValue, input.NumberOfParameters)],
            InitialParameters = input.InitialParameters ?? [.. Enumerable.Repeat(1.0, input.NumberOfParameters)],
            Tolerance = input.Tolerance,
            MaxIterations = input.MaxIterations,
            EvaluateConstraintsAndPenalties = input.EvaluateConstraintsAndPenalties ?? (parameters =>
            {
                double sum = parameters[0];
                for (int i = 0; i < (parameters.Length - 1) / 2; i++)
                    sum += parameters[2 * i + 1];

                double diff = sum - initialValue;
                return diff * diff;
            })
        };
        return innerFitter.Fit(fitInput);
    }
}
