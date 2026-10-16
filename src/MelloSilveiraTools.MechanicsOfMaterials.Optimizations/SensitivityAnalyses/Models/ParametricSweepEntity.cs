using MelloSilveiraTools.Database.RelationalDatabase.Attributes;
using MelloSilveiraTools.Database.RelationalDatabase.Models.Entities;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.Models;

/// <summary>
/// Database entity representing a One-At-A-Time (OAAT) parametric sensitivity sweep execution and its configuration snapshot.
/// </summary>
[Table("parametric_sweeps")]
public record ParametricSweepEntity : EntityBase
{
    /// <summary>
    /// Unique external business or run identifier correlating the sweep execution with generated artifacts.
    /// </summary>
    [UniqueColumn]
    [Column]
    public required string ExternalIdentifier { get; init; }

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
