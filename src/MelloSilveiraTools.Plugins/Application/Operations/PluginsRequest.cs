using MelloSilveiraTools.WebApi.Application.Commands;

namespace MelloSilveiraTools.Plugins.Application.Operations;

/// <summary>
/// Request used to filter plugins for a command.
/// </summary>
public record PluginsRequest : RequestBase
{
    /// <summary>
    /// Optional plugin name. When omitted, all plugins are considered.
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// Optional plugin version (in <c>PluginVersion</c> string form). When omitted, all versions are considered.
    /// </summary>
    public string? Version { get; init; }
}
