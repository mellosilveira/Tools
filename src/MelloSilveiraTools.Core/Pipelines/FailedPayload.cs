namespace MelloSilveiraTools.Core.Pipelines;

/// <summary>
/// Encapsulates a faulted payload, the originating exception, and the execution context for dead-letter routing.
/// </summary>
public readonly record struct FailedPayload(string CallbackName, object? Payload, Exception Exception);

/// <summary>
/// Encapsulates a faulted payload, the originating exception, and the execution context for dead-letter routing.
/// </summary>
public readonly record struct FailedPayload<T>(string CallbackName, T Payload, Exception Exception)
{
    public static implicit operator FailedPayload(FailedPayload<T> payload) => new(payload.CallbackName, payload.Payload, payload.Exception);
    public static implicit operator FailedPayload(FailedPayload<T>? payload)
    {
        if (!payload.HasValue)
            throw new ArgumentNullException(nameof(payload));

        return new(payload.Value.CallbackName, payload.Value.Payload, payload.Value.Exception);
    }
}

public readonly record struct SafeResult<TIn>
{
    private SafeResult(bool success)
    {
        Success = success;
        FailedPayload = null;
    }

    public SafeResult(string callbackName, TIn payload, Exception exception)
    {
        Success = false;
        FailedPayload = new FailedPayload<TIn>(callbackName, payload, exception);
    }

    public static SafeResult<TIn> CreateSuccess() => new(true);

    public bool Success { get; }
    public FailedPayload<TIn>? FailedPayload { get; }
}

public readonly record struct SafeResult<TIn, TOut>
{
    public SafeResult(TOut? output)
    {
        Success = true;
        Output = output;
        FailedPayload = null;
    }

    public SafeResult(string callbackName, TIn payload, Exception exception)
    {
        Success = false;
        Output = default;
        FailedPayload = new FailedPayload<TIn>(callbackName, payload, exception);
    }

    public bool Success { get; }
    public TOut? Output { get; }
    public FailedPayload<TIn>? FailedPayload { get; }

    public static implicit operator SafeResult<TIn, TOut>(TOut output) => new(output);
} 