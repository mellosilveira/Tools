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
    // TODO: ARQUIVOS DEVEM VIR DO PROVEDOR DE ARQUIVOS UTILIZADO PELA APLICAÇÃO E NÃO ARQUIVO SUBMETIDO VIA REQUEST
    /// <inheritdoc/>
    protected override async Task<Result<ProcessExperimentalDataResponse>> ExecuteCommandAsync(ProcessExperimentalDataRequest request, CancellationToken cancellationToken = default)
    {
        Stream strainStream = request.StrainFile.OpenReadStream();
        Stream stressStream = request.StressFile.OpenReadStream();

        try
        {
            ExperimentalDataProcessingInput input = request.ToInput(strainStream, stressStream);
            Result<string> pipelineResult = await pipeline.ProcessAsync(input, cancellationToken).ConfigureAwait(false);
            return pipelineResult.ChangeData(new ProcessExperimentalDataResponse(pipelineResult.Data!));
        }
        finally
        {
            await strainStream.DisposeAsync().ConfigureAwait(false);
            await stressStream.DisposeAsync().ConfigureAwait(false);
        }
    }
}
