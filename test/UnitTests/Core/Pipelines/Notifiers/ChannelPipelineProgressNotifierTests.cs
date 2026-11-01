using MelloSilveiraTools.Core.Pipelines.Models;
using MelloSilveiraTools.Core.Pipelines.Notifiers;
using System.Threading.Channels;

namespace UnitTests.Core.Pipelines.Notifiers;

/// <summary>
/// Unit tests for <see cref="ChannelPipelineProgressNotifier"/>, <see cref="ContextPipelineProgressNotifier"/>, and <see cref="NullPipelineProgressNotifier"/>.
/// </summary>
public sealed class ChannelPipelineProgressNotifierTests
{
    private static PipelineProgressNotifierSettings CreateTestSettings(int capacity = 16, int maxChannels = 100) => new()
    {
        ChannelCapacity = capacity,
        FullMode = BoundedChannelFullMode.Wait,
        MaxChannels = maxChannels
    };

    /// <summary>
    /// Verifies that <see cref="ChannelPipelineProgressNotifier"/> streams emitted events in order and completes cleanly.
    /// </summary>
    [Fact]
    public async Task NotifyAsync_StreamsEmittedEventsInOrderAndCompletesCleanly()
    {
        // Arrange
        PipelineProgressNotifierSettings settings = CreateTestSettings(16);
        await using ChannelPipelineProgressNotifier notifier = new(settings, TimeProvider.System);
        DateTimeOffset now = DateTimeOffset.UtcNow;
        PipelineProgressEvent first = new()
        {
            AnalysisIdentifier = "test_analysis_1",
            StepName = "Step1",
            Description = "Starting",
            State = PipelineStepState.Started,
            Timestamp = now
        };
        PipelineProgressEvent second = new()
        {
            AnalysisIdentifier = "test_analysis_1",
            StepName = "Step1",
            Description = "Completed",
            State = PipelineStepState.Completed,
            Timestamp = now,
            Details = "{\"items\":10}"
        };

        // Act
        await notifier.NotifyAsync("test_analysis_1", first);
        await notifier.NotifyAsync("test_analysis_1", second);
        bool completed = notifier.TryComplete("test_analysis_1");

        List<PipelineProgressEvent> received = [];
        await foreach (PipelineProgressEvent item in notifier.ReadAllAsync("test_analysis_1"))
        {
            received.Add(item);
        }

        // Assert
        Assert.True(completed);
        Assert.False(notifier.TryComplete("test_analysis_1"));
        Assert.Equal(2, received.Count);
        Assert.Equal(first, received[0]);
        Assert.Equal(second, received[1]);
    }

    /// <summary>
    /// Verifies that <see cref="ChannelPipelineProgressNotifier"/> isolates event streams by distinct analysis identifiers.
    /// </summary>
    [Fact]
    public async Task NotifyAsync_WithAnalysisIdentifier_IsolatesStreamsPerAnalysisIdentifier()
    {
        // Arrange
        PipelineProgressNotifierSettings settings = CreateTestSettings(16);
        await using ChannelPipelineProgressNotifier notifier = new(settings, TimeProvider.System);
        DateTimeOffset now = DateTimeOffset.UtcNow;

        PipelineProgressEvent eventA = new()
        {
            AnalysisIdentifier = "analysis_A",
            StepName = "Segmenter",
            Description = "Analysis A running",
            State = PipelineStepState.Running,
            Timestamp = now
        };

        PipelineProgressEvent eventB = new()
        {
            AnalysisIdentifier = "analysis_B",
            StepName = "CurveFitter",
            Description = "Analysis B running",
            State = PipelineStepState.Running,
            Timestamp = now
        };

        // Act
        await notifier.NotifyAsync("analysis_A", eventA);
        await notifier.NotifyAsync("analysis_B", eventB);

        notifier.TryComplete("analysis_A");
        notifier.TryComplete("analysis_B");

        List<PipelineProgressEvent> receivedA = [];
        await foreach (PipelineProgressEvent item in notifier.ReadAllAsync("analysis_A"))
        {
            receivedA.Add(item);
        }

        List<PipelineProgressEvent> receivedB = [];
        await foreach (PipelineProgressEvent item in notifier.ReadAllAsync("analysis_B"))
        {
            receivedB.Add(item);
        }

        // Assert
        Assert.Single(receivedA);
        Assert.Equal(eventA, receivedA[0]);

        Assert.Single(receivedB);
        Assert.Equal(eventB, receivedB[0]);
    }

