using MelloSilveiraTools.Database.RelationalDatabase.Attributes;
using MelloSilveiraTools.Database.RelationalDatabase.Models;
using MelloSilveiraTools.Database.RelationalDatabase.Models.Filters;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Models;

/// <summary>
/// Filter model used to query <see cref="AnalysisStepExecutionEntity"/> records by analysis identifier and step sequence.
/// </summary>
[Filter(typeof(AnalysisStepExecutionEntity))]
public sealed record AnalysisStepExecutionFilter : FilterBase
{
    /// <summary>
    /// Filters by external business analysis identifier.
    /// </summary>
    [FilterColumn(FilterClause.Equal, nameof(AnalysisStepExecutionEntity.AnalysisIdentifier))]
    public string? AnalysisIdentifier { get; init; }

    /// <summary>
    /// Filters by minimum step sequence order.
    /// </summary>
    [FilterColumn(FilterClause.GreaterThanOrEqual, nameof(AnalysisStepExecutionEntity.StepOrder))]
    public int? MinStepOrder { get; init; }
}
