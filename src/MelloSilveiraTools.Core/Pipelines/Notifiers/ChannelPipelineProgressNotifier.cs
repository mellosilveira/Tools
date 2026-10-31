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
/// <param name="settings">Mandatory configuration settings defining buffer capacities, full mode policies, and maximum channel limits.</param>
/// <param name="timeProvider">Mandatory time provider for recording creation timestamps to support LRU eviction if capacity is reached.</param>
public sealed class ChannelPipelineProgressNotifier(
    PipelineProgressNotifierSettings settings,
    TimeProvider timeProvider)
    : IPipelineProgressNotifier
{
    private readonly ConcurrentDictionary<string, ChannelEntry> _channels = new(StringComparer.Ordinal);

    /// <inheritdoc/>
    public ValueTask NotifyAsync(string analysisIdentifier, PipelineProgressEvent progressEvent, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(analysisIdentifier);
        ChannelEntry entry = GetOrCreateEntry(analysisIdentifier);
        return entry.Channel.Writer.WriteAsync(progressEvent, cancellationToken);
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<PipelineProgressEvent> ReadAllAsync(
        string analysisIdentifier,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(analysisIdentifier);
        ChannelEntry entry = GetOrCreateEntry(analysisIdentifier);

        try
        {
            await foreach (PipelineProgressEvent item in entry.Channel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
            {
                yield return item;
            }
        }
        finally
        {
            _channels.TryRemove(analysisIdentifier, out _);
        }
    }

    /// <inheritdoc/>
    public bool TryComplete(string analysisIdentifier, Exception? error = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(analysisIdentifier);
        if (_channels.TryGetValue(analysisIdentifier, out ChannelEntry? entry))
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

    private ChannelEntry GetOrCreateEntry(string analysisIdentifier)
    {
        if (_channels.TryGetValue(analysisIdentifier, out ChannelEntry? existing))
        {
            return existing;
        }

        EnsureCapacityLimit();

        return _channels.GetOrAdd(analysisIdentifier, _ =>
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
        {
            return;
        }

        // 1. Purge completed channels that were abandoned without reading.
        KeyValuePair<string, ChannelEntry>[] completedEntries = _channels
            .Where(pair => pair.Value.IsCompleted)
            .OrderBy(pair => pair.Value.CreatedAt)
            .ToArray();

        foreach (KeyValuePair<string, ChannelEntry> pair in completedEntries)
        {
            if (_channels.TryRemove(pair))
            {
                if (_channels.Count < settings.MaxChannels)
                {
                    return;
                }
            }
        }

        // 2. If still over capacity, purge oldest channels to enforce bounded memory.
        KeyValuePair<string, ChannelEntry>[] oldestEntries = _channels
            .OrderBy(pair => pair.Value.CreatedAt)
            .ToArray();

        foreach (KeyValuePair<string, ChannelEntry> pair in oldestEntries)
        {
            if (_channels.TryRemove(pair))
            {
                pair.Value.Channel.Writer.TryComplete();
                if (_channels.Count < settings.MaxChannels)
                {
                    return;
                }
            }
        }
    }

    private sealed class ChannelEntry(
        Channel<PipelineProgressEvent> channel,
        DateTimeOffset createdAt)
    {
        public Channel<PipelineProgressEvent> Channel { get; } = channel;

        public DateTimeOffset CreatedAt { get; } = createdAt;

        public bool IsCompleted { get; set; }
    }
}
