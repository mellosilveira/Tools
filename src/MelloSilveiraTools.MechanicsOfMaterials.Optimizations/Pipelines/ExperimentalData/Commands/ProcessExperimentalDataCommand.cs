using MelloSilveiraTools.Core.Application.Commands;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Core.Validators;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Models;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Commands;

/// <summary>
/// Application command that validates and orchestrates experimental raw data processing via <see cref="IExperimentalDataProcessingPipeline"/>.
/// </summary>
/// <param name="pipeline">The experimental data processing pipeline.</param>
/// <param name="validator">Request validator for <see cref="ProcessExperimentalDataRequest"/>.</param>
public sealed class ProcessExperimentalDataCommand(
    IExperimentalDataProcessingPipeline pipeline,
    IValidator<ProcessExperimentalDataRequest> validator)
    : CommandBaseWithData<ProcessExperimentalDataRequest, ProcessExperimentalDataResponse>(validator)
{
    /// <inheritdoc/>
    protected override async Task<Result<ProcessExperimentalDataResponse>> ExecuteCommandAsync(ProcessExperimentalDataRequest request)
    {
        bool disposeStrainStream = request.StrainStream is null;
        bool disposeStressStream = request.StressStream is null;

        Stream strainStream = request.StrainStream ?? request.StrainFile!.OpenReadStream();
        Stream stressStream = request.StressStream ?? request.StressFile!.OpenReadStream();

        try
        {
            ExperimentalDataProcessingInput input = new()
            {
                StrainStream = strainStream,
                StressStream = stressStream,
                StartExperimentalTimeThreshold = request.StartExperimentalTimeThreshold,
                Identifier = request.Identifier,
                MechanicalModelName = request.MechanicalModelName,
                TargetSegments = request.TargetSegments,
                CurveFitterType = request.CurveFitterType,
                TargetRSquared = request.TargetRSquared,
                AsymptoteConsecutivePointsThreshold = request.AsymptoteConsecutivePointsThreshold,
                SimulationTimeStep = request.SimulationTimeStep,
                FinalSimulationTime = request.FinalSimulationTime,
                DownsamplingSkipTimeStep = request.DownsamplingSkipTimeStep,
                Options = request.Options ?? new ExperimentalDataProcessingOptions(),
                Progress = request.Progress
            };

            Result<string> pipelineResult = await pipeline.ProcessAsync(input, CancellationToken.None).ConfigureAwait(false);
            if (!pipelineResult.Success)
            {
                return new Result<ProcessExperimentalDataResponse>
                {
                    Success = false,
                    StatusCode = pipelineResult.StatusCode,
                    Messages = pipelineResult.Messages
                };
            }

            return new ProcessExperimentalDataResponse(pipelineResult.Data!);
        }
        finally
        {
            if (disposeStrainStream)
            {
                await strainStream.DisposeAsync().ConfigureAwait(false);
            }

            if (disposeStressStream)
            {
                await stressStream.DisposeAsync().ConfigureAwait(false);
            }
        }
    }
}
