using MelloSilveiraTools.Core.Pipelines.Models;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Services.ExperimentalData;

/// <summary>
/// Infrastructure and execution topology settings for experimental data processing.
/// </summary>
public record ExperimentalDataSettings
{
    /// <summary>
    /// Configuration options for the file writer pipeline step (concurrency, buffer capacity, and ordering).
    /// Defaults to MaxWorkers = 1, MaxBufferSize = 10000, and KeepOrder = true to guarantee data integrity.
    /// </summary>
    public PipelineStepOptions FileWriterOptions { get; init; } = new()
    {
        MaxWorkers = 1,
        MaxBufferSize = 10000,
        KeepOrder = true
    };

    /// <summary>
    /// Configuration options for the curve segment grouping step in the Dataflow pipeline.
    /// Defaults to default pipeline step options.
    /// </summary>
    public PipelineStepOptions GroupingOptions { get; init; } = PipelineStepOptions.Default;

    /// <summary>
    /// Configuration options for the curve segment builder step in the Dataflow pipeline.
    /// Defaults to default pipeline step options.
    /// </summary>
    public PipelineStepOptions SegmentBuilderOptions { get; init; } = PipelineStepOptions.Default;
}
