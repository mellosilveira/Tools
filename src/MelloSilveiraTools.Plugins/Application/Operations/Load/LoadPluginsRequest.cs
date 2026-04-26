using MelloSilveiraTools.WebApi.Application.Operations;

namespace MelloSilveiraTools.Plugins.Application.Operations.Load;

/// <summary>
/// Request used by the <c>LoadPlugins</c> operation to identify which plugins should be loaded at runtime.
/// </summary>
public record LoadPluginsRequest : OperationRequestBase
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
