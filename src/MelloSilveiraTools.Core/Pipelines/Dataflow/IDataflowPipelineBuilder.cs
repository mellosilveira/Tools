using MelloSilveiraTools.Core.Pipelines.Models;
using MelloSilveiraTools.Core.Pipelines.Steps;

namespace MelloSilveiraTools.Core.Pipelines.Dataflow;

/// <summary>
/// Fluent API contract for building asynchronous data processing topologies.
/// Maintains strict input/output type invariance across the execution graph.
/// </summary>
/// <typeparam name="THead">The immutable root input type serving as the ingestion contract.</typeparam>
/// <typeparam name="TTail">The transient terminal state type prior to subsequent step linkage.</typeparam>
public interface IDataflowPipelineBuilder<THead, TTail>
{
    /// <summary>
    /// Configures a synchronous Dead-Letter Queue (DLQ) to capture failed payloads.
    /// </summary>
    /// <param name="errorHandler">The synchronous delegate executed when a payload faults.</param>
    /// <param name="options">Concurrency and buffer options for this step.</param>
    /// <remarks>
    /// Technical Decision: Lightweight abstraction for standard lambda error handling.
    /// Limitation: Synchronous execution blocks the underlying ThreadPool thread during I/O operations.
    /// </remarks>
    IDataflowPipelineBuilder<THead, TTail> WithDeadLetterQueue(Action<FailedPayload> errorHandler, PipelineStepOptions options = default);

    /// <summary>
    /// Configures an asynchronous Dead-Letter Queue (DLQ) to capture failed payloads.
    /// </summary>
    /// <param name="errorHandlerAsync">The asynchronous delegate executed when a payload faults.</param>
    /// <param name="options">Concurrency and buffer options for this step.</param>
    /// <remarks>
    /// Technical Decision: Preferred setup for network-bound failure routing (e.g., Azure Service Bus, SQS).
    /// Limitation: Error handling is terminal. Cannot automatically re-inject payloads into the primary flow.
    /// </remarks>
    IDataflowPipelineBuilder<THead, TTail> WithDeadLetterQueue(Func<FailedPayload, CancellationToken, Task> errorHandlerAsync, PipelineStepOptions options = default);

    /// <summary>
    /// Configures a zero-configuration fault tolerance layer that logs errors to prevent pipeline halting.
    /// </summary>
    /// <param name="options">Concurrency and buffer options for this step.</param>
    /// <remarks>
    /// Technical Decision: Prevents unhandled exceptions from faulting the entire pipeline execution.
    /// Limitation: Payloads are immediately lost from memory and cannot be recovered or retried.
    /// </remarks>
    IDataflowPipelineBuilder<THead, TTail> WithLoggingErrors(PipelineStepOptions options = default);

    /// <summary>
    /// Appends a synchronous data mapping step to the pipeline.
    /// </summary>
    /// <param name="mapFunc">The synchronous function responsible for transforming the payload.</param>
    /// <param name="options">Concurrency and buffer options for this step.</param>
    /// <remarks>
    /// Technical Decision: Elides async state machine overhead. Designed strictly for CPU-bound data mapping (e.g., DTOs).
    /// Limitation: Cannot implement non-blocking retries. Using <see cref="System.Threading.Thread.Sleep"/> causes ThreadPool starvation.
    /// </remarks>
    IDataflowPipelineBuilder<THead, TNextOut> AddDataMapping<TNextOut>(Func<TTail, TNextOut> mapFunc, PipelineStepOptions options = default);

    /// <summary>
    /// Appends an asynchronous data mapping step to the pipeline.
    /// </summary>
    /// <param name="mapFunc">The asynchronous function responsible for transforming the payload.</param>
    /// <param name="options">Concurrency and buffer options for this step.</param>
    /// <remarks>
    /// Technical Decision: Explicitly named for payload transformation via I/O (e.g., external API enrichment).
    /// Limitation: Incurs async state machine allocation. Avoid for pure in-memory mapping.
    /// </remarks>
    IDataflowPipelineBuilder<THead, TNextOut> AddDataMapping<TNextOut>(Func<TTail, CancellationToken, Task<TNextOut>> mapFunc, PipelineStepOptions options = default);

    /// <summary>
    /// Appends a filtering step that evaluates a predicate against each payload. Payloads failing the condition are safely dropped.
    /// </summary>
    /// <param name="predicate">The condition a message must meet to proceed in the pipeline.</param>
    /// <param name="options">Concurrency and buffer options for this step.</param>
    /// <remarks>
    /// Technical Decision: Bypasses common pipeline deadlock risks associated with conditional routing by internally swallowing rejected payloads without violating the 1:1 I/O ratio.
    /// </remarks>
    IDataflowPipelineBuilder<THead, TTail> AddFilterStep(Predicate<TTail> predicate, PipelineStepOptions options = default);

    /// <summary>
    /// Appends a stateful batching step that accumulates messages into an array until the condition evaluates false.
    /// </summary>
    /// <param name="condition">The function comparing previous and current items. Returns true to group them.</param>
    /// <param name="options">Concurrency and buffer options for this step.</param>
    /// <remarks>
    /// Technical Decision: Overcomes static size-based batching limits by utilizing an encapsulated state machine for dynamic grouping.
    /// Limitation: Forces sequential execution (<c>MaxDegreeOfParallelism = 1</c>) to guarantee deterministic state accumulation. 
    /// </remarks>
    IDataflowPipelineBuilder<THead, TTail[]> AddGroupWhileStep(Func<TTail, TTail, bool> condition, PipelineStepOptions options = default);

