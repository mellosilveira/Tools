namespace MelloSilveiraTools.Infrastructure.Plugins.Models;

/// <summary>
/// Holds a loaded assembly and the types extracted from it that implement the plugin interface.
/// </summary>
/// <param name="Descriptor">The plugin descriptor this assembly was loaded from.</param>
/// <param name="ProcessableTypes">Types found in the assembly that implement the plugin interface.</param>
public record PluginAssemblyInfo(PluginDescriptor Descriptor, Type[] ProcessableTypes);
