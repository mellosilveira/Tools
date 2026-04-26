namespace MelloSilveiraTools.Plugins.Infrastructure;

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
