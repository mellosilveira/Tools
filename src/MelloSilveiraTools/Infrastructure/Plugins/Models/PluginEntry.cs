namespace MelloSilveiraTools.Infrastructure.Plugins.Models;

/// <summary>
/// Represents a discovered plugin with its descriptor, metadata, and loaded state.
/// </summary>
/// <typeparam name="TPlugin">The plugin contract type.</typeparam>
/// <param name="Descriptor">Plugin metadata (name, version, path).</param>
/// <param name="Plugin">The plugin instance.</param>
/// <param name="Loaded">Whether the plugin has been loaded into the application.</param>
public record PluginEntry<TPlugin>(PluginBaseInfo Descriptor, TPlugin Plugin, bool Loaded) where TPlugin : IPlugin;
