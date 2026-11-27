using MelloSilveiraTools.Database.RelationalDatabase.Attributes;
using MelloSilveiraTools.Database.RelationalDatabase.Models;
using MelloSilveiraTools.Database.RelationalDatabase.Models.Filters;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Pipelines.Entities;

/// <summary>
/// Filter model used to query <see cref="MechanicalModelSimulationEntity"/> records by external identifier,
/// curve fit identifier, and optional mechanical model name.
/// </summary>
[Filter(typeof(MechanicalModelSimulationEntity))]
public record MechanicalModelSimulationFilter : FilterBase
{
    /// <summary>
    /// External pipeline identifier correlating the simulation execution.
    /// </summary>
    [FilterColumn(FilterClause.Equal)]
    public string? ExternalIdentifier { get; init; }

    /// <summary>
    /// Correlated identifier of the curve fit parameter set.
    /// </summary>
    [FilterColumn(FilterClause.Equal)]
    public string? CurveFitIdentifier { get; init; }

    /// <summary>
    /// Optional filter by mechanical model name (e.g., "Fung", "SimplifiedFung", "Schapery").
    /// </summary>
    [FilterColumn(FilterClause.Equal)]
    public string? MechanicalModelName { get; init; }
}
