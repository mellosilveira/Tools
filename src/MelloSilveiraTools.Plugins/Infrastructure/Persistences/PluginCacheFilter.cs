using MelloSilveiraTools.Database.Infrastructure.Database.Attributes;
using MelloSilveiraTools.Database.Infrastructure.Database.Models.Filters;

namespace MelloSilveiraTools.Plugins.Infrastructure.Persistences;

/// <summary>
/// Filter for querying <see cref="PluginCacheEntity"/> rows.
/// Null properties are excluded from the WHERE clause.
/// </summary>
public record PluginCacheFilter : FilterBase
{
    /// <summary>Exact-match filter on <see cref="PluginCacheEntity.PluginName"/>.</summary>
    [FilterColumn("=")]
    public string? PluginName { get; init; }

    /// <summary>Exact-match filter on <see cref="PluginCacheEntity.PluginVersion"/>.</summary>
    [FilterColumn("=")]
    public string? PluginVersion { get; init; }
}
