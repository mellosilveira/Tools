namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.SensitivityAnalysis.Models;

/// <summary>
/// Settings for the Sensitivity Analysis pipeline.
/// </summary>
public sealed record SensitivityAnalysisSettings
{
    /// <summary>
    /// The default number of trajectories to run if not specified in the input.
    /// </summary>
    public int DefaultTrajectories { get; init; } = 10;

    /// <summary>
    /// The default number of grid levels to use if not specified in the input.
    /// </summary>
    public int DefaultLevels { get; init; } = 4;
}
