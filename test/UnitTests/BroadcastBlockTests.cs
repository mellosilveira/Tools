using MelloSilveiraTools.Core.Pipelines;
using MelloSilveiraTools.Core.Pipelines.Dataflow;
using Microsoft.Extensions.Logging;
using Moq;
using System.Threading.Tasks.Dataflow;

namespace UnitTests;

public class BroadcastBlockTests
{
    private readonly ILogger _logger = Mock.Of<ILogger>();

    private sealed class SampleMultiplierStep(int factor) : IPipelineStep<int, int>
    {
        public string Name => "SampleMultiplier";
        public Task<int> ExecuteAsync(int input, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(input * factor);
        }
    }

    [Fact]
    public async Task AddBroadcastBlock_WithBranchAction_SendsPointsToBothBranches()
    {
        // Arrange
        List<int> branchItems = [];
        List<int> mainItems = [];

        await using IDataflowPipeline<int> pipeline = PipelineFactory.StartDataflow<int>(_logger)
            .AddBroadcastBlock("BranchCollector", branchItems.Add)
            .AddDataMapping(x => x * 10)
            .BuildTerminal("MainCollector", mainItems.Add);

        // Act
        for (int i = 1; i <= 5; i++)
        {
            await pipeline.SendAsync(i);
        }

        pipeline.Complete();
        await pipeline.Completion;

        // Assert
        Assert.Equal([1, 2, 3, 4, 5], branchItems);
        Assert.Equal([10, 20, 30, 40, 50], mainItems);
    }

    [Fact]
    public async Task AddBroadcastBlock_WithTargetBlock_PropagatesItemsAndCompletion()
    {
        // Arrange
        List<int> branchItems = [];
        List<int> mainItems = [];

        ActionBlock<int> branchBlock = new(branchItems.Add);

        await using IDataflowPipeline<int> pipeline = PipelineFactory.StartDataflow<int>(_logger)
            .AddBroadcastBlock(branchBlock)
            .BuildTerminal("MainCollector", mainItems.Add);

        // Act
        for (int i = 10; i <= 15; i++)
        {
            await pipeline.SendAsync(i);
        }

        pipeline.Complete();
        await pipeline.Completion;

        // Assert
        Assert.Equal([10, 11, 12, 13, 14, 15], branchItems);
        Assert.Equal([10, 11, 12, 13, 14, 15], mainItems);
        Assert.True(branchBlock.Completion.IsCompletedSuccessfully);
    }

    [Fact]
    public async Task AddBroadcastStep_WithPipelineStep_ExecutesStepAsSideBranch()
    {
        // Arrange
        List<int> stepResults = [];
        List<int> mainItems = [];

        SampleMultiplierStep sideStep = new(100);

        await using IDataflowPipeline<int> pipeline = PipelineFactory.StartDataflow<int>(_logger)
            .AddBroadcastStep(sideStep)
            .BuildTerminal("MainCollector", mainItems.Add);

        // Act
        for (int i = 1; i <= 3; i++)
        {
            await pipeline.SendAsync(i);
        }

        pipeline.Complete();
        await pipeline.Completion;

        // Assert
        Assert.Equal([1, 2, 3], mainItems);
    }
}
