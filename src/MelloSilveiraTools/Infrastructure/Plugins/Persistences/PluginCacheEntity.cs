using MelloSilveiraTools.Infrastructure.Database.Attributes;
using MelloSilveiraTools.Infrastructure.Database.Models.Entities;
using MelloSilveiraTools.Infrastructure.Plugins.Models;

namespace MelloSilveiraTools.Infrastructure.Plugins.Persistences;

/// <summary>
/// Database entity that represents a single plugin cache entry.
/// The combination of (<see cref="PluginName"/>, <see cref="PluginVersion"/>) is unique.
/// </summary>
[Table("plugin_cache")]
public record PluginCacheEntity : EntityBase
{
    /// <summary>Plugin name — two-level cache level 1 key. Part of the (name, version) unique constraint.</summary>
    [UniqueColumn]
    public string PluginName { get; init; }

    /// <summary>Version string (e.g. "v1.2.3") — two-level cache level 2 key. Part of the (name, version) unique constraint.</summary>
    [UniqueColumn]
    public string PluginVersion { get; init; }

    /// <summary>
    /// Simple name of the concrete <see cref="DiscoveredPlugin"/> subtype stored in <see cref="StateJson"/>
    /// (e.g. "LoadedPlugin", "RegisteredPlugin"). Used as a discriminator during deserialization.
    /// </summary>
    [Column]
    public string StateType { get; init; }

    /// <summary>JSON-serialized representation of the cache state.</summary>
    [Column]
    public string StateJson { get; init; }
}
