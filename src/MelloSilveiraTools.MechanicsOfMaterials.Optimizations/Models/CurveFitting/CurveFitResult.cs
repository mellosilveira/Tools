namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Models.CurveFitting;

public record CurveFitResult(bool IsSuccessful, double[] OptimizedParameters, double FinalError, string Message);
