using MelloSilveiraTools.ExtensionMethods;
using MelloSilveiraTools.Infrastructure.Plugins;
using MelloSilveiraTools.Infrastructure.Plugins.Models;
using MelloSilveiraTools.Infrastructure.Plugins.Persistences;
using MelloSilveiraTools.Infrastructure.Providers;
using Microsoft.Extensions.DependencyInjection;

namespace MelloSilveiraTools.Infrastructure.Services.Plugins;

public static class PluginServiceExtensions
{
    public static PluginAssemblyInfo LoadAssembly(this PluginBaseInfo descriptor, PluginAssemblyProcessor assemblyProcessor)
        => assemblyProcessor.Load(descriptor);

    public static PluginInfo ProcessTypes(this PluginAssemblyInfo assemblyInfo, PluginAssemblyProcessor assemblyProcessor, PluginRegistrationContext context)
        => assemblyProcessor.ProcessTypes(assemblyInfo, context);

    public static PluginInfo GetInfo(this PluginAssemblyInfo assemblyInfo, PluginAssemblyProcessor assemblyProcessor)
        => assemblyProcessor.GetInfo(assemblyInfo);
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

    public IEnumerable<PluginInfo> GetPlugins(string pluginName, PluginVersion? version)
        => fileProcessor
            .Scan(pluginName, version)
            .Select(descriptor => descriptor
                .LoadAssembly(assemblyProcessor)
                .GetInfo(assemblyProcessor));

    private void LoadPlugins(PluginRegistrationContext context, string pluginName = "", PluginVersion? version = null)
        => fileProcessor
            .Scan(pluginName, version)
            .Foreach(descriptor => LoadPlugin(context, descriptor));

    private void LoadPlugin(PluginRegistrationContext context, PluginBaseInfo descriptor)
    {
        if (!cache.TryGetPluginState(descriptor.Name, descriptor.Version, out var state) || !state.IsFullyLoaded)
        {
            descriptor
                .LoadAssembly(assemblyProcessor)
                .ProcessTypes(assemblyProcessor, context);
        }

        fileProcessor.MoveToLoadedFolder(descriptor);
    }

    private void ReloadPlugins(PluginRegistrationContext context, bool forceLoad, string pluginName = "", PluginVersion? version = null)
        => fileProcessor
            .ScanLoaded(pluginName, version)
            .Foreach(descriptor =>
            {
                fileProcessor.MoveToMainFolder(descriptor);

                if (forceLoad)
                {
                    cache.Clear(pluginName, version);
                    LoadPlugin(context, descriptor);
                }
            });

    public void Clear() => cache.Clear();

    ///// <inheritdoc/>
    //private IReadOnlyList<PluginEntry<TPlugin>> DiscoverPlugins()
    //{
    //    var entries = new List<PluginEntry<TPlugin>>();
    //    foreach (PluginDescriptor descriptor in scanner.Scan())
    //    {
    //        PluginAssemblyInfo assemblyInfo = cache.GetOrAddAssemblyInfo(descriptor.Name, descriptor.Version, () => assemblyProcessor.LoadAndExtract(descriptor, typeProcessors.Keys));
    //        IReadOnlyList<TPlugin> plugins = cache.GetOrAddProcessedTypes(descriptor.Name, descriptor.Version, () => assemblyInfo.ProcessableTypes.Select(t => (TPlugin)Activator.CreateInstance(t)).ToList());
    //        PluginTypeInfo state = cache.GetOrCreateState(descriptor.Name, descriptor.Version);

    //        foreach (TPlugin plugin in plugins)
    //        {
    //            state.RegisterType(plugin.Name);
    //            entries.Add(new PluginEntry<TPlugin>(descriptor, plugin, state.IsFullyLoaded));
    //        }
    //    }

    //    return entries;
    //}

    ///// <inheritdoc/>
    //public IReadOnlyList<PluginEntry<TPlugin>> GetPlugins(string name = null, bool? loaded = null)
    //{
    //    var entries = DiscoverPlugins();

    //    if (name is not null)
    //        entries = entries.Where(e => e.Descriptor.Name == name).ToList();

    //    if (loaded is not null)
    //        entries = entries.Where(e => e.Loaded == loaded.Value).ToList();

    //    return entries;
    //}

    ///// <inheritdoc/>
    //public void ClearCache(CacheStage stage) => cache.Clear(stage);

    ///// <inheritdoc/>
    //public async Task PersistCacheAsync()
    //{
    //    var descriptors = cache.GetAllAssemblyInfos().ToDictionary(kv => kv.Key, kv => kv.Value.Descriptor);
    //    await persistence.SaveAsync(descriptors, cache.GetAllStates()).ConfigureAwait(false);
    //}

    ///// <inheritdoc/>
    //public async Task RestoreCacheAsync()
    //{
    //    var (descriptors, states) = await persistence.LoadAsync().ConfigureAwait(false);
    //    cache.RestoreFrom(descriptors, states);
    //}
}
