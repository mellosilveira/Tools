namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Models.CurveFitting;

public record CurveFitResultData<TConstitutiveParameters>(TConstitutiveParameters OptimizedParameters, double FinalError, int Iterations);
