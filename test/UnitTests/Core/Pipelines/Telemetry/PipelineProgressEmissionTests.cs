//using MelloSilveiraTools.Core.Pipelines;
//using MelloSilveiraTools.Core.Pipelines.Dataflow;
//using MelloSilveiraTools.Core.Pipelines.Models;
//using MelloSilveiraTools.Core.Pipelines.Notifiers;
//using MelloSilveiraTools.Core.Pipelines.Steps;
//using MelloSilveiraTools.Core.Pipelines.Telemetry;
//using Microsoft.Extensions.Logging.Abstractions;

//namespace UnitTests.Core.Pipelines.Telemetry;

///// <summary>
///// Unit tests verifying real-time emission of <see cref="PipelineProgressEvent"/> notifications,
///// DAG topology correlation (<see cref="PipelineProgressEvent.UpstreamSteps"/>), and item-level progress tracking
///// across synchronous, asynchronous, forking, streaming, and full Dataflow pipeline topologies.
///// </summary>
//public sealed class PipelineProgressEmissionTests
//{
//    private readonly TimeProvider _timeProvider = TimeProvider.System;

//    /// <summary>
//    /// Verifies that a successful synchronous execution emits Started and Completed progress events.
//    /// </summary>
//    [Fact]
//    public async Task HandleExecution_SyncAction_WhenSuccessful_EmitsStartedAndCompletedEvents()
//    {
//        // Arrange
//        TestProgressNotifier notifier = new();
//        Func<int, Task> action = PipelineStepExecutor.HandleExecution<int>(
//            NullLogger.Instance,
//            notifier,
//            _timeProvider,
//            "SyncStep",
//            input => { _ = input * 2; });

//        // Act
//        await action(42);

//        // Assert
//        IReadOnlyList<PipelineProgressEvent> events = notifier.Events;
//        Assert.Equal(2, events.Count);

//        Assert.Equal("SyncStep", events[0].StepName);
//        Assert.Equal(PipelineStepState.Started, events[0].State);
//        Assert.Equal(1, events[0].ReceivedItems);
//        Assert.Equal(0, events[0].ProcessedItems);
//        Assert.Equal(0.0, events[0].ProgressPercentage);

//        Assert.Equal("SyncStep", events[1].StepName);
//        Assert.Equal(PipelineStepState.Completed, events[1].State);
//        Assert.Equal(1, events[1].ReceivedItems);
//        Assert.Equal(1, events[1].ProcessedItems);
//        Assert.Equal(100.0, events[1].ProgressPercentage);
//    }

//    /// <summary>
//    /// Verifies that a failing synchronous execution emits Started and Faulted progress events.
//    /// </summary>
//    [Fact]
//    public async Task HandleExecution_SyncAction_WhenFaulted_EmitsStartedAndFaultedEvents()
//    {
//        // Arrange
//        TestProgressNotifier notifier = new();
//        Func<int, Task> action = PipelineStepExecutor.HandleExecution<int>(
//            NullLogger.Instance,
//            notifier,
//            _timeProvider,
//            "FailingSyncStep",
//            _ => throw new InvalidOperationException("Sync error"));

//        // Act & Assert
//        await Assert.ThrowsAsync<InvalidOperationException>(() => action(10));

//        IReadOnlyList<PipelineProgressEvent> events = notifier.Events;
//        Assert.Equal(2, events.Count);

//        Assert.Equal("FailingSyncStep", events[0].StepName);
//        Assert.Equal(PipelineStepState.Started, events[0].State);

//        Assert.Equal("FailingSyncStep", events[1].StepName);
//        Assert.Equal(PipelineStepState.Faulted, events[1].State);
//        Assert.Contains("Sync error", events[1].Details);
//    }

//    /// <summary>
//    /// Verifies that a successful asynchronous function emits Started and Completed progress events.
//    /// </summary>
//    [Fact]
//    public async Task HandleExecution_AsyncFunc_WhenSuccessful_EmitsStartedAndCompletedEvents()
//    {
//        // Arrange
//        TestProgressNotifier notifier = new();
//        Func<int, Task<string>> asyncFunc = PipelineStepExecutor.HandleExecution<int, string>(
//            NullLogger.Instance,
//            notifier,
//            _timeProvider,
//            "AsyncStep",
//            async (input, ct) =>
//            {
//                await Task.Yield();
//                return $"Value: {input}";
//            });

