using MelloSilveiraTools.Core.Pipelines.Models;

namespace MelloSilveiraTools.Core.Pipelines.Notifiers;

/// <summary>
/// Defines an asynchronous contract for emitting real-time <see cref="PipelineProgressEvent"/> notifications
/// to in-memory streams (e.g., <see cref="ChannelPipelineProgressNotifier"/>) or external message brokers.
/// </summary>
public interface IPipelineProgressNotifier : IAsyncDisposable
{
    /// <summary>
    /// Publishes a pipeline execution progress event asynchronously.
    /// </summary>
    /// <param name="progressEvent">The immutable progress event payload.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous notification operation.</returns>
    ValueTask NotifyAsync(PipelineProgressEvent progressEvent, CancellationToken cancellationToken = default);
}
