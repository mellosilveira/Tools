namespace MelloSilveiraTools.Application.Operations.Plugins.Cache;

public record ClearPluginCacheRequest : OperationRequestBase
{
    public string Stage { get; init; }
}
