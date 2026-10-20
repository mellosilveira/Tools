using MelloSilveiraTools.Core.Pipelines;
using MelloSilveiraTools.Core.Pipelines.Dataflow;
using MelloSilveiraTools.Core.Pipelines.Models;
using MelloSilveiraTools.Core.Pipelines.Steps;
using Microsoft.Extensions.Logging;
using Moq;

namespace UnitTests.Core.Pipelines.Dataflow;

/// <summary>
/// Unit tests for <see cref="IDataflowPipelineBuilder{THead, TTail}"/> and <see cref="IDataflowPipeline{TIn}"/>
/// verifying synchronous/asynchronous steps, observer broadcasts, filtering, grouping, forking, DLQ routing, and lifecycle cleanup.
/// </summary>
public sealed class DataflowPipelineBuilderTests
{
    private readonly ILogger _logger = Mock.Of<ILogger>();

    private sealed class MultiplierStep(int factor) : ISyncPipelineStep<int, int>
    {
        public string Name => "MultiplierStep";
        public bool IsDisposed { get; private set; }

        public int Execute(int input)
        {
            return input * factor;
        }

        public void Dispose()
        {
            IsDisposed = true;
        }
    }

    private sealed class AsyncAddStep(int addend) : IAsyncPipelineStep<int, int>
    {
        public string Name => "AsyncAddStep";
        public bool IsDisposed { get; private set; }

        public Task<int> ExecuteAsync(int input, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(input + addend);
        }

        public ValueTask DisposeAsync()
        {
            IsDisposed = true;
            return ValueTask.CompletedTask;
        }
    }

    private sealed class ObserverStep : IAsyncPipelineStep<int>
    {
        public string Name => "ObserverStep";
        public List<int> Observed { get; } = [];

        public Task ExecuteAsync(int input, CancellationToken cancellationToken = default)
        {
            Observed.Add(input);
            return Task.CompletedTask;
        }

