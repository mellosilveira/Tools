using MelloSilveiraTools.Database.RelationalDatabase.Attributes;
using MelloSilveiraTools.Database.RelationalDatabase.Models;
using MelloSilveiraTools.Database.RelationalDatabase.Models.Filters;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Pipelines.Entities;

/// <summary>
/// Filter model used to query <see cref="MechanicalModelCurveFitOutputEntity"/> records by external identifier and optional mechanical model name.
/// </summary>
[Filter(typeof(MechanicalModelCurveFitOutputEntity))]
public record MechanicalModelCurveFitOutputFilter : FilterBase
{
    /// <summary>
    /// External pipeline identifier correlating the experimental run.
    /// </summary>
    [FilterColumn(FilterClause.Equal)]
    public string? ExtenalIdentifier { get; init; }

    /// <summary>
    /// Optional filter by mechanical model name (e.g., "Fung", "SimplifiedFung", "Schapery").
    /// </summary>
    [FilterColumn(FilterClause.Equal)]
    public string? MechanicalModelName { get; init; }
}
