namespace MelloSilveiraTools.Core.Pipelines.Steps;

/// <summary>
/// Defines the atomic contract for an asynchronous execution boundary within a pipeline topology.
/// Encapsulates a singular domain responsibility, facilitating a strongly-typed state transition 
/// from a predefined input payload to a deterministic output state asynchronously.
/// </summary>
/// <typeparam name="TIn">The expected input payload type ingested by this execution node.</typeparam>
/// <typeparam name="TOut">The resultant output payload type yielded after successful state mutation.</typeparam>
public interface IAsyncPipelineStep<in TIn, TOut> : IPipelineStep, IAsyncDisposable
{
    /// <summary>
    /// Invokes the encapsulated domain logic asynchronously, mapping the ingested state to the resultant output state.
    /// </summary>
    /// <param name="input">The immutable payload propagated from the preceding pipeline node.</param>
    /// <param name="cancellationToken">The cooperative cancellation token injected by the pipeline lifecycle orchestrator.</param>
    /// <returns>A task representing the asynchronous operation, encapsulating the mutated terminal state.</returns>
    Task<TOut> ExecuteAsync(TIn input, CancellationToken cancellationToken = default);
}
