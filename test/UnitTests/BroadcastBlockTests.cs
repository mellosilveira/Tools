using MelloSilveiraTools.Core.Pipelines;
using MelloSilveiraTools.Core.Pipelines.Dataflow;
using MelloSilveiraTools.Core.Pipelines.Steps;
using Microsoft.Extensions.Logging;
using Moq;
using System.Runtime.CompilerServices;

namespace UnitTests;

public class BroadcastBlockTests
{
    private readonly ILogger _logger = Mock.Of<ILogger>();

    private sealed class SampleMultiplierStep(int factor) : IAsyncPipelineStep<int, int>
    {
        public string Name => "SampleMultiplier";

        public Task<int> ExecuteAsync(int input, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(input * factor);
        }

        public ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }
    }

    private sealed class SampleSyncMultiplierStep(int factor) : ISyncPipelineStep<int, int>
    {
        public string Name => "SampleSyncMultiplier";

        public int Execute(int input)
        {
            return input * factor;
        }

        public void Dispose() { }
    }

    private sealed class SampleStreamingStep(int count) : IAsyncEnumerablePipelineStep<int, int>
    {
        public string Name => "SampleStreaming";

        public async IAsyncEnumerable<int> ExecuteAsync(int input, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            for (int i = 1; i <= count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return input * 10 + i;
            }
        }

        public ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }
    }

    [Fact]
    public async Task AddStep_WithSyncPipelineStep_TransformsValuesCorrectly()
    {
        // Arrange
        List<int> results = [];
        SampleSyncMultiplierStep syncStep = new(5);

        await using IDataflowPipeline<int> pipeline = PipelineFactory.StartDataflow<int>(_logger)
            .AddStep(syncStep)
            .BuildTerminal("Collector", results.Add);

        // Act
        for (int i = 1; i <= 3; i++)
        {
            await pipeline.SendAsync(i);
        }

        pipeline.Complete();
        await pipeline.Completion;

        // Assert
        Assert.Equal([5, 10, 15], results);
    }

    [Fact]
    public async Task AddStep_WithStreamingPipelineStep_ExpandsStreamCorrectly()
    {
        // Arrange
        List<int> results = [];
        SampleStreamingStep streamStep = new(3);

        await using IDataflowPipeline<int> pipeline = PipelineFactory.StartDataflow<int>(_logger)
            .AddStep(streamStep)
            .BuildTerminal("Collector", results.Add);

        // Act
        await pipeline.SendAsync(1);
        await pipeline.SendAsync(2);

        pipeline.Complete();
        await pipeline.Completion;

        // Assert: Input 1 produces 11, 12, 13; Input 2 produces 21, 22, 23
        Assert.Equal([11, 12, 13, 21, 22, 23], results);
    }
}