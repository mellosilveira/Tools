using MelloSilveiraTools.Core.Pipelines.Models;

namespace MelloSilveiraTools.Core.Pipelines.Fluent;

/// <summary>
/// Represents the finalized execution graph of the fluent pipeline.
/// Exposes a strongly-typed entry point for sequential asynchronous data processing.
/// </summary>
/// <typeparam name="TIn">The immutable root input type.</typeparam>
/// <typeparam name="TOut">The guaranteed terminal output type.</typeparam>
public interface IFluentPipeline<in TIn, TOut>
{
    /// <summary>
    /// Initiates the asynchronous execution of the constructed pipeline graph.
    /// Propagates the root payload through the chronologically ordered step delegates,
    /// managing state transitions and yielding the terminal output upon successful traversal.
    /// </summary>
    /// <param name="input">The initial immutable payload ingested at the pipeline root.</param>
    /// <param name="cancellationToken">The token to cooperatively observe cancellation requests across all pipeline steps.</param>
    /// <returns>A task representing the asynchronous operation, encapsulating the strongly-typed terminal state.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when the initial input payload is null.</exception>
    /// <exception cref="PipelineExecutionException">Thrown when an internal step delegate faults, encapsulating the inner exception and execution context.</exception>
    Task<TOut> ExecuteAsync(TIn input, CancellationToken cancellationToken = default);
}