        public ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }
    }

    private sealed class FaultingStep : ISyncPipelineStep<int, int>
    {
        public string Name => "FaultingStep";

        public int Execute(int input)
        {
            if (input < 0)
            {
                throw new InvalidOperationException("Negative numbers not supported.");
            }

            return input * 2;
        }

        public void Dispose() { }
    }

    [Fact]
    public async Task AddStep_WithSyncAndAsyncSteps_TransformsPipelineInputs()
    {
        List<int> results = [];
        MultiplierStep syncStep = new(10);
        AsyncAddStep asyncStep = new(3);

        await using IDataflowPipeline<int> pipeline = PipelineFactory.StartDataflow<int>(_logger)
            .AddStep(syncStep)
            .AddStep(asyncStep)
            .BuildTerminal("Collector", results.Add);

        await pipeline.SendAsync(1);
        await pipeline.SendAsync(2);

        pipeline.Complete();
        await pipeline.Completion;

        // 1 * 10 + 3 = 13; 2 * 10 + 3 = 23
        Assert.Equal([13, 23], results);
    }

    [Fact]
    public async Task AddDataMapping_TransformsPayloadType()
    {
        List<string> results = [];

        await using IDataflowPipeline<int> pipeline = PipelineFactory.StartDataflow<int>(_logger)
            .AddDataMapping((int x) => x * 2)
            .AddDataMapping((int x) => $"Val: {x}")
            .BuildTerminal("Collector", results.Add);

        await pipeline.SendAsync(5);
        pipeline.Complete();
        await pipeline.Completion;

        Assert.Single(results);
        Assert.Equal("Val: 10", results[0]);
    }

    [Fact]
    public async Task AddBroadcastStep_ObservesStreamWithoutMutatingMainFlow()
    {
        List<int> mainResults = [];
        ObserverStep observer = new();
        MultiplierStep multiplier = new(100);

        await using IDataflowPipeline<int> pipeline = PipelineFactory.StartDataflow<int>(_logger)
            .AddBroadcastStep(observer)
            .AddStep(multiplier)
            .BuildTerminal("Collector", mainResults.Add);

        await pipeline.SendAsync(1);
        await pipeline.SendAsync(2);

        pipeline.Complete();
        await pipeline.Completion;

        Assert.Equal([1, 2], observer.Observed);
        Assert.Equal([100, 200], mainResults);
    }

    [Fact]
    public async Task AddBroadcastStep_WithMapping_TransformsItemForObserver()
    {
        List<string> mainResults = [];
        ObserverStep observer = new();

        await using IDataflowPipeline<string> pipeline = PipelineFactory.StartDataflow<string>(_logger)
            .AddBroadcastStep(observer, (string s) => int.Parse(s))
            .BuildTerminal("Collector", mainResults.Add);

        await pipeline.SendAsync("42");
        pipeline.Complete();
        await pipeline.Completion;

        Assert.Single(observer.Observed);
        Assert.Equal(42, observer.Observed[0]);
        Assert.Single(mainResults);
        Assert.Equal("42", mainResults[0]);
    }

    [Fact]
    public async Task AddFilterStep_FiltersOutNonMatchingElements()
    {
        List<int> results = [];

        await using IDataflowPipeline<int> pipeline = PipelineFactory.StartDataflow<int>(_logger)
            .AddFilterStep((int x) => x % 2 == 0)
            .BuildTerminal("Collector", results.Add);

        await pipeline.SendAsync(1);
        await pipeline.SendAsync(2);
        await pipeline.SendAsync(3);
        await pipeline.SendAsync(4);

        pipeline.Complete();
        await pipeline.Completion;

        Assert.Equal([2, 4], results);
    }

    [Fact]
    public async Task AddGroupWhileStep_BatchesItemsBasedOnPredicate()
    {
        List<int[]> batches = [];

        await using IDataflowPipeline<int> pipeline = PipelineFactory.StartDataflow<int>(_logger)
            .AddGroupWhileStep((int previous, int current) => current == previous)
            .BuildTerminal("Collector", batches.Add);

        await pipeline.SendAsync(1);
        await pipeline.SendAsync(1);
        await pipeline.SendAsync(2);
        await pipeline.SendAsync(2);
        await pipeline.SendAsync(2);

        pipeline.Complete();
        await pipeline.Completion;

        Assert.Equal(2, batches.Count);
        Assert.Equal([1, 1], batches[0]);
        Assert.Equal([2, 2, 2], batches[1]);
    }

    [Fact]
    public async Task AddCollectAllStep_AggregatesAllItemsIntoSingleArray()
    {
        List<int[]> collected = [];

        await using IDataflowPipeline<int> pipeline = PipelineFactory.StartDataflow<int>(_logger)
            .AddCollectAllStep()
            .BuildTerminal("Collector", collected.Add);

        await pipeline.SendAsync(10);
        await pipeline.SendAsync(20);
        await pipeline.SendAsync(30);

        pipeline.Complete();
        await pipeline.Completion;

        Assert.Single(collected);
        Assert.Equal([10, 20, 30], collected[0]);
    }

    [Fact]
    public async Task Fork_TwoBranches_CombinesResultsIntoTuple()
    {
        List<Tuple<int, string>> results = [];

        await using IDataflowPipeline<int> pipeline = PipelineFactory.StartDataflow<int>(_logger)
            .Fork(
                branch1 => branch1.AddDataMapping((int x) => x * 10),
                branch2 => branch2.AddDataMapping((int x) => $"#{x}"))
            .BuildTerminal("Collector", results.Add);

        await pipeline.SendAsync(3);
        pipeline.Complete();
        await pipeline.Completion;

        Assert.Single(results);
        Assert.Equal(30, results[0].Item1);
        Assert.Equal("#3", results[0].Item2);
    }

    [Fact]
    public async Task Fork_ThreeBranches_CombinesResultsIntoTuple()
    {
        List<Tuple<int, string, bool>> results = [];

        await using IDataflowPipeline<int> pipeline = PipelineFactory.StartDataflow<int>(_logger)
            .Fork(
                branch1 => branch1.AddDataMapping((int x) => x + 1),
                branch2 => branch2.AddDataMapping((int x) => $"val:{x}"),
                branch3 => branch3.AddDataMapping((int x) => x > 0))
            .BuildTerminal("Collector", results.Add);

        await pipeline.SendAsync(5);
        pipeline.Complete();
        await pipeline.Completion;

        Assert.Single(results);
        Assert.Equal(6, results[0].Item1);
        Assert.Equal("val:5", results[0].Item2);
        Assert.True(results[0].Item3);
    }

    [Fact]
    public async Task WithDeadLetterQueue_CatchesExceptionsAndRoutesFailedPayloads()
    {
        List<int> successResults = [];
        List<FailedPayload> failedPayloads = [];
        FaultingStep faultingStep = new();

        await using IDataflowPipeline<int> pipeline = PipelineFactory.StartDataflow<int>(_logger)
            .WithDeadLetterQueue(failedPayloads.Add)
            .AddStep(faultingStep)
            .BuildTerminal("Collector", successResults.Add);

        await pipeline.SendAsync(10);
        await pipeline.SendAsync(-5);
        await pipeline.SendAsync(20);

        pipeline.Complete();
        await pipeline.Completion;

        Assert.Equal([20, 40], successResults);
        Assert.Single(failedPayloads);
        Assert.Equal(-5, (int)failedPayloads[0].Payload!);
        Assert.IsType<InvalidOperationException>(failedPayloads[0].Exception);
    }

    [Fact]
    public async Task DisposeAsync_DisposesAllRegisteredSteps()
    {
        MultiplierStep syncStep = new(2);
        AsyncAddStep asyncStep = new(5);

        IDataflowPipeline<int> pipeline = PipelineFactory.StartDataflow<int>(_logger)
            .AddStep(syncStep)
            .AddStep(asyncStep)
            .BuildTerminal("Collector", _ => { });

        await pipeline.SendAsync(1);
        await pipeline.DisposeAsync();

        Assert.True(syncStep.IsDisposed);
        Assert.True(asyncStep.IsDisposed);
    }
}
