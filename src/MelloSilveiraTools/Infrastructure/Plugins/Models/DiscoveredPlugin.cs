namespace MelloSilveiraTools.Infrastructure.Plugins.Models;

/// <summary>
/// Metadata about a plugin DLL discovered on disk.
/// Parsed from the filename pattern: {name}.v{major}.{minor}.{patch}.dll.
/// </summary>
public record DiscoveredPlugin
{
    public DiscoveredPlugin() { }

    public DiscoveredPlugin(string name, PluginVersion version, string fullPath, DateTimeOffset discoveredAt)
    {
        Name = name;
        Version = version;
        FullPath = fullPath;
        DiscoveredAt = discoveredAt;
    }

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
