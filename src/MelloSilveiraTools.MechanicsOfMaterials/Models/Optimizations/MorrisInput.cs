using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;

namespace MelloSilveiraTools.MechanicsOfMaterials.Models.Optimizations;

/// <summary>
/// The exact payload required to start a Morris Screening analysis.
/// </summary>
public record MorrisInput
{
    /// <summary>
    /// Discriminator to identify the specific mechanical model (e.g., "Schapery", "Fung").
    /// </summary>
    public string MechanicalModelName { get; init; }

    /// <summary>
    /// The un-mutated biological configuration.
    /// </summary>
    public ConstitutiveParameters BaselineConfiguration { get; init; }

    /// <summary>
    /// The list of parameters to include in the screening (defines 'k').
    /// </summary>
    public IReadOnlyCollection<MorrisParameterBoundary> Boundaries { get; init; }

    /// <summary>
    /// The mechanical outputs to analyze (e.g., "Stress", "Force").
    /// </summary>
    public IReadOnlyCollection<string> TargetOutputs { get; init; }

    /// <summary>
    /// Number of trajectories (r) - random starting points in the parameter space.
    /// </summary>
    public int Trajectories { get; init; } = 10;

    /// <summary>
    /// Number of levels (p) - the resolution of the normalized grid.
    /// </summary>
    public int Levels { get; init; } = 4;
}