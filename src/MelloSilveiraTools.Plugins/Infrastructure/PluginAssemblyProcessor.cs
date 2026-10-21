using MelloSilveiraTools.Core.Managers.File.Providers;
using MelloSilveiraTools.Plugins.Infrastructure.Models;
using System.Reflection;
using System.Runtime.Loader;

namespace MelloSilveiraTools.Plugins.Infrastructure;

/// <summary>
/// The system responsible for dynamically loading external modules ("plugins") into our application.
/// It acts like a gatekeeper that inspects new functionalities, organizing them so our software can adopt these new capabilities on the fly without requiring a full system restart.
/// </summary>
/// <param name="typeProcessors">Collection of type processors used to handle each <see cref="IPluginTypeProcessor.ProcessableType"/> discovered inside a plugin assembly.</param>
/// <param name="fileStorageService">Storage service used to open and stream plugin assemblies into memory.</param>
/// <param name="cache">Plugin cache used to memoize loaded and registered plugins by name and version.</param>
public class PluginAssemblyProcessor(
    IEnumerable<IPluginTypeProcessor> typeProcessors,
    IFileStorageService fileStorageService,
    PluginCache cache)
{
    private readonly IPluginTypeProcessor[] _typeProcessors = [.. typeProcessors];

    /// <summary>
    /// Asynchronously loads the assembly described by <paramref name="discovered"/> and returns the set of processable types it contains.
    /// Uses <see cref="AssemblyLoadContext.LoadFromStream(Stream, Stream?)"/> to eliminate file locking on host operating systems.
    /// </summary>
    /// <param name="discovered">Discovered plugin metadata holding the assembly location.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A loaded plugin representation holding the collectible assembly load context and discovered processable types.</returns>
    public Task<LoadedPlugin> LoadAsync(DiscoveredPlugin discovered, CancellationToken cancellationToken = default)
    {
        return cache.GetOrAddAsync(
            discovered.Name,
            discovered.Version,
            async () =>
            {
                AssemblyLoadContext pluginContext = new($"PluginContext_{discovered.Name}", isCollectible: true);

                await using Stream assemblyStream = await fileStorageService.OpenReadAsync(discovered.FullPath, cancellationToken).ConfigureAwait(false);

                Stream streamToLoad = assemblyStream;
                MemoryStream? memoryStream = null;
                if (!assemblyStream.CanSeek)
                {
                    memoryStream = new MemoryStream();
                    await assemblyStream.CopyToAsync(memoryStream, cancellationToken).ConfigureAwait(false);
                    memoryStream.Position = 0;
                    streamToLoad = memoryStream;
                }

                Assembly assembly;
                try
                {
                    assembly = pluginContext.LoadFromStream(streamToLoad);
                }
                finally
                {
                    if (memoryStream is not null)
                    {
                        await memoryStream.DisposeAsync().ConfigureAwait(false);
                    }
                }

                Type[] processableTypes = [.. assembly.GetTypes().Where(t =>
                    !t.IsInterface &&
                    !t.IsAbstract &&
                    Array.Exists(_typeProcessors, tp => tp.ProcessableType.IsAssignableFrom(t)))];

                return new LoadedPlugin(discovered, processableTypes, pluginContext);
            });
    }

    /// <summary>
    /// Returns a <see cref="RegisteredPlugin"/> instance for <paramref name="loaded"/>, creating a cache entry when missing.
    /// </summary>
    /// <param name="loaded">The loaded plugin instance.</param>
    /// <returns>A registered plugin metadata entry.</returns>
    public RegisteredPlugin GetInfo(LoadedPlugin loaded)
    {
        return cache.GetOrAdd(loaded.Name, loaded.Version, () => new(loaded));
    }

    /// <summary>
    /// Runs each processable type of <paramref name="loaded"/> through its matching <see cref="IPluginTypeProcessor"/>
    /// using the provided <paramref name="context"/> and records the progress in cache.
    /// </summary>
    /// <param name="loaded">The loaded plugin instance.</param>
    /// <param name="context">The registration context for startup or runtime DI.</param>
    /// <returns>The registered plugin metadata entry.</returns>
    public RegisteredPlugin ProcessTypes(LoadedPlugin loaded, PluginRegistrationContext context)
    {
        RegisteredPlugin registered = cache.GetOrAdd(loaded.Name, loaded.Version, () => new(loaded));

        foreach (Type type in loaded.ProcessableTypes)
        {
            Array.Find(_typeProcessors, tp => tp.ProcessableType.IsAssignableFrom(type))!.Process(type, context);
            registered.MarkTypeLoaded(type);
            cache.Update(loaded.Name, loaded.Version, registered);
        }

        return registered;
    }
}
