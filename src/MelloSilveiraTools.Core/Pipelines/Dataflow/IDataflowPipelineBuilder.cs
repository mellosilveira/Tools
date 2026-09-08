using MelloSilveiraTools.Core.Pipelines.Models;
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
    /// <remarks>
    /// Technical Decision: Accepting an <see cref="ITargetBlock{T}"/> allows multiple discrete pipeline branches to share a single centralized DLQ block for aggregated error handling.
    /// Limitation: Faulted payloads routed here are permanently diverted from the primary execution graph. If the DLQ block's buffer fills up and enforces backpressure, it will stall the upstream blocks that are trying to offload errors.
    /// </remarks>
    IDataflowPipelineBuilder<THead, TTail> WithDeadLetterQueue(ITargetBlock<FailedPayload> deadLetterQueueSink);

    /// <summary>
    /// Configures a Dead-Letter Queue (DLQ) using a synchronous callback action. 
    /// Automatically wraps the action in an <see cref="ActionBlock{T}"/> to capture failed payloads seamlessly.
    /// </summary>
    /// <param name="errorHandler">The synchronous delegate executed when a payload faults.</param>
    /// <param name="options">Concurrency and buffer options for the DLQ processing block.</param>
    /// <remarks>
    /// Technical Decision: Provides a lightweight abstraction over TPL Dataflow blocks for developers who just want to write a standard C# lambda for error handling.
    /// Limitation: Because it executes synchronously, any blocking I/O inside the <paramref name="errorHandler"/> (e.g., writing to a database) will block the underlying ThreadPool thread assigned to the DLQ ActionBlock.
    /// </remarks>
    IDataflowPipelineBuilder<THead, TTail> WithDeadLetterQueue(Action<FailedPayload> errorHandler, PipelineStepOptions options = default);

    /// <summary>
    /// Configures a Dead-Letter Queue (DLQ) using an asynchronous callback delegate. 
    /// Automatically wraps the delegate in an <see cref="ActionBlock{T}"/> to capture failed payloads seamlessly.
    /// </summary>
    /// <param name="errorHandlerAsync">The asynchronous delegate executed when a payload faults.</param>
    /// <param name="options">Concurrency and buffer options for the DLQ processing block.</param>
    /// <remarks>
    /// Technical Decision: The preferred DLQ setup for handling network-bound failure routing (e.g., sending failed payloads to an Azure Service Bus or SQS queue) as it leverages async I/O.
    /// Limitation: Like all DLQ routing in this topology, the error handling is terminal for the specific message. It does not provide a mechanism to automatically re-inject the payload back into the primary flow after DLQ processing.
    /// </remarks>
    IDataflowPipelineBuilder<THead, TTail> WithDeadLetterQueue(Func<FailedPayload, CancellationToken, Task> errorHandlerAsync, PipelineStepOptions options = default);

    /// <summary>
    /// Configures a Dead-Letter Queue (DLQ) for only logging the errors. 
    /// </summary>
    /// <param name="options">Concurrency and buffer options for the DLQ processing block.</param>
    /// <remarks>
    /// Technical Decision: Implements a zero-configuration fault tolerance layer. Unhandled exceptions are logged, preventing the default TPL behavior which would fault the entire pipeline block and halt all message processing.
    /// Limitation: The failed payload is strictly written to the standard <see cref="Microsoft.Extensions.Logging.ILogger"/> and is immediately lost from memory. It cannot be recovered, programmatically inspected, or retried later.
    /// </remarks>
    IDataflowPipelineBuilder<THead, TTail> WithLoggingErrors(PipelineStepOptions options = default);

    /// <summary>
    /// Appends a TransformBlock bound to a synchronous delegate.
    /// </summary>
    /// <remarks>
    /// Technical Decision: Explicitly elides the async state machine allocation entirely. This is highly performant and strictly designed for pure CPU-bound data mapping operations (e.g., mapping DTOs).
    /// Limitation: Does not support exponential backoff retries. Implementing retries in a synchronous block requires <see cref="System.Threading.Thread.Sleep"/>, which would cause severe ThreadPool starvation.
    /// </remarks>
    IDataflowPipelineBuilder<THead, TNextOut> AddDataMapping<TNextOut>(Func<TTail, TNextOut> mapFunc, PipelineStepOptions options = default);

    /// <summary>
    /// Appends a TransformBlock bound to an asynchronous delegate for mapping operations.
    /// </summary>
    /// <remarks>
    /// Technical Decision: Separated from <see cref="AddStep{TNextOut}"/> purely for semantic clarity in fluent chains, indicating that the core purpose of the delegate is payload transformation via I/O (e.g., enriching data via an external API).
    /// Limitation: Incurs standard Task allocation and async state machine overhead. Do not use for pure in-memory object mapping where the synchronous overload would suffice.
    /// </remarks>
    IDataflowPipelineBuilder<THead, TNextOut> AddDataMapping<TNextOut>(Func<TTail, CancellationToken, Task<TNextOut>> mapFunc, PipelineStepOptions options = default);

    /// <summary>
    /// Appends a TransformBlock bound to an asynchronous delegate.
    /// Optimized for I/O-bound operations or computationally expensive tasks leveraging MaxWorkers > 1.
    /// </summary>
    /// <remarks>
    /// Technical Decision: Enforces a string <paramref name="stepName"/> parameter to guarantee that OpenTelemetry spans and logs have a consistent, queryable identifier across distributed tracing systems.
    /// Limitation: Implements a strict 1:1 input-to-output ratio. A message must return a result to continue down the pipeline. To drop a message, it must return <c>null</c> and rely on a downstream filter to ignore it.
    /// </remarks>
    IDataflowPipelineBuilder<THead, TNextOut> AddStep<TNextOut>(string stepName, Func<TTail, CancellationToken, Task<TNextOut>> stepFunc, PipelineStepOptions options = default);

    /// <summary>
    /// Appends a TransformBlock bound to a synchronous delegate with explicit naming for telemetry and tracing.
    /// Optimized for CPU-bound computations, eliminating Task allocations.
    /// </summary>
    /// <typeparam name="TNextOut">The resultant state type emitted by the appended synchronous delegate.</typeparam>
    /// <param name="stepName">The semantic identifier utilized for structured telemetry and fault localization.</param>
    /// <param name="stepFunc">The synchronous delegate encapsulating the execution logic and state mutation.</param>
    /// <param name="options">Options configuring buffer sizes and cancellation tokens for the block.</param>
    /// <returns>A new builder instance representing the next pipeline stage.</returns>
    IDataflowPipelineBuilder<THead, TNextOut> AddStep<TNextOut>(string stepName, Func<TTail, TNextOut> stepFunc, PipelineStepOptions options = default);

    /// <summary>
    /// Appends a streaming transformation step that expands each ingested payload into an asynchronous sequence of output items,
    /// propagating each yielded item individually downstream through the pipeline execution graph.
    /// </summary>
    /// <typeparam name="TNextOut">The resultant element type yielded by the asynchronous stream.</typeparam>
    /// <param name="stepName">The semantic identifier utilized for structured telemetry and fault localization.</param>
    /// <param name="stepFunc">The asynchronous streaming delegate yielding an asynchronous sequence of items.</param>
    /// <param name="options">Options configuring buffer sizes and cancellation tokens for the step.</param>
    /// <returns>A new builder instance representing the next pipeline stage yielding individual items.</returns>
    IDataflowPipelineBuilder<THead, TNextOut> AddStep<TNextOut>(string stepName, Func<TTail, CancellationToken, IAsyncEnumerable<TNextOut>> stepFunc, PipelineStepOptions options = default);

    /// <summary>
    /// Appends a conditional branching step evaluating the output payload after the primary execution completes.
    /// </summary>
    /// <remarks>
    /// Technical Decision: Allows execution topologies to recover or alternative route based on business validation rules evaluated against the *result* of the primary operation.
    /// Limitation: The primary <paramref name="stepFunc"/> executes fully before the <paramref name="fallbackCondition"/> is evaluated. If the primary step applies external side effects (e.g., mutating a database row), those side effects cannot be rolled back by the pipeline engine if the fallback triggers.
    /// </remarks>
    IDataflowPipelineBuilder<THead, TNextOut> AddForkingStep<TNextOut>(string stepName, string fallbackStepName, Func<TTail, CancellationToken, Task<TNextOut>> stepFunc, Func<TNextOut, bool> fallbackCondition, Func<TTail, CancellationToken, Task<TNextOut>> fallbackStep, PipelineStepOptions options = default);

    /// <summary>
    /// Appends a conditional branching step evaluating the input payload prior to execution.
    /// </summary>
    /// <remarks>
    /// Technical Decision: Provides a short-circuit routing mechanism. If the input meets the fallback condition, the primary execution is bypassed entirely, saving compute and I/O resources.
    /// Limitation: The branch replacement is absolute. The output of the <paramref name="fallbackStep"/> strictly replaces the primary step's output and continues down the identical main pipeline track. This does not create a bifurcated, parallel pipeline graph.
    /// </remarks>
    IDataflowPipelineBuilder<THead, TNextOut> AddForkingStep<TNextOut>(string stepName, string fallbackStepName, Func<TTail, CancellationToken, Task<TNextOut>> stepFunc, Func<TTail, bool> fallbackCondition, Func<TTail, CancellationToken, Task<TNextOut>> fallbackStep, PipelineStepOptions options = default);

    /// <summary>
    /// Appends a BatchBlock to aggregate messages into arrays based on a specified batch size.
    /// </summary>
    /// <remarks>
    /// Technical Decision: Leverages the native TPL <see cref="BatchBlock{T}"/> to optimize downstream I/O-bound operations, such as bulk SQL inserts or batched HTTP requests, reducing network round-trips.
    /// Limitation: Messages are held in memory until the exact <paramref name="batchSize"/> is reached. If the upstream stops emitting messages (a "trickle" scenario), the partial batch remains stuck in limbo indefinitely unless the pipeline is explicitly signaled via <see cref="IDataflowPipeline{TIn}.Complete"/> to force-flush.
    /// </remarks>
    IDataflowPipelineBuilder<THead, TTail[]> AddBatchStep(int batchSize, PipelineStepOptions options = default);

    /// <summary>
    /// Appends a step that evaluates a predicate against each payload. Payloads that evaluate to false are safely dropped from the pipeline.
    /// </summary>
    /// <param name="predicate">The condition a message must meet to proceed.</param>
    /// <param name="options"></param>
    /// <remarks>
    /// Technical Decision: Bypasses the buffer exhaustion and deadlock risks associated with predicate-based <see cref="DataflowLinkOptions"/>[cite: 4]. By yielding an empty array internally via a TransformManyBlock, the message is gracefully consumed and discarded without violating the strict 1:1 input-to-output pipeline ratio[cite: 3].
    /// </remarks>
    IDataflowPipelineBuilder<THead, TTail> AddFilterStep(Predicate<TTail> predicate, PipelineStepOptions options = default);

    /// <summary>
    /// Appends a stateful grouping block that accumulates messages into an array until the provided condition evaluates to false.
    /// </summary>
    /// <param name="condition">A function comparing the previously buffered item to the current item. Returns true if they belong in the same group.</param>
    /// <param name="options"></param>
    /// <remarks>
    /// Technical Decision: Overcomes the limitation of the native TPL BatchBlock which strictly holds messages until a numeric count is met[cite: 3]. This utilizes an encapsulated state machine.
    /// Limitation: Forces <c>MaxDegreeOfParallelism = 1</c> to guarantee deterministic sequential state accumulation. 
    /// </remarks>
    IDataflowPipelineBuilder<THead, TTail[]> AddGroupWhileStep(Func<TTail, TTail, bool> condition, PipelineStepOptions options = default);

    /// <summary>
    /// Appends a BroadcastBlock to the execution topology, duplicating emitted payloads across a side-branch target block
    /// while propagating the primary data stream downstream for continued pipeline orchestration.
    /// </summary>
    /// <param name="branchTarget">The independent consumer or branch target block receiving broadcasted messages.</param>
    /// <param name="cloneFunc">An optional function used to clone each payload before broadcasting. If null, the payload instance reference is shared.</param>
    /// <param name="options">Options configuring buffer sizes and cancellation tokens for the broadcast block.</param>
    /// <remarks>
    /// Technical Decision: In TPL Dataflow, a BroadcastBlock broadcasts each incoming message to all linked targets.
    /// Linking a side target and returning the broadcast block as the tail block allows both the side branch and downstream 
    /// pipeline stages to concurrently ingest every message without diverting or dropping items.
    /// </remarks>
    IDataflowPipelineBuilder<THead, TTail> AddBroadcastBlock(ITargetBlock<TTail> branchTarget, Func<TTail, TTail>? cloneFunc = null, PipelineStepOptions options = default);

    /// <summary>
    /// Appends a BroadcastBlock to the execution topology, duplicating emitted payloads across multiple side-branch target blocks
    /// while propagating the primary data stream downstream for continued pipeline orchestration.
    /// </summary>
    /// <param name="branchTargets">The independent consumers or branch target blocks receiving broadcasted messages.</param>
    /// <param name="cloneFunc">An optional function used to clone each payload before broadcasting. If null, the payload instance reference is shared.</param>
    /// <param name="options">Options configuring buffer sizes and cancellation tokens for the broadcast block.</param>
    IDataflowPipelineBuilder<THead, TTail> AddBroadcastBlock(IEnumerable<ITargetBlock<TTail>> branchTargets, Func<TTail, TTail>? cloneFunc = null, PipelineStepOptions options = default);

    /// <summary>
    /// Appends a BroadcastBlock to the execution topology executing an asynchronous side-branch action for every payload,
    /// while propagating the primary data stream downstream for continued pipeline orchestration.
    /// </summary>
    /// <param name="stepName">Semantic name of the side-branch step for telemetry and distributed tracing.</param>
    /// <param name="branchAction">The asynchronous action to execute for each broadcasted message.</param>
    /// <param name="cloneFunc">An optional function used to clone each payload before broadcasting. If null, the payload instance reference is shared.</param>
    /// <param name="options">Options configuring concurrency, buffer sizes, and cancellation tokens.</param>
    IDataflowPipelineBuilder<THead, TTail> AddBroadcastBlock(string stepName, Func<TTail, CancellationToken, Task> branchAction, Func<TTail, TTail>? cloneFunc = null, PipelineStepOptions options = default);

    /// <summary>
    /// Appends a BroadcastBlock to the execution topology executing a synchronous side-branch action for every payload,
    /// while propagating the primary data stream downstream for continued pipeline orchestration.
    /// </summary>
    /// <param name="stepName">Semantic name of the side-branch step for telemetry and distributed tracing.</param>
    /// <param name="branchAction">The synchronous action to execute for each broadcasted message.</param>
    /// <param name="cloneFunc">An optional function used to clone each payload before broadcasting. If null, the payload instance reference is shared.</param>
    /// <param name="options">Options configuring concurrency, buffer sizes, and cancellation tokens.</param>
    IDataflowPipelineBuilder<THead, TTail> AddBroadcastBlock(string stepName, Action<TTail> branchAction, Func<TTail, TTail>? cloneFunc = null, PipelineStepOptions options = default);

    /// <summary>
    /// Appends a synchronous ActionBlock to consume the final pipeline output.
    /// Serves as the pipeline sink, linking the final ISourceBlock and returning the execution interface.
    /// </summary>
    /// <remarks>
    /// Technical Decision: Caps the builder pattern by returning the concrete <see cref="IDataflowPipeline{THead}"/> interface rather than the builder. This strongly enforces that pipelines cannot have dangling outputs.
    /// Limitation: Terminal blocks cannot emit data. Once <see cref="BuildTerminal(string, Action{TTail}, PipelineStepOptions)"/> is called, the execution graph is sealed and no further blocks can be appended.
    /// </remarks>
    IDataflowPipeline<THead> BuildTerminal(string stepName, Action<TTail> terminalAction, PipelineStepOptions options = default);

    /// <summary>
    /// Appends an asynchronous ActionBlock to consume the final pipeline output.
    /// </summary>
    /// <remarks>
    /// Technical Decision: Designed for the most common end-of-pipe scenarios, such as persisting the final transformed state to a database or publishing a completed event to a message broker.
    /// Limitation: Any unhandled exceptions at this terminal step that are not caught by a Dead-Letter Queue configuration will still fault this final block, potentially dropping the fully processed payload right at the finish line.
    /// </remarks>
    IDataflowPipeline<THead> BuildTerminal(string stepName, Func<TTail, CancellationToken, Task> terminalAction, PipelineStepOptions options = default);
}