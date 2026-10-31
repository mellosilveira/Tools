using MelloSilveiraTools.Core.Pipelines.Models;
using MelloSilveiraTools.Database.RelationalDatabase.Attributes;
using MelloSilveiraTools.Database.RelationalDatabase.Models.Entities;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Models;

/// <summary>
/// Database entity representing the discrete execution checkpoint of a pipeline step,
/// persisting its state, processed item count, and Brotli-compressed input and output payloads.
/// </summary>
[Table("analysis_step_executions")]
public record AnalysisStepExecutionEntity : EntityBase
{
    /// <summary>
    /// Unique deterministic identifier for the step execution checkpoint, formatted as <c>{AnalysisIdentifier}_{StepOrder}</c>.
    /// </summary>
    [UniqueColumn]
    public string Identifier { get; init; } = string.Empty;

    /// <summary>
    /// External business identifier correlating all steps within a single experimental data processing run.
    /// </summary>
    [Column]
    public string AnalysisIdentifier { get; init; } = string.Empty;

    /// <summary>
    /// Logical name of the executed pipeline step (e.g., "Segmenter", "CurveFitter", "Simulation").
    /// </summary>
    [Column]
    public string StepName { get; init; } = string.Empty;

    /// <summary>
    /// Sequential execution order of the step within the pipeline DAG (1-based).
    /// </summary>
    [Column]
    public int StepOrder { get; init; }

    /// <summary>
    /// Current lifecycle state of the step execution.
    /// </summary>
    [Column]
    public PipelineStepState State { get; init; }

    /// <summary>
    /// Brotli-compressed binary representation (<c>bytea</c>) of the step's input parameters and storage references.
    /// </summary>
    [Column]
    public byte[] InputData { get; init; } = [];

    /// <summary>
    /// Optional Brotli-compressed binary representation (<c>bytea</c>) of the step's output metrics and generated storage references.
    /// </summary>
    [Column]
    public byte[]? OutputData { get; init; }

    /// <summary>
    /// Total consolidated number of data points or items processed by this step.
    /// </summary>
    [Column]
    public int ProcessedItems { get; init; }

    /// <summary>
    /// Diagnostic error message or exception details if the step faulted during execution.
    /// </summary>
    [Column]
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Moment in UTC when step execution commenced.
    /// </summary>
    [Column]
    public DateTimeOffset StartedAt { get; init; }

    /// <summary>
    /// Moment in UTC when step execution concluded, or null if still running or faulted.
    /// </summary>
    [Column]
    public DateTimeOffset? CompletedAt { get; init; }
}
