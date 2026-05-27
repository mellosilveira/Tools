using MelloSilveiraTools.Plugins.Application.Commands;

namespace MelloSilveiraTools.Plugins.Application.Commands.Get;

/// <summary>
/// Request used to filter plugins returned by the <c>GetPlugins</c> operation.
/// </summary>
public record GetPluginsRequest : PluginsRequest
{
    /// <summary>
    /// When set, restricts the result to plugins that are fully loaded (<c>true</c>) or not fully loaded (<c>false</c>).
    /// </summary>
    public bool? FullyLoaded { get; init; }
}
