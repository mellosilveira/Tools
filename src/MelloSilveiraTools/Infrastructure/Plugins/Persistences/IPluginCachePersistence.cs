using MelloSilveiraTools.Infrastructure.Plugins.Models;

namespace MelloSilveiraTools.Infrastructure.Plugins.Persistences;

/// <summary>
/// Persists and restores plugin cache state to/from non-volatile storage.
/// Operates on the two-level structure of <see cref="Infrastructure.Plugins.PluginCache"/>:
/// level 1 is the plugin name, level 2 is the version.
/// </summary>
public interface IPluginCachePersistence
{
    /// <summary>
    /// Saves the provided cache entries to non-volatile storage.
    /// Entries are consumed lazily from <paramref name="entries"/> as they are written.
    /// Existing entries with the same (name, version) are overwritten.
    /// </summary>
    Task SaveAsync(IAsyncEnumerable<PluginCacheEntry> entries, CancellationToken cancellationToken = default);

    /// <summary>
    /// Streams previously saved entries from non-volatile storage.
    /// When <paramref name="name"/> is non-empty, only entries for that plugin are returned.
    /// When <paramref name="version"/> is non-null, the result is further narrowed to that version.
    /// </summary>
    IAsyncEnumerable<PluginCacheEntry> LoadAsync(string? name = null, PluginVersion? version = null, CancellationToken cancellationToken = default);
}
