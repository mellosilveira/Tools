using MelloSilveiraTools.Infrastructure.Plugins.Models;
using System.Reflection;
using System.Runtime.Loader;

namespace MelloSilveiraTools.Infrastructure.Plugins;

public class PluginAssemblyProcessor(
    Dictionary<Type, IPluginTypeProcessor> typeProcessors,
    PluginCache cache)
{
    public PluginAssemblyInfo Load(PluginBaseInfo descriptor)
        => cache.GetOrAdd(descriptor.Name, descriptor.Version, () =>
        {
            Assembly assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(descriptor.FullPath);
            Type[] processableTypes = [.. assembly.GetTypes().Where(t => typeProcessors.Keys.Any(processableType => processableType.IsAssignableFrom(t)) && !t.IsInterface && !t.IsAbstract)];
            return new PluginAssemblyInfo(descriptor, processableTypes);
        });

    public PluginInfo GetInfo(PluginAssemblyInfo assemblyInfo)
        => cache.GetOrAdd(assemblyInfo.Descriptor.Name, assemblyInfo.Descriptor.Version, () => new(assemblyInfo));

    public PluginInfo ProcessTypes(PluginAssemblyInfo assemblyInfo, PluginRegistrationContext context)
    {
        string name = assemblyInfo.Descriptor.Name;
        PluginVersion version = assemblyInfo.Descriptor.Version;
        PluginInfo typeInfo = cache.GetOrAdd(name, version, () => new(assemblyInfo));

        foreach (Type type in assemblyInfo.ProcessableTypes)
        {
            typeProcessors[type].Process(type, context);

            typeInfo.MarkTypeLoaded(type);
            cache.UpdateState(name, version, typeInfo);
        }

        return typeInfo;
    }
}
