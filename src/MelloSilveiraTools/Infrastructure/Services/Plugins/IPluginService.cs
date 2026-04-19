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

    IEnumerable<PluginInfo> GetPlugins(string pluginName, PluginVersion? version);

    ///// <summary>
    ///// Clears the cache from the specified stage onward.
    ///// </summary>
    //void ClearCache(CacheStage stage);

    ///// <summary>
    ///// Persists the current cache to non-volatile storage.
    ///// </summary>
    //Task PersistCacheAsync();

    ///// <summary>
    ///// Restores cache from non-volatile storage.
    ///// </summary>
    //Task RestoreCacheAsync();
}
