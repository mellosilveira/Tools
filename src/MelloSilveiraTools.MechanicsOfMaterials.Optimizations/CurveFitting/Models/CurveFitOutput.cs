namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Models;

public record CurveFitOutput(double[] OptimizedParameters, double FinalError, double RSquared, int Iterations);
