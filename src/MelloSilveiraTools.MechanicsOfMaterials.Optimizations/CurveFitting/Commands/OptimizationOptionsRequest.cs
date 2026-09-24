namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Commands;

public record OptimizationOptionsRequest
{
    public int? MaxIterations { get; init; }
    public double? Tolerance { get; init; }
    public Dictionary<string, double>? LowerBounds { get; init; }
    public Dictionary<string, double>? UpperBounds { get; init; }
}
