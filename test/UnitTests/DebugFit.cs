using System;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Algorithms;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.MathematicalFunctions;

var alglibFitter = new AlglibCurveFitter();
var expFitter = new ExponentialCurveFitter(alglibFitter);

double[] strains = new double[] { 0.03, 0.04, 0.05, 0.06 };
double[] heValues = new double[] { 1, 0.8167, 0.7672, 0.7413 };

var result = expFitter.TryFit(2, strains, heValues);
Console.WriteLine(result.Success);
if (!result.Success) {
    Console.WriteLine(result.FailedPayload?.Exception?.ToString());
}
