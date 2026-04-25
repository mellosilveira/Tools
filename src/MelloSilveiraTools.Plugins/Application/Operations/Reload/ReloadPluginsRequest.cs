using MelloSilveiraTools.Plugins.Application.Operations.Load;

namespace MelloSilveiraTools.Plugins.Application.Operations.Reload;

/// <summary>
/// Request used by the <c>ReloadPlugins</c> operation to identify which plugins should be reloaded and whether to force the reload.
/// </summary>
public record ReloadPluginsRequest : LoadPluginsRequest
{
    /// <summary>
    /// When <c>true</c>, plugins are reloaded even if they are already up to date.
    /// </summary>
    public bool Force { get; init; }
}
