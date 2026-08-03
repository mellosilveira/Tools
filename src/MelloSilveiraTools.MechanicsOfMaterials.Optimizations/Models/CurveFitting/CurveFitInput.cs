using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Models.CurveFitting;

public record CurveFitInput
{
    public GenericMechanicalModelInput InitialInput { get; init; }
    public CurveSegment[] Segments { get; init; }
    public OptimizationOptions Options { get; init; }
    public Func<GenericMechanicalModelInput, double> EvaluateConstraintsAndPenalties { get; init; }
}
