namespace MelloSilveiraTools.Application.Operations.Plugins.Get
{
    public record GetPluginsRequest : OperationRequestBase
    {
        public string Name { get; init; }
        public string Version { get; init; }
        public bool? FullyLoaded { get; init; }
    }
}
