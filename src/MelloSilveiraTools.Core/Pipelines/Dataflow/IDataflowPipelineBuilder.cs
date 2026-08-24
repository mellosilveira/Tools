using System.Threading.Tasks.Dataflow;

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
    /// Configures a Dead-Letter Queue (DLQ) using an existing target block. 
    /// Ideal for advanced scenarios where payloads are routed to a shared buffer or queue block.
    /// </summary>
    IDataflowPipelineBuilder<THead, TTail> WithDeadLetterQueue(ITargetBlock<FailedPayload<TTail>> deadLetterQueueSink);

    /// <summary>
    /// Configures a Dead-Letter Queue (DLQ) using a synchronous callback action. 
    /// Automatically wraps the action in an <see cref="ActionBlock{T}"/> to capture failed payloads seamlessly.
    /// </summary>
    /// <param name="errorHandler">The synchronous delegate executed when a payload faults.</param>
    /// <param name="options">Concurrency and buffer options for the DLQ processing block.</param>
    IDataflowPipelineBuilder<THead, TTail> WithDeadLetterQueue(Action<FailedPayload<TTail>> errorHandler, PipelineStepOptions options = default);

    /// <summary>
    /// Configures a Dead-Letter Queue (DLQ) using an asynchronous callback delegate. 
    /// Automatically wraps the delegate in an <see cref="ActionBlock{T}"/> to capture failed payloads seamlessly.
    /// </summary>
    /// <param name="errorHandlerAsync">The asynchronous delegate executed when a payload faults.</param>
    /// <param name="options">Concurrency and buffer options for the DLQ processing block.</param>
    IDataflowPipelineBuilder<THead, TTail> WithDeadLetterQueue(Func<FailedPayload<TTail>, CancellationToken, Task> errorHandlerAsync, PipelineStepOptions options = default);

    /// <summary>
    /// Appends a TransformBlock bound to an asynchronous delegate.
    /// Optimized for I/O-bound operations or computationally expensive tasks leveraging MaxWorkers > 1.
    /// </summary>
    IDataflowPipelineBuilder<THead, TNextOut> AddStep<TNextOut>(string stepName, Func<TTail, CancellationToken, Task<TNextOut>> stepFunc, PipelineStepOptions options = default);

    /// <summary>
    /// Forks the pipeline topology. Successful payloads are transformed and passed to the next step.
    /// Faulted payloads are intercepted, wrapped in a <see cref="FailedPayload{T}"/>, and processed by a recovery delegate.
    /// </summary>
    IDataflowPipelineBuilder<THead, TNextOut> AddForkingStep<TNextOut>(string stepName, string recoveryStepName, Func<TTail, CancellationToken, Task<TNextOut>> primaryFunc, Func<FailedPayload<TTail>, CancellationToken, Task> recoveryFunc, PipelineStepOptions options = default);

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