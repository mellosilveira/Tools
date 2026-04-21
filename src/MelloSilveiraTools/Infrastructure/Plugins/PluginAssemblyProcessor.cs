using MelloSilveiraTools.Infrastructure.Plugins.Models;
using System.Reflection;
using System.Runtime.Loader;

namespace MelloSilveiraTools.Infrastructure.Plugins;

public class PluginAssemblyProcessor(
    Dictionary<Type, IPluginTypeProcessor> typeProcessors,
    PluginCache cache)
{
    public LoadedPlugin Load(DiscoveredPlugin discovered)
        => cache.GetOrAdd(discovered.Name, discovered.Version, () =>
        {
            Assembly assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(discovered.FullPath);
            Type[] processableTypes = [.. assembly.GetTypes().Where(t => typeProcessors.Keys.Any(processableType => processableType.IsAssignableFrom(t)) && !t.IsInterface && !t.IsAbstract)];
            return new LoadedPlugin(discovered, processableTypes);
        });

    public RegisteredPlugin GetInfo(LoadedPlugin loaded)
        => cache.GetOrAdd(loaded.Name, loaded.Version, () => new(loaded));

    public RegisteredPlugin ProcessTypes(LoadedPlugin loaded, PluginRegistrationContext context)
    {
        RegisteredPlugin registered = cache.GetOrAdd(loaded.Name, loaded.Version, () => new(loaded));

        foreach (Type type in loaded.ProcessableTypes)
        {
            typeProcessors[type].Process(type, context);

            registered.MarkTypeLoaded(type);
            cache.Update(loaded.Name, loaded.Version, registered);
        }

        return registered;
    }
}
