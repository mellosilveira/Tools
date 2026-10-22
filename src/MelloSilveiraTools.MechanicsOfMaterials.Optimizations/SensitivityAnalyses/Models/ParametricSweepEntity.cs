using MelloSilveiraTools.Database.RelationalDatabase.Attributes;
using MelloSilveiraTools.Database.RelationalDatabase.Models.Entities;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.Models;

/// <summary>
/// Database entity representing a One-At-A-Time (OAAT) parametric sensitivity sweep execution and its configuration snapshot.
/// </summary>
[Table("parametric_sweeps")]
public record ParametricSweepEntity : EntityBase
{
    /// <summary>
    /// Unique deterministic hash identifier for the parametric sweep configuration.
    /// </summary>
    [UniqueColumn]
    public required string Identifier { get; init; }

    /// <summary>
    /// External business or run identifier correlating the sweep execution with generated artifacts.
    /// </summary>
    [Column]
    public string? ExternalIdentifier { get; init; }

    /// <summary>
    /// Name of the evaluated mechanical constitutive model (e.g., "Fung", "SimplifiedFung", "Schapery").
    /// </summary>
    [Column]
    public required string MechanicalModelName { get; init; }

    /// <summary>
    /// Baseline strain level applied when strain is not the swept variable.
    /// </summary>
    [Column]
    public required double BaseStrain { get; init; }

    /// <summary>
    /// Configuration for how ramp time is considered during the simulation.
    /// </summary>
    [Column]
    public required RampTimeConsideration RampTimeConsideration { get; init; }

    /// <summary>
    /// Duration of the initial loading ramp, if applicable.
    /// </summary>
    [Column]
    public double? RampTime { get; init; }

    /// <summary>
    /// Initial simulation evaluation time ($t_0$).
    /// </summary>
    [Column]
    public required double InitialTime { get; init; }

    /// <summary>
    /// Final simulation evaluation time ($t_f$).
    /// </summary>
    [Column]
    public required double FinalTime { get; init; }

    /// <summary>
    /// Time step increment ($\Delta t$) used during numerical integration.
    /// </summary>
    [Column]
    public required double TimeStep { get; init; }

    /// <summary>
    /// Relative tolerance threshold utilized for steady-state asymptote detection.
    /// </summary>
    [Column]
    public required double AsymptoteTolerance { get; init; }

    /// <summary>
    /// Polymorphic JSON snapshot of the baseline constitutive parameters formatted to 7 significant figures.
    /// </summary>
    [Column]
    public required string BaselineParametersJson { get; init; }

    /// <summary>
    /// JSON snapshot of the swept variable ranges (constant, array, and strain ranges) formatted to 7 significant figures.
    /// </summary>
    [Column]
    public required string VariableRangesJson { get; init; }
}
