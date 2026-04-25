using MelloSilveiraTools.Infrastructure.Database.Attributes;
using MelloSilveiraTools.Infrastructure.Database.Models.Filters;

namespace MelloSilveiraTools.Infrastructure.Plugins.Persistences;

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
