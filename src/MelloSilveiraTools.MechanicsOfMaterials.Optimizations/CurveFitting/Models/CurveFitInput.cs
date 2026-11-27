namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Models;

/// <summary>
/// Input parameters definition for numerical curve fitting optimization problems.
/// </summary>
public record CurveFitInput
{
    public required double[] InitialParameters { get; init; }
    public required double[] LowerBounds { get; init; }
    public required double[] UpperBounds { get; init; }
    public required List<double[]> IndependentVariables { get; init; }
    public required double[] DependentVariable { get; init; }
    public required Func<double[], double[], double> Calculate { get; init; }

    /// <summary>
    /// Selected optimization profile (default: Automatic).
    /// </summary>
    public CurveFitProfile Profile { get; init; } = CurveFitProfile.Automatic;

    /// <summary>
    /// Short-circuit boolean rule validator. Return false for physically or commercially inadmissible combinations.
    /// </summary>
    public Func<double[], bool>? ValidateParameters { get; init; }

    /// <summary>
    /// Dynamic target fit quality goal (Coefficient of Determination, R²).
    /// </summary>
    public double? TargetRSquared { get; init; }

    public int MaxIterations { get; init; } = CurveFittingConstants.MaxIterations;
    public double Tolerance { get; init; } = CurveFittingConstants.Tolerance;
    public int PopulationMultiplier { get; init; } = 10;

    /// <summary>
    /// Desired confidence level for parameter interval estimation as a fraction in (0, 1) (e.g. 0.95 for 95% confidence).
    /// Defaults to 0.95.
    /// </summary>
    public double ConfidenceLevel { get; init; } = 0.95;
}
