using MelloSilveiraTools.Database.RelationalDatabase.Attributes;
using MelloSilveiraTools.Database.RelationalDatabase.Models;
using MelloSilveiraTools.Database.RelationalDatabase.Models.Filters;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.ParametricSweep.Pipelines.Entities;

/// <summary>
/// Filter model used to query <see cref="ParametricSweepEntity"/> records by external identifier
/// and optional mechanical model name.
/// </summary>
[Filter(typeof(ParametricSweepEntity))]
public record ParametricSweepFilter : FilterBase
{
    /// <summary>
    /// External pipeline identifier correlating the parametric sweep execution.
    /// </summary>
    [FilterColumn(FilterClause.Equal)]
    public string? ExternalIdentifier { get; init; }

    /// <summary>
    /// Optional filter by mechanical model name (e.g., "Fung", "SimplifiedFung", "Schapery").
    /// </summary>
    [FilterColumn(FilterClause.Equal)]
    public string? MechanicalModelName { get; init; }
}
