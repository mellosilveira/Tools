using MelloSilveiraTools.Plugins.Infrastructure.Models;
using System.Reflection;
using System.Runtime.Loader;

namespace MelloSilveiraTools.Plugins.Infrastructure;

/// <summary>
/// The system responsible for dynamically loading external modules ("plugins") into our application.
/// It acts like a gatekeeper that unzips or inspects new functionalities, organizing them so our software can adopt these new capabilities on the fly without requiring a full system restart.
/// </summary>
/// <param name="typeProcessors">Collection of type processors used to handle each <see cref="IPluginTypeProcessor.ProcessableType"/> discovered inside a plugin assembly.</param>
/// <param name="cache">Plugin cache used to memoize loaded and registered plugins by name and version.</param>
public class PluginAssemblyProcessor(
    IEnumerable<IPluginTypeProcessor> typeProcessors,
    PluginCache cache)
{
    private readonly IPluginTypeProcessor[] _typeProcessors = [.. typeProcessors];

    /// <summary>
    /// Loads the assembly described by <paramref name="discovered"/> and returns the set of processable types it contains.
    /// </summary>
    public LoadedPlugin Load(DiscoveredPlugin discovered) => cache.GetOrAdd(
        discovered.Name,
        discovered.Version,
        () =>
        {
            // 1. Created an isolated context and collectible for plugin.
            AssemblyLoadContext pluginContext = new($"PluginContext_{discovered.Name}", isCollectible: true);

            // 2. Load the assembly in this new isolated context, instead of the default.
            Assembly assembly = pluginContext.LoadFromAssemblyPath(discovered.FullPath);

            Type[] processableTypes = [.. assembly.GetTypes().Where(t =>
                !t.IsInterface &&
                !t.IsAbstract &&
                Array.Exists(_typeProcessors, tp => tp.ProcessableType.IsAssignableFrom(t)))];

            return new LoadedPlugin(discovered, processableTypes, pluginContext);
        });

    /// <summary>
    /// Returns a <see cref="RegisteredPlugin"/> instance for <paramref name="loaded"/>, creating a cache entry when missing.
    /// </summary>
    public RegisteredPlugin GetInfo(LoadedPlugin loaded) => cache.GetOrAdd(loaded.Name, loaded.Version, () => new(loaded));

    /// <summary>
    /// Runs each processable type of <paramref name="loaded"/> through its matching <see cref="IPluginTypeProcessor"/>
    /// using the provided <paramref name="context"/> and records the progress in cache.
    /// </summary>
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

