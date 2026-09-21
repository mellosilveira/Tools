using MelloSilveiraTools.Mathematics.Models;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Models;

public record CurveFitInput
{
    public required double[] InitialParameters { get; init; }
    public required double[] LowerBounds { get; init; }
    public required double[] UpperBounds { get; init; }
    public required List<double[]> IndependentVariables { get; init; }
    public required double[] DependentVariable { get; init; }
    public required Func<double[], double[], double> Calculate { get; init; }
    public Func<double[], double>? EvaluateConstraintsAndPenalties { get; init; }
    public int MaxIterations { get; init; } = 1_000_000;
    public double Tolerance { get; init; } = MathematicConstants.Tolerance;
}
