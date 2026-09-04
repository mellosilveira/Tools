namespace MelloSilveiraTools.Core.Pipelines;

/// <summary>
/// Encapsulates the execution outcome of a terminal pipeline step, isolating failures to prevent graph collapse.
/// </summary>
/// <remarks>
/// Technical Decision: Used internally by the pipeline builder to wrap terminal actions (sinks). 
/// By catching exceptions and returning this struct instead of throwing, the pipeline prevents the native TPL Dataflow 
/// behavior that would otherwise transition the block to a faulted state and drop all pending messages.
/// Limitation: Designed exclusively for steps that do not produce an output. It strictly represents a binary state of success or failure.
/// </remarks>
public readonly record struct SafeResult<TIn>
{
    private SafeResult(bool success)
    {
        Success = success;
        FailedPayload = null;
    }

    /// <summary>
    /// Constructs a failed result containing the original payload and the caught exception.
    /// </summary>
    public SafeResult(string callbackName, TIn payload, Exception exception)
    {
        Success = false;
        FailedPayload = new FailedPayload<TIn>(callbackName, payload, exception);
    }

    /// <summary>
    /// Instantiates a successful result marker.
    /// </summary>
    public static SafeResult<TIn> CreateSuccess() => new(true);

    public bool Success { get; }
    public FailedPayload<TIn>? FailedPayload { get; }
}

/// <summary>
/// Encapsulates the execution outcome of a transitional pipeline step, carrying either the mapped output or the failure context.
/// </summary>
/// <remarks>
/// Technical Decision: Acts as the core enabling structure for the builder's <c>AddSafeStep</c> bifurcation logic. 
/// It allows a single <c>TransformBlock</c> to emit a unified wrapper type, which is then routed by downstream 
/// predicates to either the next processing step (if successful) or the Dead-Letter Queue (if failed).
/// Limitation: Both the <c>Output</c> and <c>FailedPayload</c> properties allocate memory footprints even when unused. 
/// In highly constrained memory environments with massive throughput, the size of this struct could impact L1/L2 cache locality.
/// </remarks>
public readonly record struct SafeResult<TIn, TOut>
{
    /// <summary>
    /// Constructs a successful result wrapping the mapped output.
    /// </summary>
    public SafeResult(TOut? output)
    {
        Success = true;
        Output = output;
        FailedPayload = null;
    }

    /// <summary>
    /// Constructs a failed result containing the original input payload and the caught exception.
    /// </summary>
    public SafeResult(string callbackName, TIn payload, Exception exception)
    {
        Success = false;
        Output = default;
        FailedPayload = new FailedPayload<TIn>(callbackName, payload, exception);
    }

    public bool Success { get; }
    public TOut? Output { get; }
    public FailedPayload<TIn>? FailedPayload { get; }

    /// <summary>
    /// Implicitly converts a raw mapped output into a successful <see cref="SafeResult{TIn, TOut}"/>.
    /// </summary>
    /// <remarks>
    /// Technical Decision: Streamlines internal telemetry wrappers by allowing them to return the raw <typeparamref name="TOut"/> 
    /// from successful function executions, relying on the compiler to automatically box it into this safe envelope without manual instantiation.
    /// </remarks>
    public static implicit operator SafeResult<TIn, TOut>(TOut output) => new(output);
}