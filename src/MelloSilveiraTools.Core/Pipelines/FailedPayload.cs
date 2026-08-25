namespace MelloSilveiraTools.Core.Pipelines;

/// <summary>
/// Encapsulates a faulted payload, the originating exception, and the 
/// execution context for dead-letter routing.
/// </summary>
public readonly record struct FailedPayload<T>(T Payload, Exception Exception, string StepName)
{
    public static implicit operator FailedPayload<object>(FailedPayload<T> payload) => new(payload.Payload, payload.Exception, payload.StepName);
}
