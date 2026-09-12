using System.Threading.Tasks.Dataflow;

namespace MelloSilveiraTools.Core.Pipelines.Dataflow;

public static class TaskExtensions
{
    public static Task ContinueWith(this Task task, IDataflowBlock dataflowBlock, Func<Task>? additionalFunc = null) => task.ContinueWith(async task =>
    {
        if (additionalFunc is not null)
            await additionalFunc().ConfigureAwait(false);

        if (task.IsFaulted)
            dataflowBlock.Fault(task.Exception);
        else
            dataflowBlock.Complete();
    }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
}
