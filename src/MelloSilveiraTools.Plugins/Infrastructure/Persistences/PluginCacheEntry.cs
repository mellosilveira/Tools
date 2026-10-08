using MelloSilveiraTools.Plugins.Infrastructure.Models;

namespace MelloSilveiraTools.Plugins.Infrastructure.Persistences;

/// <summary>
/// Represents a single two-level cache entry to be persisted or restored.
/// </summary>
/// <param name="Name">The plugin name representing the first-level cache key.</param>
/// <param name="Version">The plugin version string representing the second-level cache key.</param>
/// <param name="State">The cached <see cref="DiscoveredPlugin"/> state instance associated with the entry.</param>
public record PluginCacheEntry(string Name, string Version, DiscoveredPlugin State);
