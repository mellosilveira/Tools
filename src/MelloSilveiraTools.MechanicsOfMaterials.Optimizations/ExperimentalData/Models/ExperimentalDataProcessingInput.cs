using MelloSilveiraTools.Mathematics.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Models;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Models;

/// <summary>
/// Input parameters for processing experimental raw data streams through the segmentation and optimization pipeline via storage locations.
/// </summary>
public sealed record ExperimentalDataProcessingInput
{
    /// <summary>
    /// Storage location URI or path for the raw experimental strain time-history CSV.
    /// </summary>
    public required string StrainFileLocation { get; init; }

    /// <summary>
    /// Storage location URI or path for the raw experimental stress time-history CSV.
    /// </summary>
    public required string StressFileLocation { get; init; }

    /// <summary>
    /// Optional threshold time to ignore initial transient data. When provided, all data points with time less than this threshold are ignored during processing.
    /// </summary>
    public double? StartExperimentalTimeThreshold { get; init; }

    /// <summary>
    /// Predefined analysis identifier.
    /// </summary>
    public required string Identifier { get; init; }

    /// <summary>
    /// The name of the mechanical model to fit (e.g., "Fung", "Schapery").
    /// </summary>
    public required string MechanicalModelName { get; init; }

    /// <summary>
    /// Segment types to be processed.
    /// If left empty, all segment types supported by the model will be considered.
    /// </summary>
    public IReadOnlyList<SegmentType> TargetSegments { get; init; } = [];

    /// <summary>
    /// Type of curve fitter algorithm to execute (e.g. Alglib or MathNet). Defaults to <see cref="CurveFitterType.Alglib"/>.
    /// </summary>
    public required CurveFitterType CurveFitterType { get; init; }

    /// <summary>
    /// Mandatory target R² determination coefficient specified by the user for curve fitting optimization.
    /// </summary>
    public required double TargetRSquared { get; init; }

    /// <summary>
    /// The consecutive points threshold required to detect that a steady-state asymptote has been reached.
    /// </summary>
    public int AsymptoteConsecutivePointsThreshold { get; init; }

    /// <summary>
    /// Optional time step used during extended simulation. If omitted, defaults to the experimental time step.
    /// </summary>
    public double? SimulationTimeStep { get; init; }

    /// <summary>
    /// Optional target final simulation time. When provided and greater than the last experimental time, the forward numerical simulation continues marching until this time is reached.
    /// </summary>
    public double? FinalSimulationTime { get; init; }

    /// <summary>
    /// Optional minimal time step between consecutive points in downsampling experimental data.
    /// </summary>
    public double? DownsamplingSkipTimeStep { get; init; }

    /// <summary>
    /// Processing options for segmentation tolerances and smoothing.
    /// </summary>
    public required ExperimentalDataProcessingOptions Options { get; init; }

    /// <summary>
    /// Converts this processing input into the segmenter ingestion payload.
    /// </summary>
    /// <returns>A new <see cref="ExperimentalDataSegmenterInput"/> configured for ingestion.</returns>
    public ExperimentalDataSegmenterInput ToSegmenterInput() => new(
        StartExperimentalTimeThreshold ?? MathematicConstants.InitialTime,
        StrainFileLocation,
        StressFileLocation,
        Options);
}
