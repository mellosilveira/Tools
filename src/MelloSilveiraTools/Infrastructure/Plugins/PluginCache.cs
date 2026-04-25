using MelloSilveiraTools.Infrastructure.Caching;
using MelloSilveiraTools.Infrastructure.Plugins.Models;
using MelloSilveiraTools.Infrastructure.Plugins.Persistences;

namespace MelloSilveiraTools.Infrastructure.Plugins;

/// <summary>
/// Two-level cache for plugin state.
/// Level 1: plugin name. Level 2: <see cref="PluginVersion"/>.
/// Each (name, version) slot holds the latest pipeline stage reached:
/// <see cref="DiscoveredPlugin"/>, <see cref="LoadedPlugin"/> or <see cref="RegisteredPlugin"/>.
/// </summary>
public class PluginCache(ITwoLevelCache cache)
{
    /// <summary>
    /// Returns the cached <see cref="DiscoveredPlugin"/> for (<paramref name="name"/>, <paramref name="version"/>) or adds the one produced by <paramref name="factory"/>.
    /// </summary>
    public DiscoveredPlugin GetOrAdd(string name, PluginVersion version, Func<DiscoveredPlugin> factory)
        => cache.GetOrAdd(name, version.Name, factory);

    /// <summary>
    /// Returns the cached <see cref="LoadedPlugin"/> for (<paramref name="name"/>, <paramref name="version"/>) or adds the one produced by <paramref name="factory"/>.
    /// </summary>
    public LoadedPlugin GetOrAdd(string name, PluginVersion version, Func<LoadedPlugin> factory)
        => cache.GetOrAdd(name, version.Name, factory);

    /// <summary>
    /// Returns the cached <see cref="RegisteredPlugin"/> for (<paramref name="name"/>, <paramref name="version"/>) or adds the one produced by <paramref name="factory"/>.
    /// </summary>
    public RegisteredPlugin GetOrAdd(string name, PluginVersion version, Func<RegisteredPlugin> factory)
        => cache.GetOrAdd(name, version.Name, factory);

    /// <summary>
    /// Attempts to retrieve the cached plugin state for (<paramref name="name"/>, <paramref name="version"/>) as <typeparamref name="T"/>.
    /// </summary>
    public bool TryGet<T>(string name, PluginVersion version, out T? value) where T : DiscoveredPlugin
        => cache.TryGet(name, version.Name, out value);

    /// <summary>
    /// Replaces the cached state for (<paramref name="name"/>, <paramref name="version"/>) with <paramref name="plugin"/>.
    /// </summary>
    public void Update(string name, PluginVersion version, DiscoveredPlugin plugin)
        => cache.Set(name, version.Name, plugin);

    /// <summary>
    /// Removes every cached plugin entry.
    /// </summary>
    public void Clear() => cache.Clear();

    /// <summary>
    /// Clears all cache entries for the given plugin name.
    /// When <paramref name="version"/> is <see langword="null"/>, all versions are removed;
    /// otherwise only the specified version is evicted.
    /// </summary>
    public void Clear(string name, PluginVersion? version)
    {
        if (version is null)
            cache.Remove(name);
        else
            cache.Remove(name, version.Value.Name);
    }

    /// <summary>
    /// Streams all entries currently held in cache, regardless of pipeline stage.
    /// </summary>
    public IAsyncEnumerable<PluginCacheEntry> Stream(CancellationToken cancellationToken = default)
        => cache.StreamAll<DiscoveredPlugin>(cancellationToken).Select(MapToEntry);

    /// <summary>
    /// Streams entries filtered by <paramref name="name"/> and optionally <paramref name="version"/>.
    /// </summary>
    /// <remarks>
    /// A <see langword="null"/> or empty <paramref name="name"/> means "match all names";
    /// a <see langword="null"/> <paramref name="version"/> means "match all versions".
    /// The empty-string sentinel is normalized to <see langword="null"/> before being forwarded
    /// to the underlying <see cref="ITwoLevelCache"/>, whose filtering semantics treat
    /// <see langword="null"/> as "match all" for that level.
    /// </remarks>
    public IAsyncEnumerable<PluginCacheEntry> Stream(string name, PluginVersion? version, CancellationToken cancellationToken = default)
        => cache.StreamAll<DiscoveredPlugin>(NormalizeFilter(name), version?.Name, cancellationToken).Select(MapToEntry);

    private static string? NormalizeFilter(string? value) => string.IsNullOrEmpty(value) ? null : value;

    private static PluginCacheEntry MapToEntry((string Group, string Key, DiscoveredPlugin Plugin) tuple) => new(tuple.Group, tuple.Key, tuple.Plugin);
}
