using MelloSilveiraTools.Core.Pipelines.Models;
using MelloSilveiraTools.Mathematics.Functions;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Algorithms;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Models;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.MathematicalFunctions;

public sealed class ExponentialCurveFitter(ICurveFitter curveFitter) : IMathematicalFunctionCurveFitter
{
    public SafeResult<CurveFitInput, CurveFitOutput> TryFit(MathematicalCurveFitInput input)
    {
        CurveFitInput? fitInput = null;
        try
        {
            int parameterCount = 2 * input.NumberOfParameters;
            if (parameterCount > input.IndependentVariable.Length)
                throw new ArgumentException("The number of constants to calculate cannot be greater than the number of points.");

            double minX = input.IndependentVariable[0];
            double maxX = input.IndependentVariable[^1];

            fitInput = new()
            {
                IndependentVariables = [input.IndependentVariable],
                DependentVariable = input.DependentVariable,
                Calculate = (p, xValues) => new ExponentialFunction(minX, maxX, p).Calculate(xValues[0]),
                LowerBounds = input.LowerBounds ?? [.. Enumerable.Repeat(double.MinValue, parameterCount)],
                UpperBounds = input.UpperBounds ?? [.. Enumerable.Repeat(double.MaxValue, parameterCount)],
                InitialParameters = GetInitialParameters(input, parameterCount),
                MaxIterations = input.MaxIterations,
                Tolerance = input.Tolerance,
                EvaluateConstraintsAndPenalties = input.EvaluateConstraintsAndPenalties
            };

            return curveFitter.Fit(fitInput);
        }
        catch (Exception ex)
        {
            return (nameof(ExponentialCurveFitter), fitInput, ex);
        }
    }

    private static double[] GetInitialParameters(MathematicalCurveFitInput input, int parameterCount)
    {
        if (input.InitialParameters is not null)
            return input.InitialParameters;

        double[] initialParameters = new double[parameterCount];
        for (int i = 0; i < input.NumberOfParameters; i++)
            initialParameters[2 * i] = 1.0;

        return initialParameters;
    }
}

