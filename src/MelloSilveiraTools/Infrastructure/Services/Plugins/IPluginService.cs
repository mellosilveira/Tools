using MelloSilveiraTools.Infrastructure.Plugins.Models;

namespace MelloSilveiraTools.Infrastructure.Services.Plugins;

/// <summary>
/// High-level orchestrator for plugin discovery, loading, reload, and cache management.
/// </summary>
public interface IPluginService
{
    void LoadPluginsOnStartup(string pluginName = "", PluginVersion? version = null);

    void LoadPluginsOnRuntime(string pluginName = "", PluginVersion? version = null);

    void ReloadPluginsOnStartup(bool forceLoad, string pluginName = "", PluginVersion? version = null);

    void ReloadPluginsOnRuntime(bool forceLoad, string pluginName = "", PluginVersion? version = null);

    void Clear();

    IEnumerable<RegisteredPlugin> GetPlugins(string pluginName, PluginVersion? version);

    Task PersistCacheAsync(string name = "", PluginVersion? version = null, CancellationToken cancellationToken = default);

    Task RestoreCacheAsync(string name = "", PluginVersion? version = null, CancellationToken cancellationToken = default);
}
