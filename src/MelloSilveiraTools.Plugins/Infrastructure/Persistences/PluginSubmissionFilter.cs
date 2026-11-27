using MelloSilveiraTools.Database.RelationalDatabase.Attributes;
using MelloSilveiraTools.Database.RelationalDatabase.Models;
using MelloSilveiraTools.Database.RelationalDatabase.Models.Filters;
using MelloSilveiraTools.Plugins.Infrastructure.Models;

namespace MelloSilveiraTools.Plugins.Infrastructure.Persistences;

/// <summary>
/// Filter for querying <see cref="PluginSubmissionEntity"/> rows via <c>IRepository</c>.
/// </summary>
[Filter(typeof(PluginSubmissionEntity))]
public record PluginSubmissionFilter : FilterBase
{
    /// <summary>Optional exact author user ID filter.</summary>
    [FilterColumn(FilterClause.Equal)]
    public long? UserId { get; init; }

    /// <summary>Optional case-insensitive plugin name filter.</summary>
    [FilterColumn(FilterClause.ILike)]
    public string? PluginName { get; init; }

    /// <summary>Optional exact semantic version filter (e.g. <c>v1.0.0</c>).</summary>
    [FilterColumn(FilterClause.Equal)]
    public string? PluginVersion { get; init; }

    /// <summary>Optional exact moderation status filter.</summary>
    [FilterColumn(FilterClause.Equal)]
    public PluginSubmissionStatus? Status { get; init; }
}