//        // Act
//        string result = await asyncFunc(99);

//        // Assert
//        Assert.Equal("Value: 99", result);

//        IReadOnlyList<PipelineProgressEvent> events = notifier.Events;
//        Assert.Equal(2, events.Count);

//        Assert.Equal("AsyncStep", events[0].StepName);
//        Assert.Equal(PipelineStepState.Started, events[0].State);

//        Assert.Equal("AsyncStep", events[1].StepName);
//        Assert.Equal(PipelineStepState.Completed, events[1].State);
//        Assert.Equal(100.0, events[1].ProgressPercentage);
//    }

//    /// <summary>
//    /// Verifies that an asynchronous function failing after retries emits Started and Faulted progress events.
//    /// </summary>
//    [Fact]
//    public async Task HandleExecution_AsyncFunc_WhenRetriesExhausted_EmitsStartedAndFaultedEvents()
//    {
//        // Arrange
//        TestProgressNotifier notifier = new();
//        Func<int, Task<string>> asyncFunc = PipelineStepExecutor.HandleExecution<int, string>(
//            NullLogger.Instance,
//            notifier,
//            _timeProvider,
//            "FailingAsyncStep",
//            async (_, ct) =>
//            {
//                await Task.Yield();
//                throw new ApplicationException("Fatal error");
//            });

//        // Act & Assert
//        await Assert.ThrowsAsync<ApplicationException>(() => asyncFunc(1));

//        IReadOnlyList<PipelineProgressEvent> events = notifier.Events;
//        Assert.True(events.Count >= 2);

//        Assert.Equal("FailingAsyncStep", events[0].StepName);
//        Assert.Equal(PipelineStepState.Started, events[0].State);

//        PipelineProgressEvent lastEvent = events[^1];
//        Assert.Equal("FailingAsyncStep", lastEvent.StepName);
//        Assert.Equal(PipelineStepState.Faulted, lastEvent.State);
//        Assert.Contains("Fatal error", lastEvent.Details);
//    }

//    /// <summary>
//    /// Verifies that post-execution conditional forking executes the primary callback first and only executes fallback when the condition is met.
//    /// </summary>
//    [Fact]
//    public async Task HandleExecution_ForkingWithFallbackCondition_ExecutesPrimaryFirstAndFallbackWhenConditionMet()
//    {
//        // Arrange
//        TestProgressNotifier notifier = new();
//        Func<int, Task<int>> forkingFunc = PipelineStepExecutor.HandleExecution<int, int>(
//            NullLogger.Instance,
//            notifier,
//            _timeProvider,
//            "PrimaryStep",
//            "FallbackStep",
//            callback: x => x * 2,
//            fallbackCondition: output => output < 0,
//            fallback: _ => 0);

//        // Act
//        int normalOutput = await forkingFunc(5);
//        int fallbackOutput = await forkingFunc(-3);

//        // Assert
//        Assert.Equal(10, normalOutput);
//        Assert.Equal(0, fallbackOutput);

//        IReadOnlyList<PipelineProgressEvent> events = notifier.Events;
//        Assert.Equal(6, events.Count);
//        Assert.Equal("PrimaryStep", events[0].StepName);
//        Assert.Equal("PrimaryStep", events[1].StepName);
//        Assert.Equal("PrimaryStep", events[2].StepName);
//        Assert.Equal("PrimaryStep", events[3].StepName);
//        Assert.Equal("FallbackStep", events[4].StepName);
//        Assert.Equal("FallbackStep", events[5].StepName);
//    }

//    /// <summary>
//    /// Verifies that an asynchronous streaming enumerable emits Started, Running (with emitted counts), and Completed events.
//    /// </summary>
//    [Fact]
//    public async Task HandleExecution_AsyncEnumerable_EmitsStartedRunningAndCompletedEvents()
//    {
//        // Arrange
//        TestProgressNotifier notifier = new();
//        Func<int, IAsyncEnumerable<int>> streamFunc = PipelineStepExecutor.HandleExecution<int, int>(
//            NullLogger.Instance,
//            notifier,
//            _timeProvider,
//            "StreamStep",
//            (count, _) => GenerateNumbersAsync(count));

