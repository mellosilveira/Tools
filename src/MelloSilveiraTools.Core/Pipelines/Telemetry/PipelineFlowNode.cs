namespace MelloSilveiraTools.Core.Pipelines.Telemetry;

/// <summary>
/// Represents a directional emission point in the pipeline DAG, tracking the immediate upstream
/// named steps and propagating item arrival notifications to downstream step trackers.
/// </summary>
/// <remarks>
/// Architectural Decision: Anonymous plumbing blocks (such as <c>AddDataMapping</c>, <c>AddFilterStep</c>,
/// <c>AddGroupWhileStep</c>, and <c>JoinBlock</c>) create new <see cref="PipelineFlowNode"/> instances that preserve
/// the <see cref="UpstreamStepNames"/> of the last named domain steps. This keeps the user-facing DAG topology clean
/// while accurately propagating real-time item arrival signals (<c>ReceivedItems</c>) across filters, batches, and forks.
/// </remarks>
internal sealed class PipelineFlowNode(IReadOnlyList<string> upstreamStepNames)
{
    private readonly List<Func<CancellationToken, ValueTask>> _itemEmittedHandlers = [];

    /// <summary>
    /// Gets the names of the immediate upstream named steps feeding into this node.
    /// </summary>
    public IReadOnlyList<string> UpstreamStepNames => upstreamStepNames;

    /// <summary>
    /// Subscribes an asynchronous handler invoked whenever an item is emitted from this node.
    /// </summary>
    /// <param name="handler">The asynchronous callback triggered on each item emission.</param>
    public void Subscribe(Func<CancellationToken, ValueTask> handler) => _itemEmittedHandlers.Add(handler);

    /// <summary>
    /// Notifies all downstream subscribers that an item has been emitted from this node.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async ValueTask NotifyItemEmittedAsync(CancellationToken cancellationToken = default)
    {
        for (int i = 0; i < _itemEmittedHandlers.Count; i++)
        {
            await _itemEmittedHandlers[i](cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Creates a converged flow node representing a TPL Dataflow <c>JoinBlock</c> across multiple parallel branches.
    /// Emits a downstream item arrival notification each time all branches have emitted a matching tuple item.
    /// </summary>
    /// <param name="branchNodes">The terminal flow nodes of each parallel branch converging at the JoinBlock.</param>
    /// <returns>A converged <see cref="PipelineFlowNode"/> whose upstream steps are the distinct union of all branches.</returns>
    public static PipelineFlowNode CreateJoined(IReadOnlyList<PipelineFlowNode> branchNodes)
    {
        List<string> mergedUpstreams = [];
        for (int i = 0; i < branchNodes.Count; i++)
        {
            IReadOnlyList<string> branchUpstreams = branchNodes[i].UpstreamStepNames;
            for (int j = 0; j < branchUpstreams.Count; j++)
            {
                string stepName = branchUpstreams[j];
                if (!mergedUpstreams.Contains(stepName, StringComparer.Ordinal))
                    mergedUpstreams.Add(stepName);
            }
        }

        PipelineFlowNode joinedNode = new(mergedUpstreams);
        int[] branchCounts = new int[branchNodes.Count];
        int emittedTuples = 0;
        Lock syncLock = new();

        for (int i = 0; i < branchNodes.Count; i++)
        {
            int branchIndex = i;
            branchNodes[branchIndex].Subscribe(async ct =>
            {
                bool shouldEmit = false;
                lock (syncLock)
                {
                    branchCounts[branchIndex]++;
                    int minEmitted = branchCounts[0];
                    for (int k = 1; k < branchCounts.Length; k++)
                    {
                        if (branchCounts[k] < minEmitted)
                            minEmitted = branchCounts[k];
                    }

                    // A non-greedy JoinBlock emits a tuple only when every branch has produced at least one item for that slot.
                    if (minEmitted > emittedTuples)
                    {
                        emittedTuples = minEmitted;
                        shouldEmit = true;
                    }
                }

                if (shouldEmit)
                    await joinedNode.NotifyItemEmittedAsync(ct).ConfigureAwait(false);
            });
        }

        return joinedNode;
    }
}
