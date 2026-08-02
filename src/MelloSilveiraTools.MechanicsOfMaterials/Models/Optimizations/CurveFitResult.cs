using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;

namespace MelloSilveiraTools.MechanicsOfMaterials.Models.Optimizations;

public record CurveFitInput
{
    public GenericMechanicalModelInput InitialInput { get; init; }
    public CurveSegment[] Segments { get; init; }
    public OptimizationOptions Options { get; init; }
    public Func<GenericMechanicalModelInput, double> EvaluateConstraintsAndPenalties { get; init; }
}

public record CurveFitResult(
    bool IsSuccessful,
    double[] OptimizedParameters,
    double FinalError,
    string Message
);

public enum SegmentType
{
    Ramp,
    Relaxation,
    Descent,
    Recovery
}

public record CurveSegment
{
    public SegmentType Type { get; init; }
    public double[] TimePoints { get; init; }
    public double[] ExperimentalStress { get; init; }

    public double[] ExperimentalStrain { get; init; }
}