namespace MelloSilveiraTools.Application.Operations.Plugins.Load;

public record LoadPluginsRequest : OperationRequestBase
{
    public string Name { get; init; }
    public string Version { get; init; }
}
