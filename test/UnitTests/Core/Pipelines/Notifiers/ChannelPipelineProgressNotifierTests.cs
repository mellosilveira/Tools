using MelloSilveiraTools.Core.Pipelines.Models;
using MelloSilveiraTools.Core.Pipelines.Notifiers;

namespace UnitTests.Core.Pipelines.Notifiers;

/// <summary>
/// Unit tests for <see cref="ChannelPipelineProgressNotifier"/> and <see cref="NullPipelineProgressNotifier"/>.
/// </summary>
public sealed class ChannelPipelineProgressNotifierTests
{
    /// <summary>
    /// Verifies that <see cref="ChannelPipelineProgressNotifier"/> streams emitted events in order and completes cleanly.
    /// </summary>
    [Fact]
    public async Task NotifyAsync_StreamsEmittedEventsInOrderAndCompletesCleanly()
    {
        // Arrange
        await using ChannelPipelineProgressNotifier notifier = new(capacity: 16);
        DateTimeOffset now = DateTimeOffset.UtcNow;
        PipelineProgressEvent first = new()
        {
            StepName = "Step1",
            Description = "Starting",
            State = PipelineStepState.Started,
            Timestamp = now
        };
        PipelineProgressEvent second = new()
        {
            StepName = "Step1",
            Description = "Completed",
            State = PipelineStepState.Completed,
            Timestamp = now,
            Details = "{\"items\":10}"
        };

        // Act
        await notifier.NotifyAsync(first);
        await notifier.NotifyAsync(second);
        bool completed = notifier.TryComplete();

        List<PipelineProgressEvent> received = [];
        await foreach (PipelineProgressEvent item in notifier.ReadAllAsync())
        {
            received.Add(item);
        }

        // Assert
        Assert.True(completed);
        Assert.False(notifier.TryComplete());
        Assert.Equal(2, received.Count);
        Assert.Equal(first, received[0]);
        Assert.Equal(second, received[1]);
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
            StepName = "Step1",
            Description = "Starting",
            State = PipelineStepState.Started,
            Timestamp = DateTimeOffset.UtcNow
        };

        // Act
        ValueTask task = notifier.NotifyAsync(progressEvent);

        // Assert
        Assert.True(task.IsCompletedSuccessfully);
        await task;
        await notifier.DisposeAsync();
    }
}
