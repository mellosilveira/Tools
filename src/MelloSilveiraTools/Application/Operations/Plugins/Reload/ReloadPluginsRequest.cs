using MelloSilveiraTools.Application.Operations.Plugins.Load;

namespace MelloSilveiraTools.Application.Operations.Plugins.Reload;

public record ReloadPluginsRequest : LoadPluginsRequest
{
    public bool Force { get; init; }
}