//        // Act
//        List<int> results = [];
//        await foreach (int item in streamFunc(5))
//        {
//            results.Add(item);
//        }

//        // Assert
//        Assert.Equal(5, results.Count);

//        IReadOnlyList<PipelineProgressEvent> events = notifier.Events;
//        Assert.True(events.Count >= 3);

//        Assert.Equal("StreamStep", events[0].StepName);
//        Assert.Equal(PipelineStepState.Started, events[0].State);

//        Assert.Contains(events, e => e.State == PipelineStepState.Running && e.Details != null && e.Details.Contains("Items emitted"));

//        PipelineProgressEvent lastEvent = events[^1];
//        Assert.Equal("StreamStep", lastEvent.StepName);
//        Assert.Equal(PipelineStepState.Completed, lastEvent.State);
//        Assert.Contains("Items emitted: 5", lastEvent.Details);
//    }

//    /// <summary>
//    /// Verifies that a full TPL Dataflow pipeline with fan-out and forked branches emits Pending topology events upfront,
//    /// tracks UpstreamSteps accurately across forks, and tracks ReceivedItems vs. ProcessedItems per step.
//    /// </summary>
//    [Fact]
//    public async Task StartDataflow_ForkedDagPipeline_EmitsTopologyAndTracksItemProgress()
//    {
//        // Arrange
//        TestProgressNotifier notifier = new();
//        List<int> processedOutputs = [];

//        FanOutStep fanOutStep = new();
//        MultiplyStep branchAStep = new(factor: 2, name: "BranchA");
//        MultiplyStep branchBStep = new(factor: 3, name: "BranchB");

//        IDataflowPipeline<int> pipeline = PipelineFactory.CreateDataflow<int>(
//                NullLogger.Instance,
//                notifier,
//                TimeProvider.System,
//                initialBufferSize: 100)
//            .AddStep(fanOutStep)
//            .Fork(branchAStep, branchBStep)
//            .AddDataMapping(tuple => tuple.Item1 + tuple.Item2)
//            .BuildTerminal("CollectTerminal", x => processedOutputs.Add(x));

//        await using (pipeline.ConfigureAwait(false))
//        {
//            // Act: send count = 4 so FanOutStep emits 4 items (1, 2, 3, 4) into the forked DAG
//            await pipeline.SendAsync(4);
//            pipeline.Complete();
//            await pipeline.Completion;
//        }

//        // Assert outputs: for i = 1..4, 2*i + 3*i = 5*i -> [5, 10, 15, 20]
//        Assert.Equal([5, 10, 15, 20], processedOutputs);

//        IReadOnlyList<PipelineProgressEvent> events = notifier.Events;

//        // 1. Verify upfront Pending topology events for all 4 DAG steps
//        Assert.Equal(PipelineStepState.Pending, events[0].State);
//        Assert.Equal("FanOutStep", events[0].StepName);
//        Assert.Empty(events[0].UpstreamSteps);

//        Assert.Equal(PipelineStepState.Pending, events[1].State);
//        Assert.Equal("BranchA", events[1].StepName);
//        Assert.Equal(["FanOutStep"], events[1].UpstreamSteps);

//        Assert.Equal(PipelineStepState.Pending, events[2].State);
//        Assert.Equal("BranchB", events[2].StepName);
//        Assert.Equal(["FanOutStep"], events[2].UpstreamSteps);

//        Assert.Equal(PipelineStepState.Pending, events[3].State);
//        Assert.Equal("CollectTerminal", events[3].StepName);
//        Assert.Equal(["BranchA", "BranchB"], events[3].UpstreamSteps);

//        // 2. Verify each step emitted Started once and Completed once with exact item counts
//        Assert.Single(events, e => e.StepName == "FanOutStep" && e.State == PipelineStepState.Started);
//        PipelineProgressEvent fanOutCompleted = Assert.Single(events, e => e.StepName == "FanOutStep" && e.State == PipelineStepState.Completed);
//        Assert.Equal(1, fanOutCompleted.ReceivedItems);
//        Assert.Equal(1, fanOutCompleted.ProcessedItems);
//        Assert.Equal(100.0, fanOutCompleted.ProgressPercentage);

