namespace MelloSilveiraTools.Infrastructure.Plugins.Models;

/// <summary>
/// A <see cref="DiscoveredPlugin"/> whose assembly has been loaded into memory.
/// Adds the set of types eligible for processing.
/// </summary>
public record LoadedPlugin : DiscoveredPlugin
{
    public LoadedPlugin(DiscoveredPlugin discovered, Type[] processableTypes) : base(discovered)
    {
        ProcessableTypes = processableTypes;
    }

    /// <summary>Types found in the assembly that implement the plugin interface.</summary>
    public Type[] ProcessableTypes { get; }
}
