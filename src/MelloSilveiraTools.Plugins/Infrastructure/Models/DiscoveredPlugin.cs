namespace MelloSilveiraTools.Plugins.Infrastructure.Models;

/// <summary>
/// Metadata about a plugin DLL discovered on disk.
/// Parsed from the filename pattern: {name}.v{major}.{minor}.{patch}.dll.
/// </summary>
public record DiscoveredPlugin
{
    /// <summary>
    /// Parameterless constructor required for deserialization.
    /// </summary>
    public DiscoveredPlugin() { }

    /// <summary>
    /// Initializes a new <see cref="DiscoveredPlugin"/> with the given metadata.
    /// </summary>
    public DiscoveredPlugin(string name, PluginVersion version, string fullPath, DateTimeOffset discoveredAt)
    {
        Name = name;
        Version = version;
        FullPath = fullPath;
        DiscoveredAt = discoveredAt;
    }

    /// <summary>
    /// Copy constructor used by derived records to carry over the discovery metadata.
    /// </summary>
    protected DiscoveredPlugin(DiscoveredPlugin other)
    {
        Name = other.Name;
        Version = other.Version;
        FullPath = other.FullPath;
        DiscoveredAt = other.DiscoveredAt;
    }

    /// <summary>Plugin name without version (e.g., "SoftTissue.Plugins").</summary>
    public string Name { get; }

    /// <summary>Parsed semantic version.</summary>
    public PluginVersion Version { get; }

    /// <summary>Absolute path to the DLL file.</summary>
    public string FullPath { get; }

    /// <summary>Timestamp when the plugin was first discovered.</summary>
    public DateTimeOffset DiscoveredAt { get; }
}
