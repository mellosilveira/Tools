namespace MelloSilveiraTools.Infrastructure.Plugins;

/// <summary>
/// Represents a discoverable plugin loaded dynamically at runtime.
/// </summary>
public interface IPlugin
{
    /// <summary>
    /// Unique name identifying this plugin.
    /// </summary>
    string Name { get; }
}
