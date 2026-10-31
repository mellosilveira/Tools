namespace MelloSilveiraTools.Core.Pipelines.Steps;

/// <summary>
/// Defines the atomic contract for an enumerable collection execution boundary within a pipeline topology.
/// Encapsulates a singular domain responsibility, transforming a single input payload into an <see cref="IEnumerable{TOut}"/> sequence
/// of output items, intended for one-to-many fan-out transformations (e.g., via TPL Dataflow's <see cref="System.Threading.Tasks.Dataflow.TransformManyBlock{TInput, TOutput}"/>).
/// </summary>
/// <typeparam name="TIn">The expected input payload type ingested by this execution node.</typeparam>
/// <typeparam name="TOut">The resultant output element type yielded in the enumerable collection sequence.</typeparam>
/// <remarks>
/// <para>
/// <b>Pipeline Integration &amp; Dataflow Mapping:</b><br/>
/// This contract mirrors <see cref="IAsyncEnumerablePipelineStep{TIn, TOut}"/> for scenarios where item generation is synchronous,
/// in-memory, or CPU-bound. When integrated with TPL Dataflow, this step is consumed directly by an internal
/// <see cref="System.Threading.Tasks.Dataflow.TransformManyBlock{TInput, TOutput}"/>, which flattens each produced collection into individual downstream messages.
/// </para>
/// <para>
/// <b>Parallelism &amp; Thread Safety:</b><br/>
/// Because the pipeline builder may configure <see cref="System.Threading.Tasks.Dataflow.ExecutionDataflowBlockOptions.MaxDegreeOfParallelism"/>
/// greater than 1, implementations of this step must be either stateless or thread-safe across concurrent invocations of <see cref="Execute"/>.
/// </para>
/// <para>
/// <b>Cancellation &amp; Resource Lifecycle:</b><br/>
/// Cooperative cancellation is propagated via <see cref="CancellationToken"/>. Step implementations generating large or deferred sequences
/// should observe the token during production and iteration. Resource cleanup is bound via <see cref="IDisposable"/>, guaranteed to be invoked
/// during pipeline termination and disposal.
/// </para>
/// </remarks>
public interface IEnumerablePipelineStep<in TIn, out TOut> : IPipelineStep<TIn, TOut>, IDisposable
{
    /// <summary>
    /// Invokes the encapsulated domain logic synchronously, returning an enumerable sequence of resultant output items.
    /// </summary>
    /// <param name="input">The immutable payload propagated from the preceding pipeline node.</param>
    /// <param name="cancellationToken">The cooperative cancellation token injected by the pipeline lifecycle orchestrator.</param>
    /// <returns>An enumerable sequence yielding items produced by this execution node.</returns>
    IEnumerable<TOut> Execute(TIn input, CancellationToken cancellationToken = default);
}
