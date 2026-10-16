using MelloSilveiraTools.Core.Pipelines;
using MelloSilveiraTools.Core.Pipelines.Dataflow;
using MelloSilveiraTools.Core.Pipelines.Models;
using MelloSilveiraTools.Core.Pipelines.Telemetry;
using Microsoft.Extensions.Logging.Abstractions;

namespace UnitTests.Core.Pipelines.Telemetry;

/// <summary>
/// Unit tests verifying real-time emission of <see cref="PipelineProgressEvent"/> notifications
/// across synchronous, asynchronous, streaming, and full Dataflow pipeline topologies.
/// </summary>
public sealed class PipelineProgressEmissionTests
{
    /// <summary>
    /// Verifies that a successful synchronous execution emits Started and Completed progress events.
    /// </summary>
    [Fact]
    public void HandleExecution_SyncAction_WhenSuccessful_EmitsStartedAndCompletedEvents()
    {
        // Arrange
        TestProgressReporter reporter = new();
        Action<int> action = TelemetryExtensions.HandleExecution<int>(
            NullLogger.Instance,
            "SyncStep",
            input => { _ = input * 2; },
            progress: reporter);

        // Act
        action(42);

        // Assert
        IReadOnlyList<PipelineProgressEvent> events = reporter.Events;
        Assert.Equal(2, events.Count);

        Assert.Equal("SyncStep", events[0].StepName);
        Assert.Equal(PipelineStepState.Started, events[0].State);

        Assert.Equal("SyncStep", events[1].StepName);
        Assert.Equal(PipelineStepState.Completed, events[1].State);
    }

    /// <summary>
    /// Verifies that a failing synchronous execution emits Started and Faulted progress events.
    /// </summary>
    [Fact]
    public void HandleExecution_SyncAction_WhenFaulted_EmitsStartedAndFaultedEvents()
    {
        // Arrange
        TestProgressReporter reporter = new();
        Action<int> action = TelemetryExtensions.HandleExecution<int>(
            NullLogger.Instance,
            "FailingSyncStep",
            _ => throw new InvalidOperationException("Sync error"),
            progress: reporter);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => action(10));

        IReadOnlyList<PipelineProgressEvent> events = reporter.Events;
        Assert.Equal(2, events.Count);

        Assert.Equal("FailingSyncStep", events[0].StepName);
        Assert.Equal(PipelineStepState.Started, events[0].State);

