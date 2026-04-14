namespace MelloSilveiraTools.Infrastructure.Plugins;

/// <summary>
/// Configuration settings for the plugin system.
/// </summary>
public record PluginSettings
{
    /// <summary>
    /// Path to the directory where plugin DLLs are stored.
    /// </summary>
    public string Directory { get; init; }
}
