namespace MelloSilveiraTools.Plugins.Application.Commands.Get;

/// <summary>
/// Request used to filter plugin binaries inspected by the superuser <c>InspectPlugins</c> operation.
/// </summary>
public record InspectPluginsRequest : PluginsRequest
{
    /// <summary>
    /// Optional canonical storage partition filter (<c>loaded</c>, <c>incoming</c>, <c>pending</c>, <c>archive</c>, <c>quarantine</c>, <c>rejected</c>).
    /// When omitted, all main plugin partitions are inspected.
    /// </summary>
    public string? Partition { get; init; }
}
