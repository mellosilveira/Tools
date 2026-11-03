using MelloSilveiraTools.Core.Providers.Dynamics;
using MelloSilveiraTools.Plugins.Infrastructure.Models;
using MelloSilveiraTools.Plugins.Infrastructure.Persistences;
using Microsoft.Extensions.DependencyInjection;
using System.Runtime.CompilerServices;

namespace MelloSilveiraTools.Plugins.Infrastructure.Services;

/// <summary>
/// Extension methods that streamline plugin processing pipelines on top of <see cref="PluginAssemblyProcessor"/>.
/// </summary>
public static class PluginServiceExtensions
{
    /// <summary>
    /// Asynchronously loads the assembly of a discovered plugin using the provided processor.
    /// </summary>
    /// <param name="discovered">Plugin found in storage by the file processor.</param>
    /// <param name="assemblyProcessor">Processor responsible for loading plugin assemblies.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The loaded plugin, with its assembly available for inspection.</returns>
    public static async Task<LoadedPlugin> LoadAssemblyAsync(this DiscoveredPlugin discovered, PluginAssemblyProcessor assemblyProcessor, CancellationToken cancellationToken = default)
    {
        return await assemblyProcessor.LoadAsync(discovered, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Processes the types of a loaded plugin and registers its services inside the provided registration context.
    /// </summary>
    /// <param name="loaded">Plugin whose assembly has been loaded.</param>
    /// <param name="assemblyProcessor">Processor responsible for scanning and registering plugin types.</param>
    /// <param name="context">Registration context that determines how services are wired up.</param>
    /// <returns>The plugin after its types have been processed and registered.</returns>
    public static RegisteredPlugin RegisterTypes(this LoadedPlugin loaded, PluginAssemblyProcessor assemblyProcessor, PluginRegistrationContext context)
    {
        return assemblyProcessor.ProcessTypes(loaded, context);
    }

    /// <summary>
    /// Retrieves registry information for a loaded plugin without registering its services.
    /// </summary>
    /// <param name="loaded">Plugin whose assembly has been loaded.</param>
    /// <param name="assemblyProcessor">Processor responsible for extracting plugin metadata.</param>
    /// <returns>The registry information for the plugin.</returns>
    public static RegisteredPlugin GetRegistry(this LoadedPlugin loaded, PluginAssemblyProcessor assemblyProcessor)
    {
        return assemblyProcessor.GetInfo(loaded);
    }
}

/// <inheritdoc cref="IPluginService"/>
/// <param name="persistence">Persistence used to save and restore the plugin cache.</param>
/// <param name="dynamicServiceProvider">Service provider used to register plugin services at runtime.</param>
/// <param name="fileProcessor">Processor that scans storage for plugin assemblies.</param>
/// <param name="assemblyProcessor">Processor that loads and inspects plugin assemblies.</param>
/// <param name="cache">Cache that keeps track of plugin registration state.</param>
public class PluginService(
    IPluginCachePersistence persistence,
    IDynamicServiceProvider dynamicServiceProvider,
    PluginFileProcessor fileProcessor,
    PluginAssemblyProcessor assemblyProcessor,
    PluginCache cache)
    : IPluginService
{
    /// <inheritdoc/>
    public async Task LoadPluginsOnStartupAsync(IServiceCollection services, string? name = null, PluginVersion? version = null, CancellationToken cancellationToken = default)
    {
        await LoadPluginsAsync(PluginRegistrationContext.ForStartup(services), name, version, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task LoadPluginsOnRuntimeAsync(string? name = null, PluginVersion? version = null, CancellationToken cancellationToken = default)
    {
        await LoadPluginsAsync(PluginRegistrationContext.ForRuntime(dynamicServiceProvider), name, version, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task ReloadPluginsOnStartupAsync(
        IServiceCollection services,
        bool forceLoad,
        string? name = null,
        PluginVersion? version = null,
        CancellationToken cancellationToken = default)
    {
        await ReloadPluginsAsync(PluginRegistrationContext.ForStartup(services), forceLoad, name, version, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task ReloadPluginsOnRuntimeAsync(
        bool forceLoad,
        string? name = null,
        PluginVersion? version = null,
        CancellationToken cancellationToken = default)
    {
        await ReloadPluginsAsync(PluginRegistrationContext.ForRuntime(dynamicServiceProvider), forceLoad, name, version, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<RegisteredPlugin> GetPluginsAsync(
        string? name = null,
        PluginVersion? version = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (DiscoveredPlugin discovered in fileProcessor.ScanLoadedAsync(name, version, cancellationToken).ConfigureAwait(false))
        {
            LoadedPlugin loaded = await discovered.LoadAssemblyAsync(assemblyProcessor, cancellationToken).ConfigureAwait(false);
            yield return loaded.GetRegistry(assemblyProcessor);
        }

        await foreach (DiscoveredPlugin discovered in fileProcessor.ScanAsync(name, version, cancellationToken).ConfigureAwait(false))
        {
            LoadedPlugin loaded = await discovered.LoadAssemblyAsync(assemblyProcessor, cancellationToken).ConfigureAwait(false);
            yield return loaded.GetRegistry(assemblyProcessor);
        }
    }

    /// <inheritdoc/>
    public Task ClearAsync() => cache.ClearAsync();

    /// <inheritdoc/>
    public Task PersistCacheAsync(string? name = null, PluginVersion? version = null, CancellationToken cancellationToken = default)
    {
        return persistence.SaveAsync(cache.Stream(name, version, cancellationToken), cancellationToken);
    }

    /// <inheritdoc/>
    public async Task RestoreCacheAsync(string? name = null, PluginVersion? version = null, CancellationToken cancellationToken = default)
    {
        await foreach (PluginCacheEntry entry in persistence.LoadAsync(name, version, cancellationToken).ConfigureAwait(false))
        {
            cache.Update(entry.Name, PluginVersion.Parse(entry.Version), entry.State);
        }
    }

    private async Task LoadPluginsAsync(
        PluginRegistrationContext context,
        string? name = null,
        PluginVersion? version = null,
        CancellationToken cancellationToken = default)
    {
        await foreach (DiscoveredPlugin discovered in fileProcessor.ScanAsync(name, version, cancellationToken).ConfigureAwait(false))
        {
            await LoadPluginAsync(context, discovered, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task LoadPluginAsync(PluginRegistrationContext context, DiscoveredPlugin discovered, CancellationToken cancellationToken = default)
    {
        if (!cache.TryGet<RegisteredPlugin>(discovered.Name, discovered.Version, out RegisteredPlugin? registered) || registered is null || !registered.IsFullyLoaded)
        {
            LoadedPlugin loaded = await discovered.LoadAssemblyAsync(assemblyProcessor, cancellationToken).ConfigureAwait(false);
            loaded.RegisterTypes(assemblyProcessor, context);
        }

        await fileProcessor.MoveToLoadedFolderAsync(discovered, cancellationToken).ConfigureAwait(false);
    }

    private async Task ReloadPluginsAsync(
        PluginRegistrationContext context,
        bool forceLoad,
        string? name = null,
        PluginVersion? version = null,
        CancellationToken cancellationToken = default)
    {
        await foreach (DiscoveredPlugin discovered in fileProcessor.ScanLoadedAsync(name, version, cancellationToken).ConfigureAwait(false))
        {
            DiscoveredPlugin inMain = await fileProcessor.MoveToMainFolderAsync(discovered, cancellationToken).ConfigureAwait(false);

            if (forceLoad)
            {
                cache.Clear(discovered.Name, discovered.Version);
                await LoadPluginAsync(context, inMain, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
