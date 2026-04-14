namespace MelloSilveiraTools.Application.Operations.Plugins.Cache;

public record PersistPluginCacheRequest : OperationRequestBase
{
    public string Target { get; init; }
}
