namespace MelloSilveiraTools.Infrastructure.Plugins.Models;

/// <summary>
/// Holds a loaded assembly and the types extracted from it that implement the plugin interface.
/// </summary>
public record PluginAssemblyInfo : PluginBaseInfo
{
    public PluginAssemblyInfo(PluginBaseInfo baseInfo, Type[] processableTypes) : base(baseInfo)
    {
        ProcessableTypes = processableTypes;
    }

    /// <summary>
    /// Types found in the assembly that implement the plugin interface.
    /// </summary>
    public Type[] ProcessableTypes { get; }
}