    /// <summary>
    /// Verifies that when the writer completes before the reader connects, all buffered events are still drained
    /// and the channel is subsequently evicted from the dictionary.
    /// </summary>
    [Fact]
    public async Task ReadAllAsync_WhenWriterCompletesBeforeReaderConnects_StillDrainsAllEventsAndEvicts()
    {
        // Arrange
        PipelineProgressNotifierSettings settings = CreateTestSettings(16);
        await using ChannelPipelineProgressNotifier notifier = new(settings, TimeProvider.System);
        DateTimeOffset now = DateTimeOffset.UtcNow;

        PipelineProgressEvent ev = new()
        {
            AnalysisIdentifier = "pre_completed_analysis",
            StepName = "FastStep",
            Description = "Finished quickly",
            State = PipelineStepState.Completed,
            Timestamp = now
        };

        // Act: producer writes and completes BEFORE consumer connects
        await notifier.NotifyAsync("pre_completed_analysis", ev);
        bool completed = notifier.TryComplete("pre_completed_analysis");

        // Consumer connects afterwards
        List<PipelineProgressEvent> received = [];
        await foreach (PipelineProgressEvent item in notifier.ReadAllAsync("pre_completed_analysis"))
        {
            received.Add(item);
        }

        // Assert
        Assert.True(completed);
        Assert.Single(received);
        Assert.Equal(ev, received[0]);

        // After reading reaches completion, channel is evicted; TryComplete now returns false
        Assert.False(notifier.TryComplete("pre_completed_analysis"));
    }

    /// <summary>
    /// Verifies that when the maximum channel limit is exceeded, abandoned completed channels are purged.
    /// </summary>
    [Fact]
    public async Task GetOrCreateEntry_WhenCapacityExceeded_PurgesAbandonedCompletedChannels()
    {
        // Arrange: capacity limited to 2 channels
        PipelineProgressNotifierSettings settings = CreateTestSettings(capacity: 8, maxChannels: 2);
        await using ChannelPipelineProgressNotifier notifier = new(settings, TimeProvider.System);
        DateTimeOffset now = DateTimeOffset.UtcNow;

        PipelineProgressEvent ev = new()
        {
            AnalysisIdentifier = "abandoned_1",
            StepName = "Step",
            Description = "Abandoned",
            State = PipelineStepState.Completed,
            Timestamp = now
        };

        // Populate 2 channels and mark first as completed without reading
        await notifier.NotifyAsync("abandoned_1", ev);
        notifier.TryComplete("abandoned_1");

        await notifier.NotifyAsync("active_2", ev with { AnalysisIdentifier = "active_2" });

        // Act: creating a 3rd channel triggers capacity limit enforcement, purging abandoned_1
        await notifier.NotifyAsync("new_3", ev with { AnalysisIdentifier = "new_3" });

        // Assert: abandoned_1 was purged, so TryComplete on it returns false
        Assert.False(notifier.TryComplete("abandoned_1"));
        Assert.True(notifier.TryComplete("active_2"));
        Assert.True(notifier.TryComplete("new_3"));
    }

    /// <summary>
    /// Verifies that <see cref="ContextPipelineProgressNotifier"/> enriches emitted events with the bound analysis identifier.
    /// </summary>
    [Fact]
    public async Task ContextPipelineProgressNotifier_EnrichesEventsWithAnalysisIdentifier()
    {
        // Arrange
        PipelineProgressNotifierSettings settings = CreateTestSettings(16);
        await using ChannelPipelineProgressNotifier notifier = new(settings, TimeProvider.System);
        ContextPipelineProgressNotifier contextNotifier = new(notifier, "analysis_ctx_123");
        DateTimeOffset now = DateTimeOffset.UtcNow;

        PipelineProgressEvent progressEvent = new()
        {
            StepName = "Simulation",
            Description = "Simulating with context",
            State = PipelineStepState.Running,
            Timestamp = now
        };

        // Act
        await contextNotifier.NotifyAsync(string.Empty, progressEvent);
        contextNotifier.TryComplete(string.Empty);

        List<PipelineProgressEvent> received = [];
        await foreach (PipelineProgressEvent item in notifier.ReadAllAsync("analysis_ctx_123"))
        {
            received.Add(item);
        }

        // Assert
        Assert.Single(received);
        Assert.Equal("analysis_ctx_123", received[0].AnalysisIdentifier);
        Assert.Equal("Simulation", received[0].StepName);
    }

    /// <summary>
    /// Verifies that <see cref="NullPipelineProgressNotifier"/> completes synchronously with no side effects.
    /// </summary>
    [Fact]
    public async Task NullPipelineProgressNotifier_NotifyAsync_CompletesSynchronously()
    {
        // Arrange
        NullPipelineProgressNotifier notifier = NullPipelineProgressNotifier.Instance;
        PipelineProgressEvent progressEvent = new()
        {
            AnalysisIdentifier = "test_analysis_null",
            StepName = "Step1",
            Description = "Starting",
            State = PipelineStepState.Started,
            Timestamp = DateTimeOffset.UtcNow
        };

        // Act
        ValueTask task = notifier.NotifyAsync("test_analysis_null", progressEvent);

        // Assert
        Assert.True(task.IsCompletedSuccessfully);
        await task;
        await notifier.DisposeAsync();
    }
}
