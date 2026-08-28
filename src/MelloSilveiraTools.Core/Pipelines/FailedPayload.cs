namespace MelloSilveiraTools.Core.Pipelines;

/// <summary>
/// Encapsulates a faulted payload, the originating exception, and the 
/// execution context for dead-letter routing.
/// </summary>
public readonly record struct FailedPayload<T>(string CallbackName, T Payload, Exception Exception)
{
    public static implicit operator FailedPayload<object?>(FailedPayload<T> payload) => new(payload.CallbackName, payload.Payload, payload.Exception);
}
