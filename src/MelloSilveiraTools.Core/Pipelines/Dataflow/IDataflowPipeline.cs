namespace MelloSilveiraTools.Core.Pipelines.Dataflow;

/// <summary>
/// Exposes the operational ingestion surface of the continuous Dataflow pipeline.
/// Implements IAsyncDisposable to ensure graceful teardown of buffers upon context destruction.
/// </summary>
public interface IDataflowPipeline<in TIn> : IAsyncDisposable
{
    /// <summary>
    /// Asynchronously posts a message to the pipeline's head block.
    /// Will yield the calling thread if the head block's BoundedCapacity is exhausted.
    /// </summary>
    Task<bool> SendAsync(TIn item, CancellationToken cancellationToken = default);

    /// <summary>
    /// Signals the head block to cease accepting new messages and begin propagating the completion 
    /// signal downstream as internal queues drain.
    /// </summary>
    void Complete();

    /// <summary>
    /// A localized Task representing the terminal state of the pipeline network.
    /// Awaiting this ensures all propagated messages have been fully consumed by the terminal action block.
    /// </summary>
    Task Completion { get; }
}