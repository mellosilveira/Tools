using MelloSilveiraTools.Core.Pipelines.Models;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData;

/// <summary>
/// Infrastructure and execution topology settings for experimental data processing.
/// </summary>
public record ExperimentalDataSettings
{
    /// <summary>
    /// Configuration options for the experimental data segmenter pipeline step in the Dataflow pipeline.
    /// </summary>
    public required PipelineStepOptions SegmenterOptions { get; init; }

    /// <summary>
    /// Configuration options for the file writer pipeline step (concurrency, buffer capacity, and ordering).
    /// </summary>
    public required PipelineStepOptions FileWriterOptions { get; init; }

    /// <summary>
    /// Configuration options for the persistence pipeline step in the Dataflow pipeline.
    /// </summary>
    public required PipelineStepOptions PersistanceOptions { get; init; }


    /// <summary>
    /// Configuration options for the curve segment grouping step in the Dataflow pipeline.
    /// </summary>
    public required PipelineStepOptions GroupingOptions { get; init; }

    /// <summary>
    /// Configuration options for the curve segment builder step in the Dataflow pipeline.
    /// </summary>
    public required PipelineStepOptions SegmentBuilderOptions { get; init; }

    /// <summary>
    /// Configuration options for the mechanical model curve fitter pipeline step in the Dataflow pipeline.
    /// </summary>
    public required PipelineStepOptions CurveFitterOptions { get; init; }

    /// <summary>
    /// Configuration options for the simulation input builder step in the Dataflow pipeline.
    /// </summary>
    public required PipelineStepOptions SimulationInputBuilderOptions { get; init; }

    /// <summary>
    /// Mandatory maximum bounded capacity for the initial ingestion buffer block of the experimental data Dataflow pipeline.
    /// </summary>
    public required int IngestionBufferCapacity { get; init; }
}
