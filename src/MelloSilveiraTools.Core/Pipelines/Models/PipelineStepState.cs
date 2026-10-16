namespace MelloSilveiraTools.Core.Pipelines.Models;

/// <summary>
/// Represents the execution lifecycle state of a pipeline step for real-time progress reporting.
/// </summary>
public enum PipelineStepState
{
    /// <summary>
    /// The pipeline step has started processing an input payload.
    /// </summary>
    Started = 0,

    /// <summary>
    /// The pipeline step is actively running and producing or processing intermediate items.
    /// </summary>
    Running = 1,

    /// <summary>
    /// The pipeline step has completed execution successfully.
    /// </summary>
    Completed = 2,

    /// <summary>
    /// The pipeline step encountered an unhandled error and faulted.
    /// </summary>
    Faulted = 3
}
