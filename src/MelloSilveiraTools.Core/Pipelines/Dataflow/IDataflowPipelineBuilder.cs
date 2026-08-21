namespace MelloSilveiraTools.Core.Pipelines.Dataflow;

/// <summary>
/// Defines the fluent contract for orchestrating continuous, push-based execution topologies utilizing TPL Dataflow.
/// Facilitates the strictly-typed construction of block linkages, ensuring type invariance at the ingestion root 
/// while safely mapping transient intermediate state transitions across the execution graph.
/// </summary>
/// <typeparam name="THead">The immutable root input type configured at the pipeline head, serving as the ingestion contract.</typeparam>
/// <typeparam name="TTail">The transient terminal state type of the topology prior to subsequent block linkage or sink attachment.</typeparam>
public interface IDataflowPipelineBuilder<THead, TTail>
{
    /// <summary>
    /// Appends a TransformBlock bound to an asynchronous delegate.
    /// Optimized for I/O-bound operations or computationally expensive tasks leveraging MaxWorkers > 1.
    /// </summary>
    IDataflowPipelineBuilder<THead, TNextOut> AddStep<TNextOut>(string stepName, Func<TTail, CancellationToken, Task<TNextOut>> stepFunc, PipelineStepOptions options = default);

    /// <summary>
    /// Appends a TransformBlock bound to a synchronous delegate.
    /// Elides the async state machine allocation entirely, making this highly performant for 
    /// synchronous CPU-bound data mapping operations.
    /// </summary>
    IDataflowPipelineBuilder<THead, TNextOut> AddDataMapping<TNextOut>(string stepName, Func<TTail, TNextOut> mapFunc, PipelineStepOptions options = default);

    /// <summary>
    /// Appends a TransformBlock bound to a synchronous delegate.
    /// Elides the async state machine allocation entirely, making this highly performant for 
    /// synchronous CPU-bound data mapping operations.
    /// </summary>
    IDataflowPipelineBuilder<THead, TNextOut> AddDataMapping<TNextOut>(string stepName, Func<TTail, CancellationToken, Task<TNextOut>> mapFunc, PipelineStepOptions options = default);

    /// <summary>
    /// Appends an ActionBlock to consume the final pipeline output.
    /// Serves as the pipeline sink, linking the final ISourceBlock and returning the execution interface.
    /// </summary>
    IDataflowPipeline<THead> BuildTerminal(string stepName, Action<TTail> terminalAction, PipelineStepOptions options = default);
}