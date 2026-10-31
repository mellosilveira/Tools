namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.SensitivityAnalysis;

/// <summary>
/// Infrastructure and execution topology configuration settings for parametric sensitivity sweep pipelines.
/// All properties are required with zero assumption of default values.
/// </summary>
public sealed record ParametricSweepSettings
{
    /// <summary>
    /// Mandatory maximum bounded capacity for the initial ingestion buffer block of the parametric sweep Dataflow pipeline.
    /// </summary>
    public required int IngestionBufferCapacity { get; init; }
}
