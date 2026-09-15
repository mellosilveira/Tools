using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Models.CurveFitting;

public record CurveFitInput<TConstitutiveParameters>
    where TConstitutiveParameters : ConstitutiveParameters
{
    public MechanicalModelInput<TConstitutiveParameters> InitialMechanicalModelInput { get; init; }
    public required double[] TimePoints { get; init; }
    public required double[] StrainPoints { get; init; }
    public required double[] StressPoints { get; init; }
    public OptimizationOptions Options { get; init; }
    public Func<MechanicalModelInput<TConstitutiveParameters>, double, double, double> CalculateStress { get; init; }
    public Func<MechanicalModelInput<TConstitutiveParameters>, double> EvaluateConstraintsAndPenalties { get; init; }
}
