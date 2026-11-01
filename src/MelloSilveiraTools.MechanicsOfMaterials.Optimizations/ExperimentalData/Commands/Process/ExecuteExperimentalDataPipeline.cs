using MelloSilveiraTools.Core.Pipelines;
using MelloSilveiraTools.Core.Pipelines.Commands;
using MelloSilveiraTools.Core.Validators;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Pipelines.Models;
using Microsoft.Extensions.Logging;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Commands.Process;

public class ExecuteExperimentalDataPipeline(
    ILogger<ExecuteExperimentalDataPipeline> logger,
    IPipeline<ExperimentalDataProcessingInput, string> pipeline,
    IValidator<ExecuteExperimentalDataPipelineRequest> validator)
    : ExecutePipeline<ExecuteExperimentalDataPipelineRequest, ExperimentalDataProcessingInput, string>(logger, pipeline, validator)
{
    protected override ExperimentalDataProcessingInput MapToInput(ExecuteExperimentalDataPipelineRequest request) => new()
    {
        Identifier = string.IsNullOrWhiteSpace(request.Identifier) ? Guid.NewGuid().ToString("N") : request.Identifier,
        StrainFileLocation = request.StrainFileLocation,
        StressFileLocation = request.StressFileLocation,
        StartExperimentalTimeThreshold = request.StartExperimentalTimeThreshold,
        MechanicalModelName = request.MechanicalModelName,
        TargetSegments = request.TargetSegments,
        CurveFitterType = request.CurveFitterType,
        TargetRSquared = request.TargetRSquared,
        AsymptoteConsecutivePointsThreshold = request.AsymptoteConsecutivePointsThreshold,
        SimulationTimeStep = request.SimulationTimeStep,
        FinalSimulationTime = request.FinalSimulationTime,
        DownsamplingSkipTimeStep = request.DownsamplingSkipTimeStep,
        // TODO: In next versions, analyze if it must be a settings.
        Options = request.Options ?? new ExperimentalDataProcessingOptions()
    };
}
