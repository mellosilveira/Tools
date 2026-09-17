namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Algorithms.SensitivityAnalyses.Morris;

/// <summary>
/// The core diagnostic metrics for a specific parameter against a specific mechanical output.
/// </summary>
public record MorrisMetrics
{
    public string ParameterPath { get; init; }

    public string TargetOutput { get; init; }

    /// <summary>
    /// Standard Mean. Can indicate directional effect, but risks positive/negative cancellation.
    /// </summary>
    public double Mu { get; init; }

    /// <summary>
    /// Absolute Mean. The overall magnitude of influence this parameter has on the output.
    /// </summary>
    public double MuStar { get; init; }

    /// <summary>
    /// Standard Deviation. Measures non-linearity and interaction with other parameters.
    /// </summary>
    public double Sigma { get; init; }
}
