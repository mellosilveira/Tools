using MelloSilveiraTools.Infrastructure.Plugins.Models;
using System.Reflection;
using System.Runtime.Loader;

namespace MelloSilveiraTools.Infrastructure.Plugins;

public class PluginAssemblyProcessor(
    Dictionary<Type, IPluginTypeProcessor> typeProcessors,
    PluginCache cache)
{
    public PluginAssemblyInfo Load(PluginDescriptor descriptor)
        => cache.GetOrAdd(descriptor.Name, descriptor.Version, () =>
        {
            Assembly assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(descriptor.FullPath);
            Type[] processableTypes = [.. assembly.GetTypes().Where(t => typeProcessors.Keys.Any(processableType => processableType.IsAssignableFrom(t)) && !t.IsInterface && !t.IsAbstract)];
            return new PluginAssemblyInfo(descriptor, processableTypes);
        });

    public PluginTypeInfo GetTypes(PluginAssemblyInfo assemblyInfo)
        => cache.GetOrAdd(assemblyInfo.Descriptor.Name, assemblyInfo.Descriptor.Version, () => new(assemblyInfo));

    public PluginTypeInfo ProcessTypes(PluginAssemblyInfo assemblyInfo, PluginRegistrationContext context)
    {
        string name = assemblyInfo.Descriptor.Name;
        PluginVersion version = assemblyInfo.Descriptor.Version;
        PluginTypeInfo typeInfo = cache.GetOrAdd(name, version, () => new(assemblyInfo));

        foreach (Type type in assemblyInfo.ProcessableTypes)
        {
            typeProcessors[type].Process(type, context);

            typeInfo.MarkTypeLoaded(type);
            cache.UpdateState(name, version, typeInfo);
        }

        return typeInfo;
    }
}
