using System;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Models;

public record MathematicalCurveFitInput
{
    public required int NumberOfParameters { get; init; }
    public required double[] IndependentVariable { get; init; }
    public required double[] DependentVariable { get; init; }
    public bool ZeroBased { get; init; } = false;
    public double Tolerance { get; init; } = CurveFittingConstants.Tolerance;
    public int MaxIterations { get; init; } = CurveFittingConstants.MaxIterations;
    public double[]? LowerBounds { get; init; }
    public double[]? UpperBounds { get; init; }
    public double[]? InitialParameters { get; init; }
    public Func<double[], double>? EvaluateConstraintsAndPenalties { get; init; }
}
