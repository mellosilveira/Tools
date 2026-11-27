using System.Runtime.Loader;

namespace MelloSilveiraTools.Plugins.Infrastructure.Models;

/// <summary>
/// A <see cref="DiscoveredPlugin"/> whose assembly has been loaded into memory.
/// Adds the set of types eligible for processing.
/// </summary>
public record LoadedPlugin : DiscoveredPlugin
{
    private readonly AssemblyLoadContext? _pluginContext;
    private readonly string? _shadowCopyDirectory;

    /// <summary>
    /// Parameterless constructor required for deserialization.
    /// </summary>
    public LoadedPlugin() { }

    /// <summary>
    /// Initializes a new <see cref="LoadedPlugin"/> by wrapping a <paramref name="discovered"/> entry
    /// with the set of <paramref name="processableTypes"/> found in the loaded assembly.
    /// </summary>
    /// <param name="discovered">Discovered plugin metadata.</param>
    /// <param name="processableTypes">Types discovered in the assembly matching type processors.</param>
    /// <param name="pluginContext">The assembly load context hosting the plugin.</param>
    /// <param name="shadowCopyDirectory">Optional path to the temporary shadow copy directory on disk.</param>
    public LoadedPlugin(
        DiscoveredPlugin discovered,
        Type[] processableTypes,
        AssemblyLoadContext pluginContext,
        string? shadowCopyDirectory = null) : base(discovered)
    {
        ProcessableTypes = processableTypes;
        _pluginContext = pluginContext;
        _shadowCopyDirectory = shadowCopyDirectory;
    }

    /// <summary>Types found in the assembly that implement the plugin interface.</summary>
    public Type[] ProcessableTypes { get; init; } = [];

    /// <summary>
    /// Unloads the assembly load context and attempts cleanup of temporary shadow copy artifacts.
    /// This must be called only when the application updates or decommissions the plugin.
    /// </summary>
    public void UnloadAssembly()
    {
        _pluginContext?.Unload();

        if (!string.IsNullOrEmpty(_shadowCopyDirectory) && Directory.Exists(_shadowCopyDirectory))
        {
            try
            {
                Directory.Delete(_shadowCopyDirectory, recursive: true);
            }
            catch
            {
                // File handle might still be released asynchronously by the runtime GC;
                // subsequent orchestrator sweeps or process exit will purge leftover shadow folders.
            }
        }
    }
}
