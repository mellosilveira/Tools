namespace MelloSilveiraTools.Core.Pipelines.Dataflow;

/// <summary>
/// Encapsulates a faulted payload, the originating exception, and the 
/// execution context for dead-letter routing.
/// </summary>
public readonly record struct FailedPayload<T>(T Payload, Exception Exception, string StepName);
