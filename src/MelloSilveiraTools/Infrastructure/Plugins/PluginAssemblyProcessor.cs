using MelloSilveiraTools.Infrastructure.Plugins.Models;
using System.Reflection;
using System.Runtime.Loader;

namespace MelloSilveiraTools.Infrastructure.Plugins;

/// <summary>
/// Loads plugin assemblies from disk, discovers their processable types and
/// dispatches type registration to the appropriate <see cref="IPluginTypeProcessor"/>.
/// Results are memoized in <see cref="PluginCache"/> keyed by plugin name and version.
/// </summary>
/// <param name="typeProcessors">Collection of type processors used to handle each <see cref="IPluginTypeProcessor.ProcessableType"/> discovered inside a plugin assembly.</param>
/// <param name="cache">Plugin cache used to memoize loaded and registered plugins by name and version.</param>
public class PluginAssemblyProcessor(
    IEnumerable<IPluginTypeProcessor> typeProcessors,
    PluginCache cache)
{
    private Dictionary<Type, IPluginTypeProcessor> _typeProcessorsByType = typeProcessors.ToDictionary(tp => tp.ProcessableType);

    /// <summary>
    /// Loads the assembly described by <paramref name="discovered"/> and returns the set of processable types it contains.
    /// </summary>
    public LoadedPlugin Load(DiscoveredPlugin discovered)
        => cache.GetOrAdd(discovered.Name, discovered.Version, () =>
        {
            Assembly assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(discovered.FullPath);
            Type[] processableTypes = [.. assembly.GetTypes().Where(t => _typeProcessorsByType.Keys.Any(processableType => processableType.IsAssignableFrom(t)) && !t.IsInterface && !t.IsAbstract)];
            return new LoadedPlugin(discovered, processableTypes);
        });

    /// <summary>
    /// Returns a <see cref="RegisteredPlugin"/> instance for <paramref name="loaded"/>, creating a cache entry when missing.
    /// </summary>
    public RegisteredPlugin GetInfo(LoadedPlugin loaded)
        => cache.GetOrAdd(loaded.Name, loaded.Version, () => new(loaded));

    /// <summary>
    /// Runs each processable type of <paramref name="loaded"/> through its matching <see cref="IPluginTypeProcessor"/>
    /// using the provided <paramref name="context"/> and records the progress in cache.
    /// </summary>
    public RegisteredPlugin ProcessTypes(LoadedPlugin loaded, PluginRegistrationContext context)
    {
        RegisteredPlugin registered = cache.GetOrAdd(loaded.Name, loaded.Version, () => new(loaded));

        foreach (Type type in loaded.ProcessableTypes)
        {
            _typeProcessorsByType[type].Process(type, context);

            registered.MarkTypeLoaded(type);
            cache.Update(loaded.Name, loaded.Version, registered);
        }

        return registered;
    }
}
