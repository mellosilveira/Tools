using System.Threading.Tasks.Dataflow;
using MelloSilveiraTools.Core.ExtensionMethods;

namespace UnitTests.Core.ExtensionMethods;

public class TaskExtensionsTests
{
    [Fact]
    public async Task ContinueWith_WhenTaskCompletes_ShouldCompleteDataflowBlock()
    {
        BufferBlock<int> buffer = new();
        Task sourceTask = Task.CompletedTask;

        await sourceTask.ContinueWith(buffer);
        await buffer.Completion;

        Assert.True(buffer.Completion.IsCompletedSuccessfully);
    }

    [Fact]
    public async Task ContinueWith_WhenTaskFaults_ShouldFaultDataflowBlock()
    {
        BufferBlock<int> buffer = new();
        Task sourceTask = Task.FromException(new InvalidOperationException("Source faulted."));

        await sourceTask.ContinueWith(buffer);

        AggregateException ex = await Assert.ThrowsAsync<AggregateException>(async () =>
        {
            await buffer.Completion;
        });

        Assert.IsType<InvalidOperationException>(ex.InnerExceptions[0]);
        Assert.True(buffer.Completion.IsFaulted);
    }
}
