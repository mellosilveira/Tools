namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Commands.CurveFitting;

public record OptimizationOptionsRequest
{
    public int? MaxIterations { get; init; }
    public double? Tolerance { get; init; }
    public double[]? LowerBounds { get; init; }
    public double[]? UpperBounds { get; init; }
}
