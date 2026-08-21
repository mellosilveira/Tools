namespace MelloSilveiraTools.Core.Pipelines;

/// <summary>
/// Defines the atomic contract for an isolated execution boundary within a pipeline topology.
/// Encapsulates a singular domain responsibility, facilitating a strongly-typed state transition 
/// from a predefined input payload to a deterministic output state.
/// </summary>
/// <typeparam name="TIn">The expected input payload type ingested by this execution node.</typeparam>
/// <typeparam name="TOut">The resultant output payload type yielded after successful state mutation.</typeparam>
public interface IStep<TIn, TOut>
{
    /// <summary>
    /// Gets the semantic identifier for this specific execution step.
    /// Required by the pipeline orchestration engines for structured telemetry, 
    /// distributed tracing, and precise fault localization within the execution graph.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Invokes the encapsulated domain logic asynchronously, mapping the ingested state to the resultant output state.
    /// </summary>
    /// <param name="input">The immutable payload propagated from the preceding pipeline node.</param>
    /// <param name="cancellationToken">The cooperative cancellation token injected by the pipeline lifecycle orchestrator.</param>
    /// <returns>A task representing the asynchronous operation, encapsulating the mutated terminal state.</returns>
    Task<TOut> ExecuteAsync(TIn input, CancellationToken cancellationToken = default);
}