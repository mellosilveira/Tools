using MelloSilveiraTools.Mathematics.Functions;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Algorithms;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Models;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.MathematicalFunctions;

public sealed class LogarithmicCurveFitter(ICurveFitter innerFitter) : ICurveFitter
{
    public CurveFitOutput Fit(CurveFitInput input)
    {
        double[] x = input.IndependentVariables[0];
        int n = x.Length;
        double minX = x.Min();
        double maxX = x.Max();

        int logParamCount = 2 * n + 1;
        double[] logLower = Enumerable.Repeat(-1e6, logParamCount).ToArray();
        double[] logUpper = Enumerable.Repeat(1e6, logParamCount).ToArray();
        double[] logInitial = new double[logParamCount];
        logInitial[0] = 1.0; 
        for (int i = 0; i < n; i++) 
        {
            logInitial[2 * i + 1] = 1.0; 
            logInitial[2 * i + 2] = 1.0; 
        }

        CurveFitInput modifiedInput = input with
        {
            Calculate = (p, xValues) => new LogarithmicFunction(minX, maxX, p).Calculate(xValues[0]),
            LowerBounds = logLower,
            UpperBounds = logUpper,
            InitialParameters = logInitial
        };

        return innerFitter.Fit(modifiedInput);
    }
}
