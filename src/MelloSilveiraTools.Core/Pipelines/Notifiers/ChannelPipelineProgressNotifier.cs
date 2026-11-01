using MelloSilveiraTools.Core.Pipelines.Models;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace MelloSilveiraTools.Core.Pipelines.Notifiers;

/// <summary>
/// High-performance, singleton implementation of <see cref="IPipelineProgressNotifier"/> backed by discrete bounded <see cref="Channel{T}"/>
/// instances correlated by analysis identifier in an internal concurrent dictionary.
/// Channels are evicted deterministically upon the completion of reader consumption, with an upper bound on active channels.
/// </summary>
/// <param name="timeProvider">Mandatory time provider for recording creation timestamps to support LRU eviction if capacity is reached.</param>
/// <param name="settings">Mandatory configuration settings defining buffer capacities, full mode policies, and maximum channel limits.</param>
public sealed class ChannelPipelineProgressNotifier(
    TimeProvider timeProvider,
    PipelineProgressNotifierSettings settings)
    : IPipelineProgressNotifier
{
    private readonly ConcurrentDictionary<string, ChannelEntry> _channels = new(StringComparer.Ordinal);

    /// <inheritdoc/>
    public ValueTask NotifyAsync(PipelineProgressEvent progressEvent, CancellationToken cancellationToken = default)
    {
        ChannelEntry entry = GetOrCreateEntry(progressEvent.PipelineIdentifier);
        return entry.Channel.Writer.WriteAsync(progressEvent, cancellationToken);
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<PipelineProgressEvent> ReadAllAsync(string pipelineIdentifier, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (!_channels.TryGetValue(pipelineIdentifier, out ChannelEntry? entry) || entry is null)
            throw new KeyNotFoundException($"Pipeline with identifier '{pipelineIdentifier}' not found.");


        try
        {
            await foreach (PipelineProgressEvent item in entry.Channel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
            {
                yield return item;
            }
        }
        finally
        {
            _channels.TryRemove(pipelineIdentifier, out _);
        }
    }

    /// <inheritdoc/>
    public bool TryComplete(string pipelineIdentifier, Exception? error = null)
    {
        if (_channels.TryGetValue(pipelineIdentifier, out ChannelEntry? entry))
        {
            entry.IsCompleted = true;
            return entry.Channel.Writer.TryComplete(error);
        }

        return false;
    }

    /// <inheritdoc/>
    public ValueTask DisposeAsync()
    {
        foreach (KeyValuePair<string, ChannelEntry> pair in _channels)
        {
            pair.Value.Channel.Writer.TryComplete();
        }

        _channels.Clear();
        return ValueTask.CompletedTask;
    }

    private ChannelEntry GetOrCreateEntry(string pipelineIdentifier)
    {
        if (_channels.TryGetValue(pipelineIdentifier, out ChannelEntry? existing))
            return existing;

        EnsureCapacityLimit();

        return _channels.GetOrAdd(pipelineIdentifier, _ =>
        {
            Channel<PipelineProgressEvent> channel = Channel.CreateBounded<PipelineProgressEvent>(new BoundedChannelOptions(settings.ChannelCapacity)
            {
                FullMode = settings.FullMode,
                SingleReader = false,
                SingleWriter = false
            });

            return new ChannelEntry(channel, timeProvider.GetUtcNow());
        });
    }

    private void EnsureCapacityLimit()
    {
        if (_channels.Count < settings.MaxChannels)
            return;

        // 1. Purge completed channels that were abandoned without reading.
        foreach (KeyValuePair<string, ChannelEntry> pair in _channels.Where(pair => pair.Value.IsCompleted).OrderBy(pair => pair.Value.CreatedAt))
        {
            if (_channels.TryRemove(pair))
            {
                if (_channels.Count < settings.MaxChannels)
                    return;
            }
        }

        // 2. If still over capacity, purge oldest channels to enforce bounded memory.
        foreach (KeyValuePair<string, ChannelEntry> pair in _channels.OrderBy(pair => pair.Value.CreatedAt))
        {
            if (_channels.TryRemove(pair))
            {
                pair.Value.Channel.Writer.TryComplete();
                if (_channels.Count < settings.MaxChannels)
                    return;
            }
        }
    }

    private sealed class ChannelEntry(Channel<PipelineProgressEvent> channel, DateTimeOffset createdAt)
    {
        public Channel<PipelineProgressEvent> Channel { get; } = channel;

        public DateTimeOffset CreatedAt { get; } = createdAt;

        public bool IsCompleted { get; set; }
    }
}