//        Assert.Single(events, e => e.StepName == "BranchA" && e.State == PipelineStepState.Started);
//        PipelineProgressEvent branchACompleted = Assert.Single(events, e => e.StepName == "BranchA" && e.State == PipelineStepState.Completed);
//        Assert.Equal(4, branchACompleted.ReceivedItems);
//        Assert.Equal(4, branchACompleted.ProcessedItems);
//        Assert.Equal(100.0, branchACompleted.ProgressPercentage);

//        Assert.Single(events, e => e.StepName == "BranchB" && e.State == PipelineStepState.Started);
//        PipelineProgressEvent branchBCompleted = Assert.Single(events, e => e.StepName == "BranchB" && e.State == PipelineStepState.Completed);
//        Assert.Equal(4, branchBCompleted.ReceivedItems);
//        Assert.Equal(4, branchBCompleted.ProcessedItems);
//        Assert.Equal(100.0, branchBCompleted.ProgressPercentage);

//        Assert.Single(events, e => e.StepName == "CollectTerminal" && e.State == PipelineStepState.Started);
//        PipelineProgressEvent terminalCompleted = Assert.Single(events, e => e.StepName == "CollectTerminal" && e.State == PipelineStepState.Completed);
//        Assert.Equal(4, terminalCompleted.ReceivedItems);
//        Assert.Equal(4, terminalCompleted.ProcessedItems);
//        Assert.Equal(100.0, terminalCompleted.ProgressPercentage);
//        Assert.Equal(["BranchA", "BranchB"], terminalCompleted.UpstreamSteps);
//    }

//    private static async IAsyncEnumerable<int> GenerateNumbersAsync(int count)
//    {
//        for (int i = 1; i <= count; i++)
//        {
//            await Task.Yield();
//            yield return i;
//        }
//    }

//    [Fact]
//    public async Task StartDataflow_WithCustomPipelineProgressNotificationSettings_AppliesConfiguredThrottling()
//    {
//        // Arrange
//        TestProgressNotifier notifier = new();
//        PipelineProgressNotificationSettings settings = new()
//        {
//            ImmediateNotificationItemLimit = 2,
//            HighVolumeNotificationInterval = 3
//        };

//        MultiplyStep step = new(factor: 2, name: "MultiplyTest");

//        IDataflowPipeline<int> pipeline = PipelineFactory.CreateDataflow<int>(
//                NullLogger.Instance,
//                notifier,
//                TimeProvider.System,
//                notificationSettings: settings,
//                initialBufferSize: 50)
//            .AddStep(step)
//            .BuildTerminal("Terminal", _ => { });

//        await using (pipeline.ConfigureAwait(false))
//        {
//            for (int i = 1; i <= 5; i++)
//            {
//                await pipeline.SendAsync(i);
//            }
//            pipeline.Complete();
//            await pipeline.Completion;
//        }

//        IReadOnlyList<PipelineProgressEvent> events = notifier.Events;
//        Assert.NotEmpty(events);
//        Assert.Contains(events, e => e.StepName == "MultiplyTest" && e.State == PipelineStepState.Completed && e.ProcessedItems == 5);
//    }

//    private sealed class FanOutStep : IAsyncEnumerablePipelineStep<int, int>
//    {
//        public string Name => "FanOutStep";

//        public IAsyncEnumerable<int> ExecuteAsync(int input, CancellationToken cancellationToken = default)
//            => GenerateNumbersAsync(input);

//        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
//    }

//    private sealed class MultiplyStep(int factor, string name) : ISyncPipelineStep<int, int>
//    {
//        public string Name => name;

//        public int Execute(int input) => input * factor;

//        public void Dispose()
//        {
//        }
//    }

//    /// <summary>
//    /// Thread-safe in-memory progress collector for testing.
//    /// </summary>
//    private sealed class TestProgressNotifier : IPipelineProgressNotifier
//    {
//        private readonly Lock _lock = new();
//        private readonly List<PipelineProgressEvent> _events = [];

//        public IReadOnlyList<PipelineProgressEvent> Events
//        {
//            get
//            {
//                lock (_lock)
//                {
//                    return [.. _events];
//                }
//            }
//        }

//        public ValueTask NotifyAsync(PipelineProgressEvent progressEvent, CancellationToken cancellationToken = default)
//        {
//            lock (_lock)
//            {
//                _events.Add(progressEvent);
//            }

//            return ValueTask.CompletedTask;
//        }

//        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
//    }
//}
