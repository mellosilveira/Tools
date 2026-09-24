using System.Runtime.Loader;

namespace MelloSilveiraTools.Plugins.Infrastructure.Models;

/// <summary>
/// A <see cref="DiscoveredPlugin"/> whose assembly has been loaded into memory.
/// Adds the set of types eligible for processing.
/// </summary>
public record LoadedPlugin : DiscoveredPlugin
{
    private readonly AssemblyLoadContext _pluginContext;

    /// <summary>
    /// Initializes a new <see cref="LoadedPlugin"/> by wrapping a <paramref name="discovered"/> entry
    /// with the set of <paramref name="processableTypes"/> found in the loaded assembly.
    /// </summary>
    public LoadedPlugin(DiscoveredPlugin discovered, Type[] processableTypes, AssemblyLoadContext pluginContext) : base(discovered)
    {
        ProcessableTypes = processableTypes;
        _pluginContext = pluginContext;
    }

    /// <summary>Types found in the assembly that implement the plugin interface.</summary>
    public Type[] ProcessableTypes { get; }

    /// <summary>
    /// This must be called only when application update the plugin.
    /// </summary>
    public void UnloadAssembly() => _pluginContext.Unload();
}
