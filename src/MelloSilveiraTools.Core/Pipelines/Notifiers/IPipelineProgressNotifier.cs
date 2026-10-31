using MelloSilveiraTools.Core.Pipelines.Models;

namespace MelloSilveiraTools.Core.Pipelines.Notifiers;

/// <summary>
/// Defines an asynchronous contract for emitting and consuming real-time <see cref="PipelineProgressEvent"/> notifications
/// correlated by a mandatory unique analysis or pipeline execution identifier.
/// </summary>
public interface IPipelineProgressNotifier : IAsyncDisposable
{
    /// <summary>
    /// Publishes a pipeline execution progress event asynchronously for the specified analysis identifier.
    /// </summary>
    /// <param name="analysisIdentifier">Mandatory unique business identifier correlating the analysis or pipeline execution.</param>
    /// <param name="progressEvent">The immutable progress event payload.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous notification operation.</returns>
    ValueTask NotifyAsync(string analysisIdentifier, PipelineProgressEvent progressEvent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns an <see cref="IAsyncEnumerable{T}"/> that reads all emitted <see cref="PipelineProgressEvent"/> items
    /// for the specified analysis identifier until its channel completes.
    /// </summary>
    /// <param name="analysisIdentifier">Mandatory unique business identifier correlating the analysis or pipeline execution.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous enumeration.</param>
    /// <returns>An asynchronous stream of <see cref="PipelineProgressEvent"/> instances.</returns>
    IAsyncEnumerable<PipelineProgressEvent> ReadAllAsync(string analysisIdentifier, CancellationToken cancellationToken = default);

    /// <summary>
    /// Signals completion for the channel associated with the specified analysis identifier.
    /// </summary>
    /// <param name="analysisIdentifier">Mandatory unique business identifier correlating the analysis or pipeline execution.</param>
    /// <param name="error">Optional exception if the pipeline terminated with a fault.</param>
    /// <returns><see langword="true"/> if the channel was found and successfully marked complete; otherwise, <see langword="false"/>.</returns>
    bool TryComplete(string analysisIdentifier, Exception? error = null);
}
