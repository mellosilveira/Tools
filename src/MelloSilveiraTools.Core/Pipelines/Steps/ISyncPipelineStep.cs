namespace MelloSilveiraTools.Core.Pipelines.Steps;

/// <summary>
/// Defines the atomic contract for a synchronous execution boundary within a pipeline topology.
/// Encapsulates a singular domain responsibility, facilitating a strongly-typed state transition 
/// from a predefined input payload to a deterministic output state synchronously without Task allocations.
/// </summary>
/// <typeparam name="TIn">The expected input payload type ingested by this execution node.</typeparam>
/// <typeparam name="TOut">The resultant output payload type yielded after successful state mutation.</typeparam>
public interface ISyncPipelineStep<in TIn, out TOut> : IPipelineStep, IDisposable
{
    /// <summary>
    /// Invokes the encapsulated domain logic synchronously, mapping the ingested state to the resultant output state.
    /// </summary>
    /// <param name="input">The immutable payload propagated from the preceding pipeline node.</param>
    /// <returns>The resultant output state produced by the synchronous execution.</returns>
    TOut Execute(TIn input);
}
