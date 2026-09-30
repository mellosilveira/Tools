using MelloSilveiraTools.Database.RelationalDatabase.Attributes;
using MelloSilveiraTools.Database.RelationalDatabase.Models.Entities;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Models;

/// <summary>
/// Database entity for forward numerical simulations of mechanical constitutive models.
/// </summary>
[Table("mechanical_model_simulations")]
public record MechanicalModelSimulationEntity : EntityBase
{
    /// <summary>
    /// Unique identifier for the simulation execution.
    /// </summary>
    [UniqueColumn]
    [Column]
    public required string Identifier { get; init; }

    /// <summary>
    /// Correlated identifier of the curve fit parameter set.
    /// </summary>
    [Column]
    public required string CurveFitIdentifier { get; init; }

    /// <summary>
    /// Name of the mechanical model.
    /// </summary>
    [Column]
    public required string MechanicalModelName { get; init; }

    /// <summary>
    /// Generated output simulation CSV file name.
    /// </summary>
    [Column]
    public string? OutputFileName { get; init; }

    /// <summary>
    /// Time when numerical asymptote convergence was detected, or null if convergence was not reached.
    /// </summary>
    [Column]
    public double? AsymptoteTime { get; init; }

    #region Full Complex Output Snapshots (JSON)
    /// <summary>
    /// Serialized JSON snapshot of the initial simulation state.
    /// </summary>
    [Column]
    public required string InitialOutputJson { get; init; }

    /// <summary>
    /// Serialized JSON snapshot of the final simulation state.
    /// </summary>
    [Column]
    public required string FinalOutputJson { get; init; }

    /// <summary>
    /// Serialized JSON snapshot of the absolute output variation (Final - Initial).
    /// </summary>
    [Column]
    public required string AbsoluteDeltaOutputJson { get; init; }

    /// <summary>
    /// Serialized JSON snapshot of the percentage output variation.
    /// </summary>
    [Column]
    public required string PercentageDeltaOutputJson { get; init; }
    #endregion
}