    /// <summary>
    /// Appends a collection step that aggregates all remaining pipeline payloads into a single array.
    /// </summary>
    /// <param name="options">Concurrency and buffer options for this step.</param>
    IDataflowPipelineBuilder<THead, TTail[]> AddCollectAllStep(PipelineStepOptions options = default);

    /// <summary>
    /// Appends a custom synchronous processing step to the pipeline.
    /// </summary>
    /// <param name="step">The synchronous pipeline step instance to execute.</param>
    /// <param name="options">Concurrency and buffer options for this step.</param>
    IDataflowPipelineBuilder<THead, TNextOut> AddStep<TNextOut>(ISyncPipelineStep<TTail, TNextOut> step, PipelineStepOptions options = default);

    /// <summary>
    /// Appends a custom asynchronous processing step to the pipeline.
    /// </summary>
    /// <param name="step">The asynchronous pipeline step instance to execute.</param>
    /// <param name="options">Concurrency and buffer options for this step.</param>
    IDataflowPipelineBuilder<THead, TNextOut> AddStep<TNextOut>(IAsyncPipelineStep<TTail, TNextOut> step, PipelineStepOptions options = default);

    /// <summary>
    /// Appends a custom asynchronous enumerable streaming step to the pipeline.
    /// </summary>
    /// <param name="step">The asynchronous enumerable pipeline step instance to execute.</param>
    /// <param name="options">Concurrency and buffer options for this step.</param>
    IDataflowPipelineBuilder<THead, TNextOut> AddStep<TNextOut>(IAsyncEnumerablePipelineStep<TTail, TNextOut> step, PipelineStepOptions options = default);

    /// <summary>
    /// Appends an asynchronous forking step that routes payloads to a fallback step if a specific condition is met.
    /// </summary>
    /// <param name="step">The primary asynchronous step to execute.</param>
    /// <param name="fallbackCondition">The condition determining if the fallback step should be used.</param>
    /// <param name="fallbackStep">The fallback asynchronous step to execute if the condition is met.</param>
    /// <param name="options">Concurrency and buffer options for this step.</param>
    IDataflowPipelineBuilder<THead, TNextOut> AddForkingStep<TNextOut>(IAsyncPipelineStep<TTail, TNextOut> step, Func<TNextOut, bool> fallbackCondition, IAsyncPipelineStep<TTail, TNextOut> fallbackStep, PipelineStepOptions options = default);

    /// <summary>
    /// Appends a synchronous forking step that routes payloads to a fallback step if a specific condition is met.
    /// </summary>
    /// <param name="step">The primary synchronous step to execute.</param>
    /// <param name="fallbackCondition">The condition determining if the fallback step should be used.</param>
    /// <param name="fallbackStep">The fallback synchronous step to execute if the condition is met.</param>
    /// <param name="options">Concurrency and buffer options for this step.</param>
    IDataflowPipelineBuilder<THead, TNextOut> AddForkingStep<TNextOut>(ISyncPipelineStep<TTail, TNextOut> step, Func<TNextOut, bool> fallbackCondition, ISyncPipelineStep<TTail, TNextOut> fallbackStep, PipelineStepOptions options = default);

    /// <summary>
    /// Appends a non-mutating broadcast step that acts as a fire-and-forget observer.
    /// </summary>
    /// <param name="step">The asynchronous step to observe the payload.</param>
    /// <param name="cloneFunc">An optional function to clone the payload before broadcasting.</param>
    /// <param name="options">Concurrency and buffer options for this step.</param>
    IDataflowPipelineBuilder<THead, TTail> AddBroadcastStep(IAsyncPipelineStep<TTail> step, Func<TTail, TTail>? cloneFunc = null, PipelineStepOptions options = default);

    /// <summary>
    /// Appends a synchronous terminal step serving as the pipeline sink. Seals the topology.
    /// </summary>
    /// <param name="stepName">The diagnostic name of the terminal step.</param>
    /// <param name="terminalAction">The synchronous delegate to process the final payload.</param>
    /// <param name="options">Concurrency and buffer options for this step.</param>
    /// <remarks>
    /// Technical Decision: Returns <see cref="IDataflowPipeline{THead}"/> to enforce that pipelines cannot have dangling outputs.
    /// Limitation: Terminal steps cannot emit data. The execution graph is sealed after this call.
    /// </remarks>
    IDataflowPipeline<THead> BuildTerminal(string stepName, Action<TTail> terminalAction, PipelineStepOptions options = default);

    /// <summary>
    /// Appends an asynchronous terminal step serving as the pipeline sink. Seals the topology.
    /// </summary>
    /// <param name="stepName">The diagnostic name of the terminal step.</param>
    /// <param name="terminalAction">The asynchronous delegate to process the final payload.</param>
    /// <param name="options">Concurrency and buffer options for this step.</param>
    /// <remarks>
    /// Technical Decision: Designed for async end-of-pipe operations (e.g., database persistence, event publishing).
    /// Limitation: Unhandled exceptions here will drop the fully processed payload unless caught by a previously configured DLQ.
    /// </remarks>
    IDataflowPipeline<THead> BuildTerminal(string stepName, Func<TTail, CancellationToken, Task> terminalAction, PipelineStepOptions options = default);
}