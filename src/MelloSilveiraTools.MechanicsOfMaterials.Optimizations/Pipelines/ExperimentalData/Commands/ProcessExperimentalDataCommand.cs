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
        Stream strainStream = request.StrainFile.OpenReadStream();
        Stream stressStream = request.StressFile.OpenReadStream();

        try
        {
            ExperimentalDataProcessingInput input = request.ToInput(strainStream, stressStream);
            Result<string> pipelineResult = await pipeline.ProcessAsync(input, request.CancellationToken).ConfigureAwait(false);

            return pipelineResult.Success
                ? new ProcessExperimentalDataResponse(pipelineResult.Data!)
                : new Result<ProcessExperimentalDataResponse> { StatusCode = pipelineResult.StatusCode, Messages = pipelineResult.Messages };
        }
        finally
        {
            await strainStream.DisposeAsync().ConfigureAwait(false);
            await stressStream.DisposeAsync().ConfigureAwait(false);
        }
    }
}
