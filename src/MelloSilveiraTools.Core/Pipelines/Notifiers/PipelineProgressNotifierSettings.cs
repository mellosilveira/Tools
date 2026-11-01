using System.Threading.Channels;

namespace MelloSilveiraTools.Core.Pipelines.Notifiers;

/// <summary>
/// Externalized configuration settings defining buffer capacities, full mode behaviors,
/// and maximum channel retention limits for the singleton <see cref="ChannelPipelineProgressNotifier"/>.
/// </summary>
public sealed record PipelineProgressNotifierSettings
{
    /// <summary>
    /// The maximum number of events buffered in each analysis channel before applying backpressure or dropping events.
    /// </summary>
    public required int ChannelCapacity { get; init; }

    /// <summary>
    /// The behavior applied when a bounded analysis channel reaches maximum capacity.
    /// </summary>
    public required BoundedChannelFullMode FullMode { get; init; }

    /// <summary>
    /// The maximum number of concurrent active channels retained in memory before purging abandoned completed channels.
    /// </summary>
    public required int MaxChannels { get; init; }
}
