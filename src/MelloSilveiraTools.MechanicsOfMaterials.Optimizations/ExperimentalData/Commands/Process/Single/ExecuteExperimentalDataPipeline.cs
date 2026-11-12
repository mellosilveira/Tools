using MelloSilveiraTools.Core.Pipelines;
using MelloSilveiraTools.Core.Pipelines.Commands;
using MelloSilveiraTools.Core.Validators;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Pipelines.Models;
using Microsoft.Extensions.Logging;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Commands.Process.Single;

/// <summary>
/// Executes the experimental data processing pipeline for a single pair of stress and strain files.
/// </summary>
public class ExecuteExperimentalDataPipeline(
    ILogger<ExecuteExperimentalDataPipeline> logger,
    IPipeline<ExperimentalDataProcessingInput, string> pipeline,
    IValidator<ExecuteExperimentalDataPipelineRequest> validator)
    : ExecutePipeline<ExecuteExperimentalDataPipelineRequest, ExperimentalDataProcessingInput, string>(logger, pipeline, validator)
{
    /// <inheritdoc/>
    protected override ExperimentalDataProcessingInput MapToInput(ExecuteExperimentalDataPipelineRequest request) => new()
    {
        Identifier = string.IsNullOrWhiteSpace(request.ExperimentalDataGroupRequest.Identifier) ? Guid.NewGuid().ToString("N") : request.ExperimentalDataGroupRequest.Identifier,
        StrainFileLocation = request.ExperimentalDataGroupRequest.StrainFileLocation,
        StressFileLocation = request.ExperimentalDataGroupRequest.StressFileLocation,
        StartExperimentalTimeThreshold = request.ExperimentalDataGroupRequest.StartExperimentalTimeThreshold,
        MechanicalModelName = request.MechanicalModelName,
        TargetSegments = request.TargetSegments,
        CurveFitterType = request.CurveFitterType,
        TargetRSquared = request.TargetRSquared,
        ConfidenceLevel = request.ConfidenceLevel,
        AsymptoteConsecutivePointsThreshold = request.AsymptoteConsecutivePointsThreshold,
        SimulationTimeStep = request.SimulationTimeStep,
        FinalSimulationTime = request.FinalSimulationTime,
        DownsamplingSkipTimeStep = request.DownsamplingSkipTimeStep,
        // TODO: In next versions, analyze if it must be a settings.
        Options = request.Options ?? new ExperimentalDataProcessingOptions()
    };
}
