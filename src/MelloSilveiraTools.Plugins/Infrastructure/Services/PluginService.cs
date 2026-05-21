using MelloSilveiraTools.Core.ExtensionMethods;
using MelloSilveiraTools.Core.Providers.Dynamics;
using MelloSilveiraTools.Plugins.Infrastructure.Models;
using MelloSilveiraTools.Plugins.Infrastructure.Persistences;
using Microsoft.Extensions.DependencyInjection;

namespace MelloSilveiraTools.Plugins.Infrastructure.Services;

/// <summary>
/// Extension methods that streamline plugin processing pipelines on top of <see cref="PluginAssemblyProcessor"/>.
/// </summary>
public static class PluginServiceExtensions
{
    /// <summary>
    /// Loads the assembly of a discovered plugin using the provided processor.
    /// </summary>
    /// <param name="discovered">Plugin found on disk by the file processor.</param>
    /// <param name="assemblyProcessor">Processor responsible for loading plugin assemblies.</param>
    /// <returns>The loaded plugin, with its assembly available for inspection.</returns>
    public static LoadedPlugin LoadAssembly(this DiscoveredPlugin discovered, PluginAssemblyProcessor assemblyProcessor)
        => assemblyProcessor.Load(discovered);

    /// <summary>
    /// Processes the types of a loaded plugin and registers its services inside the provided registration context.
    /// </summary>
    /// <param name="loaded">Plugin whose assembly has been loaded.</param>
    /// <param name="assemblyProcessor">Processor responsible for scanning and registering plugin types.</param>
    /// <param name="context">Registration context that determines how services are wired up.</param>
    /// <returns>The plugin after its types have been processed and registered.</returns>
    public static RegisteredPlugin RegisterTypes(this LoadedPlugin loaded, PluginAssemblyProcessor assemblyProcessor, PluginRegistrationContext context)
        => assemblyProcessor.ProcessTypes(loaded, context);

    /// <summary>
    /// Retrieves registry information for a loaded plugin without registering its services.
    /// </summary>
    /// <param name="loaded">Plugin whose assembly has been loaded.</param>
    /// <param name="assemblyProcessor">Processor responsible for extracting plugin metadata.</param>
    /// <returns>The registry information for the plugin.</returns>
    public static RegisteredPlugin GetRegistry(this LoadedPlugin loaded, PluginAssemblyProcessor assemblyProcessor)
        => assemblyProcessor.GetInfo(loaded);
}

/// <inheritdoc cref="IPluginService"/>
/// <param name="fileProcessor">Processor that scans the file system for plugin assemblies.</param>
/// <param name="assemblyProcessor">Processor that loads and inspects plugin assemblies.</param>
/// <param name="cache">Cache that keeps track of plugin registration state.</param>
/// <param name="persistence">Persistence used to save and restore the plugin cache.</param>
/// <param name="services">Root service collection used to register plugin services at startup.</param>
/// <param name="dynamicServiceProvider">Service provider used to register plugin services at runtime.</param>
public class PluginService(
    PluginFileProcessor fileProcessor,
    PluginAssemblyProcessor assemblyProcessor,
    PluginCache cache,
    IPluginCachePersistence persistence,
    IServiceCollection services,
    IDynamicServiceProvider dynamicServiceProvider)
    : IPluginService
{
    /// <inheritdoc/>
    public void LoadPluginsOnStartup(string? name = null, PluginVersion? version = null)
        => LoadPlugins(PluginRegistrationContext.ForStartup(services), name, version);

    /// <inheritdoc/>
    public void LoadPluginsOnRuntime(string? name = null, PluginVersion? version = null)
        => LoadPlugins(PluginRegistrationContext.ForRuntime(dynamicServiceProvider), name, version);

    /// <inheritdoc/>
    public void ReloadPluginsOnStartup(bool forceLoad, string? name = null, PluginVersion? version = null)
        => ReloadPlugins(PluginRegistrationContext.ForStartup(services), forceLoad, name, version);

    /// <inheritdoc/>
    public void ReloadPluginsOnRuntime(bool forceLoad, string? name = null, PluginVersion? version = null)
        => ReloadPlugins(PluginRegistrationContext.ForRuntime(dynamicServiceProvider), forceLoad, name, version);

    /// <inheritdoc/>
    public IEnumerable<RegisteredPlugin> GetPlugins(string? name, PluginVersion? version)
        => fileProcessor
            .Scan(name, version)
            .Select(discovered => discovered
                .LoadAssembly(assemblyProcessor)
                .GetRegistry(assemblyProcessor));

    /// <inheritdoc/>
    public void Clear() => cache.Clear();

    /// <inheritdoc/>
    public Task PersistCacheAsync(string? name = null, PluginVersion? version = null, CancellationToken cancellationToken = default)
        => persistence.SaveAsync(
            cache.Stream(name, version, cancellationToken),
            cancellationToken);

    /// <inheritdoc/>
    public async Task RestoreCacheAsync(string? name = null, PluginVersion? version = null, CancellationToken cancellationToken = default)
    {
        await foreach (PluginCacheEntry entry in persistence.LoadAsync(name, version, cancellationToken))
        {
            cache.Update(entry.Name, PluginVersion.Parse(entry.Version), entry.State);
        }
    }

    private void LoadPlugins(PluginRegistrationContext context, string? name = null, PluginVersion? version = null)
        => fileProcessor
            .Scan(name, version)
            .Foreach(discovered => LoadPlugin(context, discovered));

    private void LoadPlugin(PluginRegistrationContext context, DiscoveredPlugin discovered)
    {
        if (!cache.TryGet<RegisteredPlugin>(discovered.Name, discovered.Version, out var registered) || registered is null || !registered.IsFullyLoaded)
        {
            discovered
                .LoadAssembly(assemblyProcessor)
                .RegisterTypes(assemblyProcessor, context);
        }

        fileProcessor.MoveToLoadedFolder(discovered);
    }

    /// <summary>
    /// Iterates over the plugins matched by the supplied filter that are currently in the loaded folder,
    /// moves each one back to the main plugin folder, and — when <paramref name="forceLoad"/> is
    /// <see langword="true"/> — evicts only that specific (name, version) entry from the cache and
    /// reloads it through the registration pipeline.
    /// </summary>
    /// <remarks>
    /// Earlier revisions of this method evicted the cache using the original <c>name</c>/<c>version</c>
    /// filter on every iteration, which meant a wildcard reload erased the entire cache repeatedly. The
    /// per-iteration eviction now targets only the discovered plugin currently being reloaded.
    /// </remarks>
    private void ReloadPlugins(PluginRegistrationContext context, bool forceLoad, string? name = null, PluginVersion? version = null)
        => fileProcessor
            .ScanLoaded(name, version)
            .Foreach(discovered =>
            {
                fileProcessor.MoveToMainFolder(discovered);

                if (forceLoad)
                {
                    cache.Clear(discovered.Name, discovered.Version);
                    LoadPlugin(context, discovered);
                }
            });
}
