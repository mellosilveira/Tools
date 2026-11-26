using MelloSilveiraTools.Core.Pipelines.Models;
using MelloSilveiraTools.Core.Pipelines.Notifiers;
using MelloSilveiraTools.Core.Security;
using System.Threading.Channels;

namespace UnitTests.Core.Pipelines.Notifiers;

/// <summary>
/// Unit tests for <see cref="ChannelPipelineProgressNotifier"/> and <see cref="NullPipelineProgressNotifier"/>
/// verifying event streaming, completion signaling, LRU channel eviction, and cleanup.
/// </summary>
public sealed class ChannelPipelineProgressNotifierTests
{
    private sealed class IncrementingTimeProvider(DateTimeOffset startTime) : TimeProvider
    {
        private long _ticksOffset;

        public override DateTimeOffset GetUtcNow() => startTime.AddSeconds(Interlocked.Increment(ref _ticksOffset));
    }

    private static PipelineProgressEvent CreateEvent(string pipelineId, string stepName = "Step1") => new()
    {
        PipelineIdentifier = pipelineId,
        StepName = stepName,
        Description = "desc",
        State = PipelineStepState.Started,
        Timestamp = DateTimeOffset.UtcNow
    };

    /// <summary>
    /// Verifies NotifyAsync, TryComplete, and ReadAllAsync deliver events and clean up the channel after reading.
    /// </summary>
    [Fact]
    public async Task NotifyReadComplete_DeliversEventsAndRemovesChannelAfterConsumption()
    {
        // Arrange
        PipelineProgressNotifierSettings settings = new()
        {
            ChannelCapacity = 16,
            FullMode = BoundedChannelFullMode.Wait,
            MaxChannels = 4
        };
        await using ChannelPipelineProgressNotifier notifier = new(new CurrentUserProvider(), TimeProvider.System, settings);

        // Act
        await notifier.NotifyAsync(CreateEvent("run-1", "S1"));
        await notifier.NotifyAsync(CreateEvent("run-1", "S2"));
        bool completed = notifier.TryComplete("run-1");
        bool unknownCompleted = notifier.TryComplete("unknown");

        List<PipelineProgressEvent> events = [];
        await foreach (PipelineProgressEvent evt in notifier.ReadAllAsync("run-1"))
            events.Add(evt);

        // Assert
        Assert.True(completed);
        Assert.False(unknownCompleted);
        Assert.Equal(2, events.Count);
        Assert.Equal("S1", events[0].StepName);
        Assert.Equal("S2", events[1].StepName);

        await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
        {
            await foreach (PipelineProgressEvent _ in notifier.ReadAllAsync("run-1")) { }
        });
    }

    /// <summary>
    /// Verifies LRU eviction first removes abandoned completed channels, then oldest active channels when MaxChannels is reached.
    /// </summary>
    [Fact]
    public async Task NotifyAsync_WhenMaxChannelsReached_EvictsCompletedThenOldestChannels()
    {
        // Arrange
        IncrementingTimeProvider timeProvider = new(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        PipelineProgressNotifierSettings settings = new()
        {
            ChannelCapacity = 8,
            FullMode = BoundedChannelFullMode.DropOldest,
            MaxChannels = 2
        };
        await using ChannelPipelineProgressNotifier notifier = new(new CurrentUserProvider(), timeProvider, settings);

        // Act: Create 2 channels, complete the first one (abandoned), then add a 3rd channel (should evict run-1)
        await notifier.NotifyAsync(CreateEvent("run-1"));
        notifier.TryComplete("run-1");
        await notifier.NotifyAsync(CreateEvent("run-2"));
        await notifier.NotifyAsync(CreateEvent("run-3"));

        // Assert: run-1 was evicted, run-2 and run-3 remain
        await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
        {
            await foreach (PipelineProgressEvent _ in notifier.ReadAllAsync("run-1")) { }
        });

        // Now both run-2 and run-3 are active. Adding run-4 should evict oldest active (run-2).
        await notifier.NotifyAsync(CreateEvent("run-4"));

        await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
        {
            await foreach (PipelineProgressEvent _ in notifier.ReadAllAsync("run-2")) { }
        });

        notifier.TryComplete("run-3");
        List<PipelineProgressEvent> run3Events = [];
        await foreach (PipelineProgressEvent evt in notifier.ReadAllAsync("run-3"))
            run3Events.Add(evt);
        Assert.Single(run3Events);
    }

    /// <summary>
    /// Verifies DisposeAsync completes active channels cleanly.
    /// </summary>
    [Fact]
    public async Task DisposeAsync_CompletesAllChannelsAndClearsDictionary()
    {
        // Arrange
        PipelineProgressNotifierSettings settings = new()
        {
            ChannelCapacity = 4,
            FullMode = BoundedChannelFullMode.Wait,
            MaxChannels = 4
        };
        ChannelPipelineProgressNotifier notifier = new(new CurrentUserProvider(), TimeProvider.System, settings);
        await notifier.NotifyAsync(CreateEvent("run-x"));

        // Act
        await notifier.DisposeAsync();

        // Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
        {
            await foreach (PipelineProgressEvent _ in notifier.ReadAllAsync("run-x")) { }
        });
    }

    /// <summary>
    /// Verifies NullPipelineProgressNotifier behaves as a zero-allocation no-op.
    /// </summary>
    [Fact]
    public async Task NullPipelineProgressNotifier_NoOpsCleanly()
    {
        NullPipelineProgressNotifier notifier = NullPipelineProgressNotifier.Instance;

        await notifier.NotifyAsync(CreateEvent("any"));
        Assert.True(notifier.TryComplete("any"));

        List<PipelineProgressEvent> events = [];
        await foreach (PipelineProgressEvent evt in notifier.ReadAllAsync("any"))
            events.Add(evt);

        Assert.Empty(events);
        await notifier.DisposeAsync();
    }
}
