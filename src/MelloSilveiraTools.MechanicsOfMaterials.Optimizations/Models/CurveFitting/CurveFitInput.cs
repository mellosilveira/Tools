using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Models.CurveFitting;

public record CurveFitInput
{
    public GenericMechanicalModelInput InitialMechanicalModelInput { get; init; }
    public required double[] TimePoints { get; init; }
    public required double[] StrainPoints { get; init; }
    public required double[] StressPoints { get; init; }
    public OptimizationOptions Options { get; init; }
    public Func<GenericMechanicalModelInput, double, double, double> CalculateStress { get; init; }
    public Func<GenericMechanicalModelInput, double> EvaluateConstraintsAndPenalties { get; init; }
}
