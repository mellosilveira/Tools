using System.Threading.Tasks.Dataflow;

namespace MelloSilveiraTools.Core.ExtensionMethods;

/// <summary>
/// Provides extension methods for asynchronous task operations.
/// </summary>
public static class TaskExtensions
{
    extension(Task task)
    {
        /// <summary>
        /// Propagates the completion or fault state of a task to a target block.
        /// </summary>
        /// <param name="dataflowBlock">The target block receiving the final state.</param>
        /// <param name="additionalFunc">An optional asynchronous function executed prior to state propagation.</param>
        /// <remarks>
        /// Design: Uses <c>TaskContinuationOptions.ExecuteSynchronously</c> to minimize scheduling overhead when cascading completion states across the topology.
        /// Constraint: The <paramref name="additionalFunc"/> must execute efficiently to avoid stalling the continuation thread.
        /// </remarks>
        public Task ContinueWith(IDataflowBlock dataflowBlock, Func<Task>? additionalFunc = null) => task.ContinueWith(async task =>
        {
            if (additionalFunc is not null)
                await additionalFunc().ConfigureAwait(false);

            if (task.IsFaulted)
                dataflowBlock.Fault(task.Exception);
            else
                dataflowBlock.Complete();
        }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
    }
}