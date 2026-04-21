using System.Runtime.CompilerServices;
using MelloSilveiraTools.Infrastructure.Caching;
using MelloSilveiraTools.Infrastructure.Plugins.Models;

namespace MelloSilveiraTools.Infrastructure.Plugins;

/// <summary>
/// Two-level cache for plugin state.
/// Level 1: plugin name. Level 2: <see cref="PluginVersion"/>.
/// Each (name, version) slot holds the latest pipeline stage reached:
/// <see cref="DiscoveredPlugin"/>, <see cref="LoadedPlugin"/> or <see cref="RegisteredPlugin"/>.
/// </summary>
public class PluginCache(ITwoLevelCache cache)
{
    public DiscoveredPlugin GetOrAdd(string name, PluginVersion version, Func<DiscoveredPlugin> factory)
        => cache.GetOrAdd(name, version.Name, factory);

    public LoadedPlugin GetOrAdd(string name, PluginVersion version, Func<LoadedPlugin> factory)
        => cache.GetOrAdd(name, version.Name, factory);

    public RegisteredPlugin GetOrAdd(string name, PluginVersion version, Func<RegisteredPlugin> factory)
        => cache.GetOrAdd(name, version.Name, factory);

    public bool TryGet<T>(string name, PluginVersion version, out T? value) where T : DiscoveredPlugin
        => cache.TryGet(name, version.Name, out value);

    public void Update(string name, PluginVersion version, DiscoveredPlugin plugin)
        => cache.Set(name, version.Name, plugin);

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
    /// Returns all entries currently held in cache, regardless of pipeline stage.
    /// </summary>
    public IReadOnlyList<DiscoveredPlugin> GetAll()
    {
        var result = new List<DiscoveredPlugin>();

        foreach (var (name, versionName) in cache.GetKeys())
            if (cache.TryGet<DiscoveredPlugin>(name, versionName, out var plugin))
                result.Add(plugin!);

        return result;
    }

    /// <summary>
    /// Streams all entries currently held in cache, regardless of pipeline stage.
    /// </summary>
    public async IAsyncEnumerable<DiscoveredPlugin> StreamAll(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var (_, _, plugin) in cache.StreamAll<DiscoveredPlugin>(cancellationToken))
            yield return plugin;
    }

    /// <summary>
    /// Streams entries filtered by <paramref name="name"/> and optionally <paramref name="version"/>.
    /// When <paramref name="name"/> is empty all names are included.
    /// </summary>
    public async IAsyncEnumerable<DiscoveredPlugin> StreamAll(
        string name,
        PluginVersion? version,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var (group, key, plugin) in cache.StreamAll<DiscoveredPlugin>(cancellationToken))
        {
            if (!string.IsNullOrEmpty(name) && group != name) continue;
            if (version is not null && key != version.Value.Name) continue;

            yield return plugin;
        }
    }
}
