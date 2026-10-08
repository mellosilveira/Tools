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
    /// <returns>A configured <see cref="IDataflowPipelineBuilder{THead, TTail}"/> instance with synchronous dead-letter queue routing.</returns>
    /// <remarks>
    /// Technical Decision: Lightweight abstraction for standard lambda error handling.
    /// Limitation: Synchronous execution blocks the underlying ThreadPool thread during I/O operations.
    /// </remarks>
    IDataflowPipelineBuilder<THead, TTail> WithDeadLetterQueue(Action<FailedPayload> errorHandler, PipelineStepOptions options = default);

    /// <summary>
    /// Configures an asynchronous Dead-Letter Queue (DLQ) to capture failed payloads.
    /// </summary>
    /// <param name="errorHandler">The asynchronous delegate executed when a payload faults.</param>
    /// <param name="options">Concurrency and buffer options for this step.</param>
    /// <returns>A configured <see cref="IDataflowPipelineBuilder{THead, TTail}"/> instance with asynchronous dead-letter queue routing.</returns>
    /// <remarks>
    /// Technical Decision: Preferred setup for network-bound failure routing (e.g., Azure Service Bus, SQS).
    /// Limitation: Error handling is terminal. Cannot automatically re-inject payloads into the primary flow.
    /// </remarks>
    IDataflowPipelineBuilder<THead, TTail> WithDeadLetterQueue(Func<FailedPayload, CancellationToken, Task> errorHandler, PipelineStepOptions options = default);

    /// <summary>
    /// Configures a zero-configuration fault tolerance layer that logs errors to prevent pipeline halting.
    /// </summary>
    /// <param name="options">Concurrency and buffer options for this step.</param>
    /// <returns>A configured <see cref="IDataflowPipelineBuilder{THead, TTail}"/> instance routing failed payloads to the logger sink.</returns>
    /// <remarks>
    /// Technical Decision: Prevents unhandled exceptions from faulting the entire pipeline execution.
    /// Limitation: Payloads are immediately lost from memory and cannot be recovered or retried.
    /// </remarks>
    IDataflowPipelineBuilder<THead, TTail> WithLoggingErrors(PipelineStepOptions options = default);

    /// <summary>
    /// Appends a synchronous data mapping step to the pipeline.
    /// </summary>
    /// <typeparam name="TNextOut">The transformed output type yielded by the mapping function.</typeparam>
    /// <param name="mapFunc">The synchronous function responsible for transforming the payload.</param>
    /// <param name="options">Concurrency and buffer options for this step.</param>
    /// <returns>A new <see cref="IDataflowPipelineBuilder{THead, TNextOut}"/> continuing from the transformed output.</returns>
    /// <remarks>
    /// Technical Decision: Elides async state machine overhead. Designed strictly for CPU-bound data mapping (e.g., DTOs).
    /// Limitation: Cannot implement non-blocking retries. Using <see cref="Thread.Sleep(int)"/> causes ThreadPool starvation.
    /// </remarks>
    IDataflowPipelineBuilder<THead, TNextOut> AddDataMapping<TNextOut>(Func<TTail, TNextOut> mapFunc, PipelineStepOptions options = default);

    /// <summary>
    /// Appends a filtering step that evaluates a predicate against each payload. Payloads failing the condition are safely dropped.
    /// </summary>
    /// <param name="predicate">The condition a message must meet to proceed in the pipeline.</param>
    /// <param name="options">Concurrency and buffer options for this step.</param>
    /// <returns>A new <see cref="IDataflowPipelineBuilder{THead, TTail}"/> continuing with payloads satisfying the predicate.</returns>
    /// <remarks>
    /// Technical Decision: Bypasses common pipeline deadlock risks associated with conditional routing by internally swallowing rejected payloads without violating the 1:1 I/O ratio.
    /// </remarks>
    IDataflowPipelineBuilder<THead, TTail> AddFilterStep(Predicate<TTail> predicate, PipelineStepOptions options = default);

    /// <summary>
    /// Forks the pipeline into two parallel branches, processing the same payload concurrently, and joins the results into a Tuple.
    /// </summary>
    /// <typeparam name="TOut1">The output type produced by the first branch.</typeparam>
    /// <typeparam name="TOut2">The output type produced by the second branch.</typeparam>
    /// <param name="branch1">Delegate defining the pipeline topology for the first branch.</param>
    /// <param name="branch2">Delegate defining the pipeline topology for the second branch.</param>
    /// <param name="options">Concurrency and buffer options for the branching blocks.</param>
    /// <returns>A new <see cref="IDataflowPipelineBuilder{THead, TTail}"/> converging both branch results into a joined tuple.</returns>
    /// <remarks>
    /// Technical Decision: Executes true DAG (Directed Acyclic Graph) topologies by utilizing a BroadcastBlock linked to two independent pipeline branches, converging at a JoinBlock.
    /// Constraint: To ensure correct tuple pairing at the JoinBlock, branches must not drop messages (e.g., using Filter without placeholder padding) and must preserve order.
    /// </remarks>
    IDataflowPipelineBuilder<THead, Tuple<TOut1, TOut2>> Fork<TOut1, TOut2>(
        Func<IDataflowPipelineBuilder<THead, TTail>, IDataflowPipelineBuilder<THead, TOut1>> branch1,
        Func<IDataflowPipelineBuilder<THead, TTail>, IDataflowPipelineBuilder<THead, TOut2>> branch2,
        PipelineStepOptions options = default);

    /// <summary>
    /// Forks the pipeline into three parallel branches, processing the same payload concurrently, and joins the results into a Tuple.
    /// </summary>
    /// <typeparam name="TOut1">The output type produced by the first branch.</typeparam>
    /// <typeparam name="TOut2">The output type produced by the second branch.</typeparam>
    /// <typeparam name="TOut3">The output type produced by the third branch.</typeparam>
    /// <param name="branch1">Delegate defining the pipeline topology for the first branch.</param>
    /// <param name="branch2">Delegate defining the pipeline topology for the second branch.</param>
    /// <param name="branch3">Delegate defining the pipeline topology for the third branch.</param>
    /// <param name="options">Concurrency and buffer options for the branching blocks.</param>
    /// <returns>A new <see cref="IDataflowPipelineBuilder{THead, TTail}"/> converging all three branch results into a joined tuple.</returns>
    IDataflowPipelineBuilder<THead, Tuple<TOut1, TOut2, TOut3>> Fork<TOut1, TOut2, TOut3>(
        Func<IDataflowPipelineBuilder<THead, TTail>, IDataflowPipelineBuilder<THead, TOut1>> branch1,
        Func<IDataflowPipelineBuilder<THead, TTail>, IDataflowPipelineBuilder<THead, TOut2>> branch2,
        Func<IDataflowPipelineBuilder<THead, TTail>, IDataflowPipelineBuilder<THead, TOut3>> branch3,
        PipelineStepOptions options = default);

    /// <summary>
    /// Forks the pipeline into two parallel branches using predefined steps, and joins the results into a Tuple.
    /// </summary>
    /// <typeparam name="TOut1">The output type produced by the first step.</typeparam>
    /// <typeparam name="TOut2">The output type produced by the second step.</typeparam>
    /// <param name="branch1Step">The pipeline step instance for the first branch.</param>
    /// <param name="branch2Step">The pipeline step instance for the second branch.</param>
    /// <param name="options">Concurrency and buffer options for the branching blocks.</param>
    /// <returns>A new <see cref="IDataflowPipelineBuilder{THead, TTail}"/> converging both step results into a joined tuple.</returns>
    IDataflowPipelineBuilder<THead, Tuple<TOut1, TOut2>> Fork<TOut1, TOut2>(
        IPipelineStep<TTail, TOut1> branch1Step,
        IPipelineStep<TTail, TOut2> branch2Step,
        PipelineStepOptions options = default);

    /// <summary>
    /// Forks the pipeline into three parallel branches using predefined steps, and joins the results into a Tuple.
    /// </summary>
    /// <typeparam name="TOut1">The output type produced by the first step.</typeparam>
    /// <typeparam name="TOut2">The output type produced by the second step.</typeparam>
    /// <typeparam name="TOut3">The output type produced by the third step.</typeparam>
    /// <param name="branch1Step">The pipeline step instance for the first branch.</param>
    /// <param name="branch2Step">The pipeline step instance for the second branch.</param>
    /// <param name="branch3Step">The pipeline step instance for the third branch.</param>
    /// <param name="options">Concurrency and buffer options for the branching blocks.</param>
    /// <returns>A new <see cref="IDataflowPipelineBuilder{THead, TTail}"/> converging all three step results into a joined tuple.</returns>
    IDataflowPipelineBuilder<THead, Tuple<TOut1, TOut2, TOut3>> Fork<TOut1, TOut2, TOut3>(
        IPipelineStep<TTail, TOut1> branch1Step,
        IPipelineStep<TTail, TOut2> branch2Step,
        IPipelineStep<TTail, TOut3> branch3Step,
        PipelineStepOptions options = default);

    /// <summary>
    /// Appends a stateful batching step that accumulates messages into an array until the condition evaluates false.
    /// </summary>
    /// <param name="groupingCondition">The function comparing previous and current items. Returns true to group them.</param>
    /// <param name="options">Concurrency and buffer options for this step.</param>
    /// <returns>A new <see cref="IDataflowPipelineBuilder{THead, TTail}"/> emitting dynamically grouped arrays.</returns>
    /// <remarks>
    /// Technical Decision: Overcomes static size-based batching limits by utilizing an encapsulated state machine for dynamic grouping.
    /// Limitation: Forces sequential execution (<c>MaxDegreeOfParallelism = 1</c>) to guarantee deterministic state accumulation. 
    /// </remarks>
    IDataflowPipelineBuilder<THead, TTail[]> AddGroupWhileStep(Func<TTail, TTail, bool> groupingCondition, PipelineStepOptions options = default);

    /// <summary>
    /// Appends a collection step that aggregates all remaining pipeline payloads into a single array.
    /// </summary>
    /// <param name="options">Concurrency and buffer options for this step.</param>
    /// <returns>A new <see cref="IDataflowPipelineBuilder{THead, TTail}"/> emitting the collected batch upon completion.</returns>
    IDataflowPipelineBuilder<THead, TTail[]> AddCollectAllStep(PipelineStepOptions options = default);

    /// <summary>
    /// Appends a custom synchronous processing step to the pipeline.
    /// </summary>
    /// <typeparam name="TNextOut">The resultant output type yielded by the synchronous step.</typeparam>
    /// <param name="step">The synchronous pipeline step instance to execute.</param>
    /// <param name="options">Concurrency and buffer options for this step.</param>
    /// <returns>A new <see cref="IDataflowPipelineBuilder{THead, TNextOut}"/> continuing from the step output.</returns>
    IDataflowPipelineBuilder<THead, TNextOut> AddStep<TNextOut>(ISyncPipelineStep<TTail, TNextOut> step, PipelineStepOptions options = default);

    /// <summary>
    /// Appends a custom asynchronous processing step to the pipeline.
    /// </summary>
    /// <typeparam name="TNextOut">The resultant output type yielded by the asynchronous step.</typeparam>
    /// <param name="step">The asynchronous pipeline step instance to execute.</param>
    /// <param name="options">Concurrency and buffer options for this step.</param>
    /// <returns>A new <see cref="IDataflowPipelineBuilder{THead, TNextOut}"/> continuing from the step output.</returns>
    IDataflowPipelineBuilder<THead, TNextOut> AddStep<TNextOut>(IAsyncPipelineStep<TTail, TNextOut> step, PipelineStepOptions options = default);

    /// <summary>
    /// Appends a custom asynchronous enumerable streaming step to the pipeline.
    /// </summary>
    /// <typeparam name="TNextOut">The resultant output element type yielded in the asynchronous sequence.</typeparam>
    /// <param name="step">The asynchronous enumerable pipeline step instance to execute.</param>
    /// <param name="options">Concurrency and buffer options for this step.</param>
    /// <returns>A new <see cref="IDataflowPipelineBuilder{THead, TNextOut}"/> continuing from the stream items.</returns>
    IDataflowPipelineBuilder<THead, TNextOut> AddStep<TNextOut>(IAsyncEnumerablePipelineStep<TTail, TNextOut> step, PipelineStepOptions options = default);

    /// <summary>
    /// Appends an asynchronous forking step that routes payloads to a fallback step if a specific condition is met.
    /// </summary>
    /// <typeparam name="TNextOut">The output type produced by either the primary or fallback step.</typeparam>
    /// <param name="step">The primary asynchronous step to execute.</param>
    /// <param name="fallbackCondition">The condition determining if the fallback step should be used.</param>
    /// <param name="fallbackStep">The fallback asynchronous step to execute if the condition is met.</param>
    /// <param name="options">Concurrency and buffer options for this step.</param>
    /// <returns>A new <see cref="IDataflowPipelineBuilder{THead, TNextOut}"/> continuing from the resolved step output.</returns>
    IDataflowPipelineBuilder<THead, TNextOut> AddForkingStep<TNextOut>(IAsyncPipelineStep<TTail, TNextOut> step, Func<TNextOut, bool> fallbackCondition, IAsyncPipelineStep<TTail, TNextOut> fallbackStep, PipelineStepOptions options = default);

    /// <summary>
    /// Appends a synchronous forking step that routes payloads to a fallback step if a specific condition is met.
    /// </summary>
    /// <typeparam name="TNextOut">The output type produced by either the primary or fallback step.</typeparam>
    /// <param name="step">The primary synchronous step to execute.</param>
    /// <param name="fallbackCondition">The condition determining if the fallback step should be used.</param>
    /// <param name="fallbackStep">The fallback synchronous step to execute if the condition is met.</param>
    /// <param name="options">Concurrency and buffer options for this step.</param>
    /// <returns>A new <see cref="IDataflowPipelineBuilder{THead, TNextOut}"/> continuing from the resolved step output.</returns>
    IDataflowPipelineBuilder<THead, TNextOut> AddForkingStep<TNextOut>(ISyncPipelineStep<TTail, TNextOut> step, Func<TNextOut, bool> fallbackCondition, ISyncPipelineStep<TTail, TNextOut> fallbackStep, PipelineStepOptions options = default);

    /// <summary>
    /// Appends a non-mutating broadcast step that acts as a fire-and-forget observer.
    /// </summary>
    /// <param name="step">The asynchronous step to observe the payload.</param>
    /// <param name="cloneFunc">An optional function to clone the payload before broadcasting.</param>
    /// <param name="options">Concurrency and buffer options for this step.</param>
    /// <returns>A new <see cref="IDataflowPipelineBuilder{THead, TTail}"/> continuing with the unmodified payload.</returns>
    /// <remarks>
    /// Technical Decision: Executes a side-effect asynchronous observer without mutating the downstream pipeline payload.
    /// </remarks>
    IDataflowPipelineBuilder<THead, TTail> AddBroadcastStep(IAsyncPipelineStep<TTail> step, Func<TTail, TTail>? cloneFunc = null, PipelineStepOptions options = default);

    /// <summary>
    /// Appends a non-mutating broadcast step that maps the payload to a different type before observing it.
    /// </summary>
    /// <typeparam name="TOut">The type transformed for the observer step.</typeparam>
    /// <param name="step">The asynchronous step to observe the mapped payload.</param>
    /// <param name="mapFunc">The function to transform the payload for the broadcast step.</param>
    /// <param name="options">Concurrency and buffer options for this step.</param>
    /// <returns>A new <see cref="IDataflowPipelineBuilder{THead, TTail}"/> continuing with the unmodified primary payload.</returns>
    /// <remarks>
    /// Technical Decision: Transforms and routes a payload projection to a side-effect observer without changing the primary pipeline contract.
    /// </remarks>
    IDataflowPipelineBuilder<THead, TTail> AddBroadcastStep<TOut>(IAsyncPipelineStep<TOut> step, Func<TTail, TOut> mapFunc, PipelineStepOptions options = default);

    /// <summary>
    /// Appends a synchronous terminal step serving as the pipeline sink. Seals the topology.
    /// </summary>
    /// <param name="stepName">The diagnostic name of the terminal step.</param>
    /// <param name="terminalAction">The synchronous delegate to process the final payload.</param>
    /// <param name="options">Concurrency and buffer options for this step.</param>
    /// <returns>A sealed <see cref="IDataflowPipeline{THead}"/> execution engine representing the completed graph.</returns>
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
    /// <returns>A sealed <see cref="IDataflowPipeline{THead}"/> execution engine representing the completed graph.</returns>
    /// <remarks>
    /// Technical Decision: Designed for async end-of-pipe operations (e.g., database persistence, event publishing).
    /// Limitation: Unhandled exceptions here will drop the fully processed payload unless caught by a previously configured DLQ.
    /// </remarks>
    IDataflowPipeline<THead> BuildTerminal(string stepName, Func<TTail, CancellationToken, Task> terminalAction, PipelineStepOptions options = default);

    /// <summary>
    /// Appends a terminal step serving as the pipeline sink without requiring an explicit consumer action. Seals the topology.
    /// </summary>
    /// <param name="stepName">The diagnostic name of the terminal step. Defaults to "Terminal".</param>
    /// <param name="options">Concurrency and buffer options for this step.</param>
    /// <returns>A sealed <see cref="IDataflowPipeline{THead}"/> execution engine representing the completed graph.</returns>
    /// <remarks>
    /// Technical Decision: Acts as a semantic drain for unconsumed payloads at the end of a pipeline graph.
    /// </remarks>
    IDataflowPipeline<THead> BuildTerminal(string stepName = "Terminal", PipelineStepOptions options = default);
}