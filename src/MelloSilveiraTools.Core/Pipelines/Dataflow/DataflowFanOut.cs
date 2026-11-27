using System.Threading.Tasks.Dataflow;

namespace MelloSilveiraTools.Core.Pipelines.Dataflow;

/// <summary>
/// Provides lossless, backpressure-aware fan-out dispatch across multiple downstream TPL Dataflow targets.
/// </summary>
/// <remarks>
/// Why TPL Dataflow's native <see cref="BroadcastBlock{T}"/> is intentionally NOT used:
/// <list type="bullet">
/// <item><description><b>Lossy ("last-value-wins") semantics:</b> <see cref="BroadcastBlock{T}"/> overwrites unconsumed items and silently drops deliveries whenever a downstream target is busy or its bounded buffer is full (<see cref="DataflowMessageStatus.Declined"/>).</description></item>
/// <item><description><b>Broken Fork/Join pairing and observer data loss:</b> In <c>Fork</c> topologies converging into a <see cref="JoinBlock{T1, T2}"/>, dropping an item in one branch breaks tuple pairing and deadlocks the join; in <c>AddBroadcastStep</c>/<c>AddBroadcastFork</c>, slower observers (e.g., disk or database persistence) would silently lose records without raising errors.</description></item>
/// <item><description><b>Missing end-to-end backpressure:</b> Because <see cref="BroadcastBlock{T}"/> advances without waiting for all targets to accept the message, upstream producers are not throttled by slower branches.</description></item>
/// </list>
/// <see cref="DataflowFanOut"/> enforces <c>SendAsync</c> acceptance across all targets, selective cloning for secondary branches (<c>index &gt; 0</c>), and deterministic completion/fault propagation.
/// </remarks>
internal static class DataflowFanOut
{
    /// <summary>
    /// Links <paramref name="source"/> to a sequential dispatcher that delivers every item to all <paramref name="targets"/> with backpressure.
    /// </summary>
    /// <typeparam name="T">The payload type broadcast to all targets.</typeparam>
    /// <param name="source">The upstream source block feeding the fan-out.</param>
    /// <param name="dataFlowOptions">Concurrency and buffer configuration whose <c>BoundedCapacity</c> governs dispatcher backpressure.</param>
    /// <param name="targets">Downstream target blocks that must each receive every payload.</param>
    /// <param name="cloneFunc">Optional cloning function applied only to secondary targets (<c>index &gt; 0</c>), preserving the original instance on the primary path (<c>index == 0</c>).</param>
    /// <param name="cancellationToken">Pipeline-wide cancellation token.</param>
    /// <returns>A completion task that finishes after completion or fault signals have been propagated to every target.</returns>
    public static Task LinkDispatcher<T>(
        ISourceBlock<T> source,
        ExecutionDataflowBlockOptions dataFlowOptions,
        ITargetBlock<T>[] targets,
        Func<T, T>? cloneFunc = null,
        CancellationToken cancellationToken = default)
    {
        // Sequential execution (MaxDegreeOfParallelism = 1) prevents out-of-order delivery across branches.
        ExecutionDataflowBlockOptions dispatcherOptions = new()
        {
            MaxDegreeOfParallelism = 1,
            BoundedCapacity = dataFlowOptions.BoundedCapacity,
            CancellationToken = cancellationToken
        };

        ActionBlock<T> dispatcher = new(async item =>
        {
            Task<bool>[] sendTasks = new Task<bool>[targets.Length];
            for (int i = 0; i < targets.Length; i++)
            {
                T payload = i > 0 && cloneFunc is not null ? cloneFunc(item) : item;
                sendTasks[i] = targets[i].SendAsync(payload, cancellationToken);
            }

            bool[] results = await Task.WhenAll(sendTasks).ConfigureAwait(false);
            foreach (bool accepted in results)
            {
                if (!accepted)
                    throw new InvalidOperationException("Failed to deliver item to a downstream pipeline branch.");
            }
        }, dispatcherOptions);

        Task propagationTask = dispatcher.Completion.ContinueWith(t =>
        {
            foreach (ITargetBlock<T> target in targets)
            {
                if (t.IsFaulted)
                    target.Fault(t.Exception!);
                else if (t.IsCanceled)
                    target.Fault(new TaskCanceledException(t));
                else
                    target.Complete();
            }
        }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);

        source.LinkTo(dispatcher);
        return propagationTask;
    }
}
