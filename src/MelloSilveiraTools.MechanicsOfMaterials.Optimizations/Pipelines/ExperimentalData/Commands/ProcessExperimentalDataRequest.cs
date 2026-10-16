using MelloSilveiraTools.Core.Application.Commands;
using MelloSilveiraTools.Core.Pipelines.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Models;
using Microsoft.AspNetCore.Http;
using System.Text.Json.Serialization;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Commands;

/// <summary>
/// Request payload for executing the experimental data processing and optimization pipeline via the Command pattern.
/// </summary>
public record ProcessExperimentalDataRequest : RequestBase
{
    /// <summary>
    /// Uploaded form file containing the raw experimental strain time-history CSV.
    /// Required when <see cref="StrainStream"/> is not provided.
    /// </summary>
    [JsonIgnore]
    public IFormFile? StrainFile { get; init; }

    /// <summary>
    /// Uploaded form file containing the raw experimental stress time-history CSV.
    /// Required when <see cref="StressStream"/> is not provided.
    /// </summary>
    [JsonIgnore]
    public IFormFile? StressFile { get; init; }

    /// <summary>
    /// Optional in-memory or file stream containing raw experimental strain time-history.
    /// </summary>
    [JsonIgnore]
    public Stream? StrainStream { get; init; }

    /// <summary>
    /// Optional in-memory or file stream containing raw experimental stress time-history.
    /// </summary>
    [JsonIgnore]
    public Stream? StressStream { get; init; }

    /// <summary>
    /// Optional threshold time to ignore initial transient data.
    /// </summary>
    public double? StartExperimentalTimeThreshold { get; init; }

    /// <summary>
    /// Optional predefined analysis identifier. If not provided, a unique GUID-based identifier is generated.
    /// </summary>
    public string? Identifier { get; init; }

    /// <summary>
    /// The name of the mechanical model to fit (e.g., "Fung", "SimplifiedFung", "Schapery").
    /// </summary>
    public required string MechanicalModelName { get; init; }

    /// <summary>
    /// Segment types to be processed. If empty, all segment types supported by the model are considered.
    /// </summary>
    public IReadOnlyList<SegmentType> TargetSegments { get; init; } = [];

    /// <summary>
    /// Type of curve fitter algorithm to execute (e.g. Alglib or MathNet). Defaults to <see cref="CurveFitterType.Alglib"/>.
    /// </summary>
    public CurveFitterType CurveFitterType { get; init; } = CurveFitterType.Alglib;

    /// <summary>
    /// Target R² determination coefficient specified for curve fitting optimization. Defaults to 0.99.
    /// </summary>
    public double TargetRSquared { get; init; } = 0.99;

    /// <summary>
    /// The consecutive points threshold required to detect that a steady-state asymptote has been reached. Defaults to 5.
    /// </summary>
    public int AsymptoteConsecutivePointsThreshold { get; init; } = 5;

    /// <summary>
    /// Optional time step used during extended simulation. If omitted, defaults to the experimental time step.
    /// </summary>
    public double? SimulationTimeStep { get; init; }

    /// <summary>
    /// Optional target final simulation time.
    /// </summary>
    public double? FinalSimulationTime { get; init; }

    /// <summary>
    /// Optional minimal time step between consecutive points in downsampling experimental data.
    /// </summary>
    public double? DownsamplingSkipTimeStep { get; init; }

    /// <summary>
    /// Optional processing options for segmentation tolerances and smoothing.
    /// </summary>
    public ExperimentalDataProcessingOptions? Options { get; init; }

    /// <summary>
    /// Optional progress reporter for emitting real-time <see cref="PipelineProgressEvent"/> notifications.
    /// </summary>
    [JsonIgnore]
    public IProgress<PipelineProgressEvent>? Progress { get; init; }
}
