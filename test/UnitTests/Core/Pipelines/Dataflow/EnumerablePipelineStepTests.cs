using MelloSilveiraTools.Core.Pipelines;
using MelloSilveiraTools.Core.Pipelines.Dataflow;
using MelloSilveiraTools.Core.Pipelines.Models;
using MelloSilveiraTools.Core.Pipelines.Notifiers;
using MelloSilveiraTools.Core.Pipelines.Steps;
using Microsoft.Extensions.Logging;
using Moq;

namespace UnitTests.Core.Pipelines.Dataflow;

public class EnumerablePipelineStepTests
{
    private readonly ILogger _logger = Mock.Of<ILogger>();
    private readonly IPipelineProgressNotifier _progressNotifier = NullPipelineProgressNotifier.Instance;

    private sealed class SampleExpansionStep(int count) : IEnumerablePipelineStep<int, int>
    {
        public string Name => "SampleExpansion";
        public bool IsDisposed { get; private set; }

        public IEnumerable<int> Execute(int input, CancellationToken cancellationToken = default)
        {
            for (int i = 1; i <= count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return input * 10 + i;
            }
        }

        public void Dispose()
        {
            IsDisposed = true;
        }
    }

    private sealed class FaultingExpansionStep : IEnumerablePipelineStep<int, int>
    {
        public string Name => "FaultingExpansion";

        public IEnumerable<int> Execute(int input, CancellationToken cancellationToken = default)
        {
            if (input < 0)
            {
                throw new InvalidOperationException("Negative input not allowed.");
            }

            yield return input * 2;
        }

        public void Dispose() { }
    }

    [Fact]
    public async Task AddStep_WithEnumerablePipelineStep_ExpandsCollectionCorrectly()
    {
        // Arrange
        List<int> results = [];
        SampleExpansionStep step = new(3);

        await using IDataflowPipeline<int> pipeline = PipelineFactory.StartDataflow<int>(_logger, _progressNotifier)
            .AddStep(step)
            .BuildTerminal("Collector", results.Add);

        // Act
        await pipeline.SendAsync(1);
        await pipeline.SendAsync(2);

        pipeline.Complete();
        await pipeline.Completion;

        // Assert: 1 -> [11, 12, 13]; 2 -> [21, 22, 23]
        Assert.Equal([11, 12, 13, 21, 22, 23], results);
    }

    [Fact]
    public async Task AddStep_WithEnumerablePipelineStep_DisposesStepOnPipelineCompletion()
    {
        // Arrange
        SampleExpansionStep step = new(2);

        IDataflowPipeline<int> pipeline = PipelineFactory.StartDataflow<int>(_logger, _progressNotifier)
            .AddStep(step)
            .BuildTerminal("Collector", _ => { });

        await pipeline.SendAsync(1);
        await pipeline.DisposeAsync();

        // Assert
        Assert.True(step.IsDisposed);
    }

    [Fact]
    public async Task AddStep_WithEnumerablePipelineStep_WhenExceptionOccursAndDlqEnabled_RoutesToDlq()
    {
        // Arrange
        List<int> successResults = [];
        List<FailedPayload> failedPayloads = [];
        FaultingExpansionStep step = new();

        await using IDataflowPipeline<int> pipeline = PipelineFactory.StartDataflow<int>(_logger, _progressNotifier)
            .WithDeadLetterQueue(failedPayloads.Add)
            .AddStep(step)
            .BuildTerminal("Collector", successResults.Add);

        // Act
        await pipeline.SendAsync(5);
        await pipeline.SendAsync(-1);
        await pipeline.SendAsync(10);

        pipeline.Complete();
        await pipeline.Completion;

        // Assert
        Assert.Equal([10, 20], successResults);
        Assert.Single(failedPayloads);
        Assert.Equal(-1, (int)failedPayloads[0].Payload!);
        Assert.IsType<InvalidOperationException>(failedPayloads[0].Exception);
    }

    [Fact]
    public async Task Fork_WithEnumerablePipelineStep_ExpandsBranchesCorrectly()
    {
        // Arrange
        List<Tuple<int, int>> convergedResults = [];
        SampleExpansionStep step1 = new(1);
        SampleExpansionStep step2 = new(1);

        await using IDataflowPipeline<int> pipeline = PipelineFactory.StartDataflow<int>(_logger, _progressNotifier)
            .Fork(step1, step2)
            .BuildTerminal("ConvergenceCollector", convergedResults.Add);

        // Act
        await pipeline.SendAsync(1);
        await pipeline.SendAsync(2);

        pipeline.Complete();
        await pipeline.Completion;

        // Assert: Input 1 produces (11, 11); Input 2 produces (21, 21)
        Assert.Equal([Tuple.Create(11, 11), Tuple.Create(21, 21)], convergedResults);
    }
}
