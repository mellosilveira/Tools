using MelloSilveiraTools.Core.Pipelines.Models;
using MelloSilveiraTools.Core.Security;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace MelloSilveiraTools.Core.Pipelines.Notifiers;

/// <summary>
/// High-performance, singleton implementation of <see cref="IPipelineProgressNotifier"/> backed by discrete bounded <see cref="Channel{T}"/>
/// instances correlated by <c>(userId, pipelineIdentifier)</c> in an internal concurrent dictionary for multi-tenant isolation.
/// Channels are evicted deterministically upon the completion of reader consumption, with an upper bound on active channels.
/// </summary>
/// <param name="currentUserProvider">Mandatory current user provider used to isolate channels per authenticated user.</param>
/// <param name="timeProvider">Mandatory time provider for recording creation timestamps to support LRU eviction if capacity is reached.</param>
/// <param name="settings">Mandatory configuration settings defining buffer capacities, full mode policies, and maximum channel limits.</param>
public sealed class ChannelPipelineProgressNotifier(
    ICurrentUserProvider currentUserProvider,
    TimeProvider timeProvider,
    PipelineProgressNotifierSettings settings)
    : IPipelineProgressNotifier
{
    private readonly ConcurrentDictionary<ChannelKey, ChannelEntry> _channels = new();

    /// <inheritdoc/>
    public void EnsureChannel(string pipelineIdentifier) => _ = GetOrCreateEntry(BuildKey(pipelineIdentifier));

    /// <inheritdoc/>
    public ValueTask NotifyAsync(PipelineProgressEvent progressEvent, CancellationToken cancellationToken = default)
    {
        ChannelKey key = BuildKey(progressEvent.PipelineIdentifier);
        ChannelEntry entry = GetOrCreateEntry(key);
        return entry.Channel.Writer.WriteAsync(progressEvent, cancellationToken);
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<PipelineProgressEvent> ReadAllAsync(string pipelineIdentifier, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        (ChannelKey resolvedKey, ChannelEntry? entry) = ResolveChannelForRead(pipelineIdentifier);
        if (entry is null)
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
            if (entry.IsCompleted)
                _channels.TryRemove(resolvedKey, out _);
        }
    }

    /// <inheritdoc/>
    public bool TryComplete(string pipelineIdentifier, Exception? error = null)
    {
        ChannelKey key = BuildKey(pipelineIdentifier);
        if (_channels.TryGetValue(key, out ChannelEntry? entry))
        {
            entry.IsCompleted = true;
            return entry.Channel.Writer.TryComplete(error);
        }

        return false;
    }

    /// <inheritdoc/>
    public ValueTask DisposeAsync()
    {
        foreach (KeyValuePair<ChannelKey, ChannelEntry> pair in _channels)
        {
            pair.Value.Channel.Writer.TryComplete();
        }

        _channels.Clear();
        return ValueTask.CompletedTask;
    }

    private ChannelKey BuildKey(string pipelineIdentifier) => new(currentUserProvider.GetCurrentUserId(), pipelineIdentifier);

    private (ChannelKey Key, ChannelEntry? Entry) ResolveChannelForRead(string pipelineIdentifier)
    {
        ChannelKey ownKey = BuildKey(pipelineIdentifier);
        if (_channels.TryGetValue(ownKey, out ChannelEntry? ownEntry))
            return (ownKey, ownEntry);

        if (currentUserProvider.IsSuperuser())
        {
            KeyValuePair<ChannelKey, ChannelEntry>? latestMatch = _channels.FirstOrDefault(pair => string.Equals(pair.Key.PipelineIdentifier, pipelineIdentifier, StringComparison.Ordinal));
            if (latestMatch.HasValue)
                return (latestMatch.Value.Key, latestMatch.Value.Value);
        }

        return (ownKey, null);
    }

    private ChannelEntry GetOrCreateEntry(ChannelKey key)
    {
        if (_channels.TryGetValue(key, out ChannelEntry? existing))
            return existing;

        EnsureCapacityLimit();

        return _channels.GetOrAdd(key, _ =>
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
        foreach (KeyValuePair<ChannelKey, ChannelEntry> pair in _channels.Where(pair => pair.Value.IsCompleted).OrderBy(pair => pair.Value.CreatedAt))
        {
            if (_channels.TryRemove(pair))
            {
                if (_channels.Count < settings.MaxChannels)
                    return;
            }
        }

        // 2. If still over capacity, purge oldest channels to enforce bounded memory.
        foreach (KeyValuePair<ChannelKey, ChannelEntry> pair in _channels.OrderBy(pair => pair.Value.CreatedAt))
        {
            if (_channels.TryRemove(pair))
            {
                pair.Value.Channel.Writer.TryComplete();
                if (_channels.Count < settings.MaxChannels)
                    return;
            }
        }
    }

    private readonly record struct ChannelKey(long? UserId, string PipelineIdentifier);

    private sealed class ChannelEntry(Channel<PipelineProgressEvent> channel, DateTimeOffset createdAt)
    {
        public Channel<PipelineProgressEvent> Channel { get; } = channel;
        public DateTimeOffset CreatedAt { get; } = createdAt;
        public volatile bool IsCompleted;
    }
}
