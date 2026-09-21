namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Models;

public record CurveSegment
{
    public required SegmentType Type { get; init; }
    public required double[] TimePoints { get; init; }
    public required double[] ExperimentalStress { get; init; }
    public required double[] ExperimentalStrain { get; init; }
}