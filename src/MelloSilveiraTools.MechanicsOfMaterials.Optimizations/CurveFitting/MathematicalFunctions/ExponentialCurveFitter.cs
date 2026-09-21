using MelloSilveiraTools.Mathematics.Functions;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Algorithms;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Models;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.MathematicalFunctions;

public sealed class ExponentialCurveFitter(ICurveFitter innerFitter) : ICurveFitter
{
    public CurveFitOutput Fit(CurveFitInput input)
    {
        double[] x = input.IndependentVariables[0];
        int n = x.Length;
        double minX = x.Min();
        double maxX = x.Max();

        int expParamCount = 2 * n;
        double[] expLower = Enumerable.Repeat(-1e6, expParamCount).ToArray();
        double[] expUpper = Enumerable.Repeat(1e6, expParamCount).ToArray();
        double[] expInitial = new double[expParamCount];
        for (int i = 0; i < n; i++) expInitial[2 * i] = 1.0; 

        CurveFitInput modifiedInput = input with
        {
            Calculate = (p, xValues) => new ExponencialFunction(minX, maxX, p).Calculate(xValues[0]),
            LowerBounds = expLower,
            UpperBounds = expUpper,
            InitialParameters = expInitial
        };

        return innerFitter.Fit(modifiedInput);
    }
}
