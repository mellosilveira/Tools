using MelloSilveiraTools.Core.Pipelines.Models;
using System.Threading.Channels;

namespace MelloSilveiraTools.Core.Pipelines.Notifiers;

/// <summary>
/// High-performance, bounded-memory implementation of <see cref="IPipelineProgressNotifier"/> backed by a <see cref="Channel{T}"/>.
/// Designed for real-time NDJSON or Server-Sent Events (SSE) streaming to HTTP clients with built-in backpressure.
/// </summary>
public sealed class ChannelPipelineProgressNotifier : IPipelineProgressNotifier
{
    private readonly Channel<PipelineProgressEvent> _channel;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChannelPipelineProgressNotifier"/> class with a bounded capacity.
    /// </summary>
    /// <param name="capacity">The maximum number of events buffered in the channel before applying backpressure or full-mode behavior. Defaults to 256.</param>
    /// <param name="fullMode">The behavior to apply when the bounded channel is full. Defaults to <see cref="BoundedChannelFullMode.Wait"/>.</param>
    public ChannelPipelineProgressNotifier(int capacity = 256, BoundedChannelFullMode fullMode = BoundedChannelFullMode.Wait)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(capacity);
        _channel = Channel.CreateBounded<PipelineProgressEvent>(new BoundedChannelOptions(capacity)
        {
            FullMode = fullMode,
            SingleReader = true,
            SingleWriter = false
        });
    }

    /// <inheritdoc/>
    public ValueTask NotifyAsync(PipelineProgressEvent progressEvent, CancellationToken cancellationToken = default) => _channel.Writer.WriteAsync(progressEvent, cancellationToken);

    /// <summary>
    /// Attempts to mark the underlying channel as complete, signaling to consumers of <see cref="ReadAllAsync"/> that no further progress events will be emitted.
    /// </summary>
    /// <param name="error">An optional exception indicating that the pipeline terminated due to an unhandled fault.</param>
    /// <returns><see langword="true"/> if this operation successfully completed the channel; otherwise, <see langword="false"/> if the channel was already marked as complete.</returns>
    public bool TryComplete(Exception? error = null) => _channel.Writer.TryComplete(error);

    /// <summary>
    /// Returns an <see cref="IAsyncEnumerable{T}"/> that reads all emitted <see cref="PipelineProgressEvent"/> items until the channel completes.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the asynchronous enumeration.</param>
    /// <returns>An asynchronous stream of <see cref="PipelineProgressEvent"/> instances.</returns>
    public IAsyncEnumerable<PipelineProgressEvent> ReadAllAsync(CancellationToken cancellationToken = default) => _channel.Reader.ReadAllAsync(cancellationToken);

    /// <inheritdoc/>
    public ValueTask DisposeAsync()
    {
        TryComplete();
        return ValueTask.CompletedTask;
    }
}
