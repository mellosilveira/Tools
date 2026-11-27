namespace MelloSilveiraTools.Core.Pipelines.Models;

/// <summary>
/// Represents the execution lifecycle state of a pipeline step for real-time progress reporting.
/// </summary>
public enum PipelineStepState
{
    /// <summary>
    /// The pipeline step is registered in the execution topology and awaiting input payloads.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// The pipeline step has started processing its first input payload.
    /// </summary>
    Started = 1,

    /// <summary>
    /// The pipeline step is actively running and producing or processing intermediate items.
    /// </summary>
    Running = 2,

    /// <summary>
    /// The pipeline step has completed execution of all items successfully.
    /// </summary>
    Completed = 3,

    /// <summary>
    /// The pipeline step encountered an unhandled error and faulted.
    /// </summary>
    Faulted = 4
}
