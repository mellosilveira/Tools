using MelloSilveiraTools.Mathematics.Functions;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Algorithms;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Models;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.MathematicalFunctions;

public sealed class PolynomialCurveFitter(ICurveFitter innerFitter) : ICurveFitter
{
    public CurveFitOutput Fit(CurveFitInput input)
    {
        double[] x = input.IndependentVariables[0];
        int n = x.Length;
        double minX = x.Min();
        double maxX = x.Max();

        int polyParamCount = n;
        double[] polyLower = Enumerable.Repeat(-1e6, polyParamCount).ToArray();
        double[] polyUpper = Enumerable.Repeat(1e6, polyParamCount).ToArray();
        double[] polyInitial = new double[polyParamCount];
        polyInitial[0] = 1.0;

        CurveFitInput modifiedInput = input with
        {
            Calculate = (p, xValues) => new PolynomialFunction(minX, maxX, p).Calculate(xValues[0]),
            LowerBounds = polyLower,
            UpperBounds = polyUpper,
            InitialParameters = polyInitial
        };

        return innerFitter.Fit(modifiedInput);
    }
}
