using MelloSilveiraTools.Mathematics.Models;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Models.CurveFitting;

public record CurveFitInput
{
    public required double[] InitialParameters { get; init; }
    public required double[] LowerBounds { get; init; }
    public required double[] UpperBounds { get; init; }
    public required double[] TimePoints { get; init; }
    public required double[] StrainPoints { get; init; }
    public required double[] StressPoints { get; init; }
    public required Func<double[], double, double, double> CalculateStress { get; init; }
    public Func<double[], double>? EvaluateConstraintsAndPenalties { get; init; }
    public int MaxIterations { get; init; } = 1_000_000;
    public double Tolerance { get; init; } = MathematicConstants.Tolerance;
}
