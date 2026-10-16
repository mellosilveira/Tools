namespace MelloSilveiraTools.Core.Pipelines.Models;

/// <summary>
/// Represents an immutable real-time notification emitted during the execution of a pipeline step.
/// </summary>
/// <param name="StepName">The unique identifier or logical name of the pipeline step emitting the event.</param>
/// <param name="Description">A human-readable description of the current step activity.</param>
/// <param name="State">The lifecycle state of the step (<see cref="PipelineStepState"/>).</param>
/// <param name="StepIndex">Optional 1-based index of the step in the pipeline topology.</param>
/// <param name="TotalSteps">Optional total number of steps in the pipeline topology.</param>
/// <param name="Detail">Optional diagnostic or contextual detail (e.g., item count, identifier, or error message).</param>
/// <param name="Timestamp">The UTC timestamp when the progress event was generated.</param>
public record PipelineProgressEvent(
    string StepName,
    string Description,
    PipelineStepState State,
    int? StepIndex = null,
    int? TotalSteps = null,
    string? Detail = null,
    DateTimeOffset Timestamp = default);
