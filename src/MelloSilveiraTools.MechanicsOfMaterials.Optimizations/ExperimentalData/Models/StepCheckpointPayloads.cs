using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Models;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Models;

/// <summary>
/// Checkpoint output payload recorded upon completion of the segmentation step.
/// </summary>
/// <param name="SegmentedFileLocation">Persistent storage location of the segmented experimental CSV file.</param>
public sealed record SegmenterStepCheckpointOutput(string SegmentedFileLocation);

/// <summary>
/// Checkpoint input payload captured prior to executing the curve fitting step.
/// </summary>
public sealed record CurveFitterStepCheckpointInput(
    string SegmentedFileLocation,
    string MechanicalModelName,
    IReadOnlyList<SegmentType> TargetSegments,
    CurveFitterType CurveFitterType,
    double TargetRSquared);

/// <summary>
/// Checkpoint output payload recorded upon completion of the curve fitting step.
/// </summary>
public sealed record CurveFitterStepCheckpointOutput(
    string CurveFitIdentifier,
    double RSquared,
    double FinalError,
    int Iterations,
    string ConstitutiveParametersJson);

/// <summary>
/// Checkpoint input payload captured prior to forward numerical simulation.
/// </summary>
public sealed record SimulationStepCheckpointInput(
    string SegmentedFileLocation,
    string CurveFitIdentifier,
    double? SimulationTimeStep,
    double? FinalSimulationTime,
    int AsymptoteConsecutivePointsThreshold);

/// <summary>
/// Checkpoint output payload recorded upon completion of the simulation step.
/// </summary>
public sealed record SimulationStepCheckpointOutput(
    string SimulationOutputFileLocation,
    double? AsymptoteTime,
    SimulationDeltaOutput DeltaOutput);
