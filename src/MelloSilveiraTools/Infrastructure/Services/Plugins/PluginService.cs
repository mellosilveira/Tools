using System.Runtime.CompilerServices;
using MelloSilveiraTools.ExtensionMethods;
using MelloSilveiraTools.Infrastructure.Plugins;
using MelloSilveiraTools.Infrastructure.Plugins.Models;
using MelloSilveiraTools.Infrastructure.Plugins.Persistences;
using MelloSilveiraTools.Infrastructure.Providers;
using Microsoft.Extensions.DependencyInjection;

namespace MelloSilveiraTools.Infrastructure.Services.Plugins;

public static class PluginServiceExtensions
{
    public static LoadedPlugin LoadAssembly(this DiscoveredPlugin discovered, PluginAssemblyProcessor assemblyProcessor)
        => assemblyProcessor.Load(discovered);

    public static RegisteredPlugin ProcessTypes(this LoadedPlugin loaded, PluginAssemblyProcessor assemblyProcessor, PluginRegistrationContext context)
        => assemblyProcessor.ProcessTypes(loaded, context);

    public static RegisteredPlugin GetInfo(this LoadedPlugin loaded, PluginAssemblyProcessor assemblyProcessor)
        => assemblyProcessor.GetInfo(loaded);
}

/// <inheritdoc cref="IPluginService"/>
public class PluginService(
    PluginFileProcessor fileProcessor,
    PluginAssemblyProcessor assemblyProcessor,
    PluginCache cache,
    IPluginCachePersistence persistence,
    IServiceCollection services,
    IDynamicServiceProvider dynamicServiceProvider)
    : IPluginService
{
    public void LoadPluginsOnStartup(string pluginName = "", PluginVersion? version = null)
        => LoadPlugins(PluginRegistrationContext.ForStartup(services), pluginName, version);

    public void LoadPluginsOnRuntime(string pluginName = "", PluginVersion? version = null)
        => LoadPlugins(PluginRegistrationContext.ForRuntime(dynamicServiceProvider), pluginName, version);

    public void ReloadPluginsOnStartup(bool forceLoad, string pluginName = "", PluginVersion? version = null)
        => ReloadPlugins(PluginRegistrationContext.ForStartup(services), forceLoad, pluginName, version);

    public void ReloadPluginsOnRuntime(bool forceLoad, string pluginName = "", PluginVersion? version = null)
        => ReloadPlugins(PluginRegistrationContext.ForRuntime(dynamicServiceProvider), forceLoad, pluginName, version);

    public IEnumerable<RegisteredPlugin> GetPlugins(string pluginName, PluginVersion? version)
        => fileProcessor
            .Scan(pluginName, version)
            .Select(discovered => discovered
                .LoadAssembly(assemblyProcessor)
                .GetInfo(assemblyProcessor));

    public void Clear() => cache.Clear();

    /// <inheritdoc/>
    public Task PersistCacheAsync(string name = "", PluginVersion? version = null, CancellationToken cancellationToken = default)
        => persistence.SaveAsync(
            cache
                .StreamAll(name, version, cancellationToken)
                .Select(plugin => new PluginCacheEntry(plugin.Name, plugin.Version.Name, plugin)), 
            cancellationToken);

    /// <inheritdoc/>
    public async Task RestoreCacheAsync(string name = "", PluginVersion? version = null, CancellationToken cancellationToken = default)
    {
        await foreach (PluginCacheEntry entry in persistence.LoadAsync(name, version, cancellationToken))
        {
            cache.Update(entry.Name, PluginVersion.Parse(entry.Version), entry.State);
        }
    }

    private void LoadPlugins(PluginRegistrationContext context, string pluginName = "", PluginVersion? version = null)
        => fileProcessor
            .Scan(pluginName, version)
            .Foreach(discovered => LoadPlugin(context, discovered));

    private void LoadPlugin(PluginRegistrationContext context, DiscoveredPlugin discovered)
    {
        if (!cache.TryGet<RegisteredPlugin>(discovered.Name, discovered.Version, out var registered) || registered is null || !registered.IsFullyLoaded)
        {
            discovered
                .LoadAssembly(assemblyProcessor)
                .ProcessTypes(assemblyProcessor, context);
        }

        fileProcessor.MoveToLoadedFolder(discovered);
    }

    private void ReloadPlugins(PluginRegistrationContext context, bool forceLoad, string pluginName = "", PluginVersion? version = null)
        => fileProcessor
            .ScanLoaded(pluginName, version)
            .Foreach(discovered =>
            {
                fileProcessor.MoveToMainFolder(discovered);

                if (forceLoad)
                {
                    cache.Clear(pluginName, version);
                    LoadPlugin(context, discovered);
                }
            });
}
