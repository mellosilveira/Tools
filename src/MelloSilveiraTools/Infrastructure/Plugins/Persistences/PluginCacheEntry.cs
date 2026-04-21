using MelloSilveiraTools.Infrastructure.Plugins.Models;

namespace MelloSilveiraTools.Infrastructure.Plugins.Persistences;

/// <summary>
/// Represents a single two-level cache entry to be persisted or restored.
/// </summary>
public record PluginCacheEntry(string Name, string Version, DiscoveredPlugin State);