        Assert.Equal("FailingSyncStep", events[1].StepName);
        Assert.Equal(PipelineStepState.Faulted, events[1].State);
        Assert.Contains("Sync error", events[1].Detail);
    }

    /// <summary>
    /// Verifies that a successful asynchronous function emits Started and Completed progress events.
    /// </summary>
    [Fact]
    public async Task HandleExecution_AsyncFunc_WhenSuccessful_EmitsStartedAndCompletedEvents()
    {
        // Arrange
        TestProgressReporter reporter = new();
        Func<int, Task<string>> asyncFunc = TelemetryExtensions.HandleExecution<int, string>(
            NullLogger.Instance,
            "AsyncStep",
            async (input, ct) =>
            {
                await Task.Yield();
                return $"Value: {input}";
            },
            progress: reporter);

        // Act
        string result = await asyncFunc(99);

        // Assert
        Assert.Equal("Value: 99", result);

        IReadOnlyList<PipelineProgressEvent> events = reporter.Events;
        Assert.Equal(2, events.Count);

        Assert.Equal("AsyncStep", events[0].StepName);
        Assert.Equal(PipelineStepState.Started, events[0].State);

        Assert.Equal("AsyncStep", events[1].StepName);
        Assert.Equal(PipelineStepState.Completed, events[1].State);
    }

    /// <summary>
    /// Verifies that an asynchronous function failing after retries emits Started and Faulted progress events.
    /// </summary>
    [Fact]
    public async Task HandleExecution_AsyncFunc_WhenRetriesExhausted_EmitsStartedAndFaultedEvents()
    {
        // Arrange
        TestProgressReporter reporter = new();
        RetryOptions retryOptions = new(MaxAttempts: 2, InitialDelayMs: 10, BackoffFactor: 1.0);

        Func<int, Task<string>> asyncFunc = TelemetryExtensions.HandleExecution<int, string>(
            NullLogger.Instance,
            "FailingAsyncStep",
            async (_, ct) =>
            {
                await Task.Yield();
                throw new ApplicationException("Fatal error");
            },
            retryOptions: retryOptions,
            progress: reporter);

        // Act & Assert
        await Assert.ThrowsAsync<ApplicationException>(() => asyncFunc(1));

        IReadOnlyList<PipelineProgressEvent> events = reporter.Events;
        Assert.True(events.Count >= 2);

        Assert.Equal("FailingAsyncStep", events[0].StepName);
        Assert.Equal(PipelineStepState.Started, events[0].State);

        PipelineProgressEvent lastEvent = events[^1];
        Assert.Equal("FailingAsyncStep", lastEvent.StepName);
        Assert.Equal(PipelineStepState.Faulted, lastEvent.State);
        Assert.Contains("Fatal error", lastEvent.Detail);
    }

    /// <summary>
    /// Verifies that an asynchronous streaming enumerable emits Started, Running (with emitted counts), and Completed events.
    /// </summary>
    [Fact]
    public async Task HandleExecution_AsyncEnumerable_EmitsStartedRunningAndCompletedEvents()
    {
        // Arrange
        TestProgressReporter reporter = new();
        Func<int, IAsyncEnumerable<int>> streamFunc = TelemetryExtensions.HandleExecution<int, int>(
            NullLogger.Instance,
            "StreamStep",
            (count, _) => GenerateNumbersAsync(count),
            progress: reporter);

        // Act
        List<int> results = [];
        await foreach (int item in streamFunc(5))
        {
            results.Add(item);
        }

        // Assert
        Assert.Equal(5, results.Count);

        IReadOnlyList<PipelineProgressEvent> events = reporter.Events;
        Assert.True(events.Count >= 3);

        Assert.Equal("StreamStep", events[0].StepName);
        Assert.Equal(PipelineStepState.Started, events[0].State);

        Assert.Contains(events, e => e.State == PipelineStepState.Running && e.Detail != null && e.Detail.Contains("Items emitted"));

        PipelineProgressEvent lastEvent = events[^1];
        Assert.Equal("StreamStep", lastEvent.StepName);
        Assert.Equal(PipelineStepState.Completed, lastEvent.State);
        Assert.Contains("Items emitted: 5", lastEvent.Detail);
    }

    /// <summary>
    /// Verifies that a full TPL Dataflow pipeline initialized via <see cref="PipelineFactory.StartDataflow"/>
    /// propagates progress events across data mapping and terminal stages.
    /// </summary>
    [Fact]
    public async Task StartDataflow_PipelineExecution_EmitsProgressEventsAcrossTopology()
    {
        // Arrange
        TestProgressReporter reporter = new();
        List<int> processedOutputs = [];

        IDataflowPipeline<int> pipeline = PipelineFactory.StartDataflow<int>(
                NullLogger.Instance,
                initialBufferSize: 100,
                progress: reporter)
            .AddDataMapping(x => x * 10)
            .BuildTerminal("CollectTerminal", x => processedOutputs.Add(x));

        await using (pipeline.ConfigureAwait(false))
        {
            // Act
            await pipeline.SendAsync(5);
            pipeline.Complete();
            await pipeline.Completion;
        }

        // Assert
        Assert.Single(processedOutputs);
        Assert.Equal(50, processedOutputs[0]);

        IReadOnlyList<PipelineProgressEvent> events = reporter.Events;
        Assert.True(events.Count >= 4, $"Expected at least 4 progress events, got {events.Count}");

        Assert.Contains(events, e => e.StepName == "Pipeline.DataMapping" && e.State == PipelineStepState.Started);
        Assert.Contains(events, e => e.StepName == "Pipeline.DataMapping" && e.State == PipelineStepState.Completed);
        Assert.Contains(events, e => e.StepName == "Pipeline.Step.CollectTerminal" && e.State == PipelineStepState.Started);
        Assert.Contains(events, e => e.StepName == "Pipeline.Step.CollectTerminal" && e.State == PipelineStepState.Completed);
    }

    private static async IAsyncEnumerable<int> GenerateNumbersAsync(int count)
    {
        for (int i = 1; i <= count; i++)
        {
            await Task.Yield();
            yield return i;
        }
    }

    /// <summary>
    /// Thread-safe in-memory progress collector for testing.
    /// </summary>
    private sealed class TestProgressReporter : IProgress<PipelineProgressEvent>
    {
        private readonly Lock _lock = new();
        private readonly List<PipelineProgressEvent> _events = [];

        public IReadOnlyList<PipelineProgressEvent> Events
        {
            get
            {
                lock (_lock)
                {
                    return [.. _events];
                }
            }
        }

        public void Report(PipelineProgressEvent value)
        {
            lock (_lock)
            {
                _events.Add(value);
            }
        }
    }
}
