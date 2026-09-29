using MelloSilveiraTools.Core.Pipelines.Models;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData;

/// <summary>
/// Infrastructure and execution topology settings for experimental data processing.
/// </summary>
public record ExperimentalDataSettings
{
    /// <summary>
    /// Configuration options for the file writer pipeline step (concurrency, buffer capacity, and ordering).
    /// Defaults to MaxWorkers = 1, MaxBufferSize = 10000, and KeepOrder = true to guarantee data integrity.
    /// </summary>
    public PipelineStepOptions FileWriterOptions { get; init; } = new(1, 5000, true);

    /// <summary>
    /// Configuration options for the curve segment grouping step in the Dataflow pipeline.
    /// Defaults to default pipeline step options.
    /// </summary>
    public PipelineStepOptions GroupingOptions { get; init; } = new(1, 100, true);

    /// <summary>
    /// Configuration options for the curve segment builder step in the Dataflow pipeline.
    /// Defaults to default pipeline step options.
    /// </summary>
    public PipelineStepOptions SegmentBuilderOptions { get; init; } = PipelineStepOptions.Synchronous;

    /// <summary>
    /// Configuration options for the experimental data segmenter pipeline step in the Dataflow pipeline.
    /// Defaults to default pipeline step options.
    /// </summary>
    public PipelineStepOptions SegmenterOptions { get; init; } = new(1, 5000, true);

    /// <summary>
    /// Configuration options for the mechanical model curve fitter pipeline step in the Dataflow pipeline.
    /// Defaults to default pipeline step options.
    /// </summary>
    public PipelineStepOptions CurveFitterOptions { get; init; } = PipelineStepOptions.Synchronous;

    /// <summary>
    /// Configuration options for the simulation input builder step in the Dataflow pipeline.
    /// Defaults to default pipeline step options.
    /// </summary>
    public PipelineStepOptions SimulationInputBuilderOptions { get; init; } = PipelineStepOptions.Synchronous;
}
