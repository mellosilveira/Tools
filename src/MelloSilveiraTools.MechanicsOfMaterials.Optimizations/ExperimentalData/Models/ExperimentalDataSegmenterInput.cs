namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Models;

/// <summary>
/// Ingestion payload consumed by <see cref="Pipelines.Steps.ExperimentalDataSegmenterStep"/> referencing persistent storage locations.
/// </summary>
/// <param name="StartExperimentalTimeThreshold">The threshold time below which initial transient data points are disregarded.</param>
/// <param name="StrainFileLocation">Storage location URI or path for the raw experimental strain time-history CSV.</param>
/// <param name="StressFileLocation">Storage location URI or path for the raw experimental stress time-history CSV.</param>
/// <param name="Options">Processing options for segmentation tolerances and smoothing.</param>
public sealed record ExperimentalDataSegmenterInput(
    double StartExperimentalTimeThreshold,
    string StrainFileLocation,
    string StressFileLocation,
    ExperimentalDataProcessingOptions Options);
