using MelloSilveiraTools.Core.Application.Commands;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Core.Pipelines;
using MelloSilveiraTools.Core.Validators;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Models;
using Microsoft.Extensions.Logging;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Commands;

/// <summary>
/// Application command that validates and orchestrates storage-backed experimental raw data processing via <see cref="IPipeline{TInput, TOutput}"/>.
/// </summary>
/// <param name="pipeline">The experimental data processing pipeline.</param>
/// <param name="validator">Request validator for <see cref="ProcessExperimentalDataRequest"/>.</param>
public sealed class ProcessExperimentalDataCommand(
    ILogger<ProcessExperimentalDataCommand> logger,
    IPipeline<ExperimentalDataProcessingInput, string> pipeline,
    IValidator<ProcessExperimentalDataRequest> validator)
    : CommandBaseWithData<ProcessExperimentalDataRequest, Result<string>>(validator)
{
    /// <inheritdoc/>
    protected override async Task<Result<Result<string>>> ExecuteCommandAsync(ProcessExperimentalDataRequest request, CancellationToken cancellationToken = default)
    {
        ExperimentalDataProcessingInput input = request.ToInput();
        _ = Task.Run(async () =>
        {
            try
            {
                logger.LogInformation("Starting experimental data processing pipeline for analysis identifier '{Identifier}'. Request: {@Request}", input.Identifier, request);

                // TODO: TENTAR REPASSAR O CANCELLATION TOKEN PARA O PIPELINE
                Result<string> result = await pipeline.ProcessAsync(input).ConfigureAwait(false);

                logger.LogInformation("Experimental data processing pipeline completed successfully for analysis identifier '{Identifier}'. Result: {@Result}", input.Identifier, result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Experimental data processing pipeline faulted for analysis identifier '{Identifier}'. Request: {@Request}", input.Identifier, request);
            }
        }, cancellationToken);

        return Result.CreateSuccessAccepted(input.Identifier);
    }
}
