namespace MelloSilveiraTools.Core.Pipelines.Models;

/// <summary>
/// Encapsulates a type-erased faulted payload, the originating exception, and the execution context for centralized dead-letter routing.
/// </summary>
/// <remarks>
/// Technical Decision: Acts as a unified, non-generic sink contract. This allows a single Dead-Letter Queue (DLQ) block to aggregate failures from any step in the pipeline, regardless of the intermediate payload types at the point of failure.
/// Limitation: Because the payload is stored as <c>object?</c>, any struct (value type) passed into this record will be boxed, incurring a small heap allocation. Furthermore, strict compile-time type safety for the payload is lost once converted to this type.
/// </remarks>
public readonly record struct FailedPayload(string CallbackName, object? Payload, Exception Exception);

/// <summary>
/// Encapsulates a strongly-typed faulted payload, the originating exception, and the execution context for dead-letter routing.
/// </summary>
/// <remarks>
/// Technical Decision: Maintains strict type safety within the generic boundaries of the pipeline builder. Utilizes <c>readonly record struct</c> to guarantee immutability and ensure zero heap allocations during the fault generation phase.
/// </remarks>
public readonly record struct FailedPayload<T>(string CallbackName, T Payload, Exception Exception)
{
    /// <summary>
    /// Implicitly converts a strongly-typed failed payload into a type-erased failed payload.
    /// </summary>
    /// <remarks>
    /// Technical Decision: Streamlines internal pipeline mechanics. Allows generic pipeline blocks to route their strictly-typed <see cref="FailedPayload{T}"/> directly to the unified <c>ITargetBlock&lt;FailedPayload&gt;</c> DLQ sink without requiring explicit casting or manual mapping delegates in the fluent builder.
    /// </remarks>
    public static implicit operator FailedPayload(FailedPayload<T> payload) => new(payload.CallbackName, payload.Payload, payload.Exception);

    /// <summary>
    /// Implicitly converts a nullable strongly-typed failed payload into a type-erased failed payload.
    /// </summary>
    /// <remarks>
    /// Limitation: Implicit operators that throw exceptions are generally considered an anti-pattern because the cast happens silently in the compiler, meaning the <see cref="ArgumentNullException"/> can surface unexpectedly at runtime. Ensure the source payload is strictly validated before this conversion occurs.
    /// </remarks>
    public static implicit operator FailedPayload(FailedPayload<T>? payload)
    {
        if (!payload.HasValue)
            throw new ArgumentNullException(nameof(payload));

        return new(payload.Value.CallbackName, payload.Value.Payload, payload.Value.Exception);
    }
}