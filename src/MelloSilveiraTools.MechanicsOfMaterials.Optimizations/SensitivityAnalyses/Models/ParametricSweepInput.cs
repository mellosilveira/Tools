using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Models.Range;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.Models;

/// <summary>
/// Represents the input request for a Full Factorial Parametric Sweep (Grid Search) analysis.
/// </summary>
public record ParametricSweepInput
{
    /// <summary>
    /// Optional predefined analysis identifier. If not provided, a unique GUID-based identifier is generated.
    /// </summary>
    public string? Identifier { get; init; }

    /// <summary>
    /// Gets the name of the mechanical model to be evaluated (e.g., "Maxwell", "Fung").
    /// </summary>
    public required string MechanicalModelName { get; init; }

    /// <summary>
    /// Gets the baseline constitutive parameters.
    /// </summary>
    public required ConstitutiveParameters Baseline { get; init; }

    /// <summary>
    /// Gets the ranges for constant scalar variables.
    /// </summary>
    public required Dictionary<string, RangeParameters> ConstantVariables { get; init; }

    /// <summary>
    /// Gets the ranges for array-based variables.
    /// </summary>
    public required Dictionary<string, Dictionary<int, RangeParameters>> ArrayVariables { get; init; }

    public required double BaseStrain { get; init; }

    /// <summary>
    /// Gets the range of strain to be applied in the mechanical simulation.
    /// </summary>
    public RangeParameters? StrainRange { get; init; }

    /// <summary>
    /// Gets the configuration for how ramp time is considered in the simulation.
    /// </summary>
    public required RampTimeConsideration RampTimeConsideration { get; init; }

    /// <summary>
    /// Gets the duration of the initial loading ramp, if applicable.
    /// </summary>
    public double? RampTime { get; init; }

    /// <summary>
    /// Gets the initial time to evaluate the initial stress.
    /// </summary>
    public required double InitialTime { get; init; }

    /// <summary>
    /// Gets the time step increment for the simulation.
    /// </summary>
    public required double TimeStep { get; init; }

    /// <summary>
    /// Gets the final time of the simulation.
    /// </summary>
    public required double FinalTime { get; init; }

    /// <summary>
    /// Gets the relative tolerance threshold utilized by the asymptote detection step.
    /// Defaults to 1e-3 (0.1% of the initial relaxation rate).
    /// </summary>
    public double AsymptoteTolerance { get; init; } = 1e-3;

    /// <summary>
    /// Alias for <see cref="AsymptoteTolerance"/> allowing fluent configuration.
    /// </summary>
    public double Tolerance
    {
        get => AsymptoteTolerance;
        init => AsymptoteTolerance = value;
    }

    /// <summary>
    /// Gets the destination directory where partitioned CSV sensitivity curves are generated.
    /// </summary>
    public required string OutputDirectory { get; init; }
}
