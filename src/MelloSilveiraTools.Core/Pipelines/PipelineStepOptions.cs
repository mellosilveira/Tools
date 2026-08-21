using System.Threading.Tasks.Dataflow;

namespace MelloSilveiraTools.Core.Pipelines;

/// <summary>
/// Encapsulates execution configurations for individual TPL Dataflow blocks.
/// Utilizes a readonly record struct to guarantee immutability and zero heap allocation during pipeline construction.
/// </summary>
public readonly record struct PipelineStepOptions
{
    /// <summary>
    /// Parameterless constructor ensures compatibility with struct instantiation rules.
    /// </summary>
    public PipelineStepOptions() { }

    /// <summary>
    /// Static singleton representing the default pipeline step configuration.
    /// </summary>
    public static readonly PipelineStepOptions Default = new();

    /// <summary>
    /// Pre-computed native TPL options instance derived from the default configuration.
    /// </summary>
    public static readonly ExecutionDataflowBlockOptions DataflowDefault = Default.ToDataflowOptions();

    /// <summary>
    /// Configures the 'MaxDegreeOfParallelism' for the underlying block.
    /// - 1: Enforces sequential execution within this step.
    /// - >1: Permits concurrent execution of multiple messages via the ThreadPool.
    /// </summary>
    public int MaxWorkers { get; init; } = 1;

    /// <summary>
    /// Configures 'BoundedCapacity' to enforce backpressure.
    /// Restricts the size of the block's input queue. When the threshold is reached, upstream blocks 
    /// (or the pipeline head) will asynchronously block via 'SendAsync' until capacity frees up.
    /// Defaults to Unbounded, which delegates memory management to the GC (risk of OutOfMemoryException under heavy load).
    /// </summary>
    public int MaxBufferSize { get; init; } = DataflowBlockOptions.Unbounded;

    /// <summary>
    /// Configures 'EnsureOrdered'. 
    /// If true, the block guarantees that messages are emitted downstream in the exact chronological order 
    /// they were ingested, regardless of concurrent execution variations caused by MaxWorkers > 1.
    /// </summary>
    public bool KeepOrder { get; init; } = true;

    /// <summary>
    /// Propagates cancellation down to the individual block level.
    /// If triggered, the block transitions to a faulted/canceled state, rejecting new messages.
    /// </summary>
    public CancellationToken CancellationToken { get; init; } = CancellationToken.None;

    /// <summary>
    /// Projects the abstracted configuration into the native TPL Dataflow options payload.
    /// </summary>
    internal ExecutionDataflowBlockOptions ToDataflowOptions() => new()
    {
        MaxDegreeOfParallelism = MaxWorkers,
        BoundedCapacity = MaxBufferSize,
        EnsureOrdered = KeepOrder,
        CancellationToken = CancellationToken,
        // Optimized for multi-producer thread safety by default
        SingleProducerConstrained = false,
    };
}