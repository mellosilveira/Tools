using MelloSilveiraTools.Plugins.Infrastructure.Models;

namespace MelloSilveiraTools.Plugins.Infrastructure.Persistences;

/// <summary>
/// Represents a single two-level cache entry to be persisted or restored.
/// </summary>
public record PluginCacheEntry(string Name, string Version, DiscoveredPlugin State);
