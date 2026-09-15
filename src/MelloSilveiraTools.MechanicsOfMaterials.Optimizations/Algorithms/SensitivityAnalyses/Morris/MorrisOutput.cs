namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Algorithms.SensitivityAnalyses.Morris;

/// <summary>
/// The final result object containing all calculated metrics.
/// </summary>
public record MorrisOutput
{
    /// <summary>
    /// A flat list of every computed metric combination (Parameter x Output).
    /// </summary>
    public IReadOnlyCollection<MorrisMetrics> Results { get; init; }
}
