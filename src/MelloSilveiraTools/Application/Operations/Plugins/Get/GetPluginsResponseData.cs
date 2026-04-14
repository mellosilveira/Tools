namespace MelloSilveiraTools.Application.Operations.Plugins.Get;

public record GetPluginsResponseData
{
    public string Name { get; init; }
    public string Version { get; init; }
    public string FullPath { get; init; }
    public DateTimeOffset DiscoveredAt { get; init; }
    public IReadOnlyDictionary<string, bool> TypesLoadedStatus { get; init; }
    public bool IsFullyLoaded { get; init; }
    public DateTimeOffset? FullyLoadedAt { get; init; }
}
