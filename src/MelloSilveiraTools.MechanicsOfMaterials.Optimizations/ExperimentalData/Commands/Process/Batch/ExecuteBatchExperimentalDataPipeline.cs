using MelloSilveiraTools.Core.Application.Commands;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Core.Pipelines;
using MelloSilveiraTools.Core.Validators;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Pipelines.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Models;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Commands.Process.Batch;

/// <summary>
/// Executes the experimental data processing and optimization pipeline across multiple file groups either sequentially or in parallel.
/// </summary>
/// <param name="logger">Structured logger instance for telemetry and diagnostic logging.</param>
/// <param name="pipeline">Canonical experimental data processing pipeline implementation.</param>
/// <param name="validator">Validator for the batch request payload.</param>
public class ExecuteBatchExperimentalDataPipeline(
    ILogger<ExecuteBatchExperimentalDataPipeline> logger,
    IPipeline<ExperimentalDataProcessingInput, string> pipeline,
    IValidator<ExecuteBatchExperimentalDataPipelineRequest> validator)
    : CommandBaseWithData<ExecuteBatchExperimentalDataPipelineRequest, IAsyncEnumerable<string>>(validator)
{
    /// <inheritdoc/>
    protected override async Task<Result<IAsyncEnumerable<string>>> ExecuteCommandAsync(ExecuteBatchExperimentalDataPipelineRequest request, CancellationToken cancellationToken = default)
    {
        IAsyncEnumerable<string> identifiers = ExecutePipelinesAsync(request, cancellationToken);
        return Result.CreateSuccessAccepted(identifiers);
    }

    private async IAsyncEnumerable<string> ExecutePipelinesAsync(ExecuteBatchExperimentalDataPipelineRequest request, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        int maxDegreeOfParallelism = request.ExecutionMode == PipelineExecutionMode.Sequential ? 1 : Math.Max(1, request.MaxDegreeOfParallelism ?? Environment.ProcessorCount);
        using (SemaphoreSlim semaphoreSlim = new(maxDegreeOfParallelism, maxDegreeOfParallelism))
        {
            foreach (ExperimentalDataGroupRequest group in request.Groups)
            {
                await semaphoreSlim.WaitAsync(cancellationToken).ConfigureAwait(false);

                string identifier = string.IsNullOrWhiteSpace(group.Identifier) ? Guid.NewGuid().ToString("N") : group.Identifier;
                ExperimentalDataProcessingInput input = MapToInput(request, group, identifier);

                _ = Task.Run(async () =>
                {
                    try
                    {
                        logger.LogInformation("Starting experimental data pipeline. Input: {@Input}.", input);

                        // TODO: TENTAR REPASSAR O CANCELLATION TOKEN PARA O PIPELINE
                        Result<string> result = await pipeline.ProcessAsync(input).ConfigureAwait(false);

                        logger.LogInformation("Experimental data pipeline completed successfully. Input: {@Input}. Result: {@Result}", request, input);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Experimental data pipeline faulted. Request: {@Request}. Input: {@Input}.", request, input);
                    }
                    finally
                    {
                        semaphoreSlim.Release();
                    }
                }, cancellationToken);

                yield return identifier;
            }
        }
    }

    private static ExperimentalDataProcessingInput MapToInput(
        ExecuteBatchExperimentalDataPipelineRequest request,
        ExperimentalDataGroupRequest group,
        string identifier) => new()
        {
            Identifier = identifier,
            StrainFileLocation = group.StrainFileLocation,
            StressFileLocation = group.StressFileLocation,
            StartExperimentalTimeThreshold = group.StartExperimentalTimeThreshold,
            MechanicalModelName = request.MechanicalModelName,
            TargetSegments = request.TargetSegments,
            CurveFitterType = request.CurveFitterType,
            TargetRSquared = request.TargetRSquared,
            ConfidenceLevel = request.ConfidenceLevel,
            AsymptoteConsecutivePointsThreshold = request.AsymptoteConsecutivePointsThreshold,
            SimulationTimeStep = request.SimulationTimeStep,
            FinalSimulationTime = request.FinalSimulationTime,
            DownsamplingSkipTimeStep = request.DownsamplingSkipTimeStep,
            Options = request.Options ?? new ExperimentalDataProcessingOptions()
        };
}
