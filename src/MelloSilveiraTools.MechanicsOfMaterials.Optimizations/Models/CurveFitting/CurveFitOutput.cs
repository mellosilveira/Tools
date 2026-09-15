namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Models.CurveFitting;

public record CurveFitOutput(double[] OptimizedParameters, double FinalError, int Iterations);
