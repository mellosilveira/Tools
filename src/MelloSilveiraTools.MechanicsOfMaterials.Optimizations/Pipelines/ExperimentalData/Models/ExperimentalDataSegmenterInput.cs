namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Models;

/// <summary>
/// Ingestion payload consumed by the <see cref="Steps.ExperimentalDataSegmenterStep"/>.
/// </summary>
/// <param name="StartExperimentalTimeThreshold">The threshold time below which initial transient data points are disregarded.</param>
/// <param name="StrainStream">Stream containing raw experimental strain time-history.</param>
/// <param name="StressStream">Stream containing raw experimental stress time-history.</param>
/// <param name="Options">Processing options for segmentation tolerances and smoothing.</param>
public sealed record ExperimentalDataSegmenterInput(
    double StartExperimentalTimeThreshold,
    Stream StrainStream,
    Stream StressStream,
    ExperimentalDataProcessingOptions Options);
