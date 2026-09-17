namespace MelloSilveiraTools.Core.Pipelines.Steps;

/// <summary>
/// Defines the atomic contract for a streaming execution boundary within a pipeline topology.
/// Encapsulates a singular domain responsibility, transforming a single input payload into an asynchronous sequence of output items.
/// </summary>
/// <typeparam name="TIn">The expected input payload type ingested by this execution node.</typeparam>
/// <typeparam name="TOut">The resultant output element type yielded in the asynchronous sequence.</typeparam>
public interface IAsyncEnumerablePipelineStep<in TIn, out TOut> : IPipelineStep, IAsyncDisposable
{
    /// <summary>
    /// Invokes the encapsulated domain logic asynchronously, streaming an asynchronous sequence of resultant output items.
    /// </summary>
    /// <param name="input">The immutable payload propagated from the preceding pipeline node.</param>
    /// <param name="cancellationToken">The cooperative cancellation token injected by the pipeline lifecycle orchestrator.</param>
    /// <returns>An asynchronous stream yielding items produced by this execution node.</returns>
    IAsyncEnumerable<TOut> ExecuteAsync(TIn input, CancellationToken cancellationToken = default);
}
