using Microsoft.Extensions.Logging;

namespace MelloSilveiraTools.Core.Pipelines.Fluent;

/// <summary>
/// Defines the contract for constructing a strongly-typed, sequential execution graph.
/// Facilitates compile-time type safety across state transitions while abstracting 
/// the underlying type-erased delegate collection.
/// </summary>
/// <typeparam name="TInitialIn">The immutable root input type bound at the pipeline's inception.</typeparam>
/// <typeparam name="TCurrentOut">The current terminal state type of the execution graph prior to the next step linkage.</typeparam>
public interface IFluentPipelineBuilder<TInitialIn, TCurrentOut>
{
    /// <summary>
    /// Appends an asynchronous execution delegate to the pipeline topology, 
    /// mutating the terminal state type of the builder graph.
    /// </summary>
    /// <typeparam name="TNextOut">The resultant state type emitted by the appended delegate.</typeparam>
    /// <param name="stepName">The semantic identifier utilized for structured telemetry and fault localization.</param>
    /// <param name="stepFunc">The asynchronous delegate encapsulating the execution logic and state mutation.</param>
    /// <returns>A new builder instance binding the root input to the newly mutated terminal state.</returns>
    IFluentPipelineBuilder<TInitialIn, TNextOut> AddStep<TNextOut>(string stepName, Func<TCurrentOut, CancellationToken, Task<TNextOut>> stepFunc);

    /// <summary>
    /// Compiles the configured execution graph into an immutable, executable pipeline instance.
    /// </summary>
    /// <param name="logger">An optional structured logging provider injected into the execution engine for telemetry and state tracking.</param>
    /// <returns>The finalized pipeline instance capable of processing the sequential state transitions.</returns>
    IFluentPipeline<TInitialIn, TCurrentOut> Build(ILogger? logger = null);
}