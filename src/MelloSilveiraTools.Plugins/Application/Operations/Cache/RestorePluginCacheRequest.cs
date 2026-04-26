using MelloSilveiraTools.WebApi.Application.Operations;

namespace MelloSilveiraTools.Plugins.Application.Operations.Cache;

/// <summary>
/// Request used by the <c>RestorePluginCache</c> operation to identify which plugins should have their cache restored.
/// </summary>
public record RestorePluginCacheRequest : OperationRequestBase
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
