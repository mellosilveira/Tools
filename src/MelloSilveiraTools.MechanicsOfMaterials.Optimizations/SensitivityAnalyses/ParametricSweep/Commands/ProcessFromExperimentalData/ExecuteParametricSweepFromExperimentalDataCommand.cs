using MelloSilveiraTools.Core.Application.Commands;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Core.Pipelines;
using MelloSilveiraTools.Core.Providers;
using MelloSilveiraTools.Core.Validators;
using MelloSilveiraTools.Database.Repositories;
using MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels.TypeResolvers;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Pipelines.Entities;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.ParametricSweep.Pipelines.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.ParametricSweep.Pipelines.Steps.Generators;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Serializers.Json;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.ParametricSweep.Commands.ProcessFromExperimentalData;

/// <summary>
/// Executes parametric sweep sensitivity analyses across all fitted constitutive parameters generated from an experimental data pipeline run.
/// Supports both sequential and concurrent (parallel) execution modes with error isolation per model.
/// </summary>
/// <param name="logger">Structured logger instance for diagnostic and operational logging.</param>
/// <param name="repository">Database repository provider used to query fitted constitutive models.</param>
/// <param name="serviceLocator">Service locator resolving keyed model type resolvers and sweep generators.</param>
/// <param name="pipeline">Canonical parametric sweep pipeline implementation.</param>
/// <param name="validator">Request validator for <see cref="ExecuteParametricSweepFromExperimentalDataRequest"/>.</param>
public sealed class ExecuteParametricSweepFromExperimentalDataCommand(
    ILogger<ExecuteParametricSweepFromExperimentalDataCommand> logger,
    IRepository repository,
    IPipeline<ParametricSweepPipelineInput, string> pipeline,
    IValidator<ExecuteParametricSweepFromExperimentalDataRequest> validator,
    ServiceLocator serviceLocator)
    : CommandBaseWithData<ExecuteParametricSweepFromExperimentalDataRequest, IAsyncEnumerable<string>>(validator)
{
    /// <inheritdoc/>
    protected override async Task<Result<IAsyncEnumerable<string>>> ExecuteCommandAsync(ExecuteParametricSweepFromExperimentalDataRequest request, CancellationToken cancellationToken)
    {
        IMechanicalModelTypeResolver? resolver = serviceLocator.GetKeyedService<IMechanicalModelTypeResolver>(request.MechanicalModelName);
        if (resolver is null)
        {
            logger.LogWarning("No mechanical model type resolver registered for model '{ModelName}'.", request.MechanicalModelName);
            return Result.CreateNotFound($"No mechanical model type resolver registered for model '{request.MechanicalModelName}'.");
        }

        MechanicalModelCurveFitOutputFilter filter = new()
        {
            ExtenalIdentifier = request.ExperimentalDataIdentifier,
            MechanicalModelName = request.MechanicalModelName
        };

        List<MechanicalModelCurveFitOutputEntity> entities = await repository
            .GetAsync<MechanicalModelCurveFitOutputEntity, MechanicalModelCurveFitOutputFilter>(filter, cancellationToken: cancellationToken)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (entities.Count == 0)
        {
            logger.LogWarning("No fitted mechanical model outputs found for experimental data identifier '{Identifier}'.", request.ExperimentalDataIdentifier);
            return Result.CreateNotFound($"No fitted mechanical models found for experimental data identifier '{request.ExperimentalDataIdentifier}'.");
        }

        logger.LogInformation(
            "Found {Count} fitted mechanical model outputs for experimental data identifier '{Identifier}'. Executing in {ExecutionMode} mode.",
            entities.Count,
            request.ExperimentalDataIdentifier,
            request.ExecutionMode);

        IAsyncEnumerable<string> identifiers = ExecutePipelinesAsync(resolver, request, entities, cancellationToken);
        return Result.CreateSuccessAccepted(identifiers);
    }

    private async IAsyncEnumerable<string> ExecutePipelinesAsync(
        IMechanicalModelTypeResolver resolver,
        ExecuteParametricSweepFromExperimentalDataRequest request,
        List<MechanicalModelCurveFitOutputEntity> entities,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        int maxDegreeOfParallelism = request.ExecutionMode == PipelineExecutionMode.Sequential ? 1 : Math.Max(1, request.MaxDegreeOfParallelism ?? Environment.ProcessorCount);
        using (SemaphoreSlim semaphoreSlim = new(maxDegreeOfParallelism, maxDegreeOfParallelism))
        {
            foreach (MechanicalModelCurveFitOutputEntity entity in entities)
            {
                await semaphoreSlim.WaitAsync(cancellationToken).ConfigureAwait(false);

                string identifier = $"{entity.ExtenalIdentifier}_{entity.MechanicalModelName}_{Guid.NewGuid():N}";

                _ = Task.Run(async () =>
                {
                    ParametricSweepPipelineInput input = null!;
                    try
                    {
                        object? deserialized = OptimizationJsonOptions.Deserialize(entity.ConstitutiveParameters, resolver.ConstitutiveParameters);
                        if (deserialized is not ConstitutiveParameters baseline)
                        {
                            logger.LogError("Failed to deserialize constitutive parameters for model '{ModelName}' from entity '{Identifier}'.", entity.MechanicalModelName, entity.Identifier);
                            return;
                        }

                        input = new()
                        {
                            Identifier = identifier,
                            MechanicalModelName = entity.MechanicalModelName,
                            Baseline = baseline,
                            ConstantVariables = request.ConstantVariables,
                            ArrayVariables = request.ArrayVariables,
                            BaseStrain = request.BaseStrain,
                            StrainRange = request.StrainRange,
                            RampTimeConsideration = request.RampTimeConsideration,
                            RampTime = request.RampTime,
                            InitialTime = request.InitialTime,
                            TimeStep = request.TimeStep,
                            FinalTime = request.FinalTime,
                            RootFindingAlgorithm = request.RootFindingAlgorithm,
                            BaseRootFindingInput = request.BaseRootFindingInput,
                        };

                        logger.LogInformation("Starting parametric sweep pipeline. Input: {@Input}.", input);

                        Result<string> pipelineResult = await pipeline.ProcessAsync(input, cancellationToken).ConfigureAwait(false);

                        logger.LogInformation("Parametric sweep pipeline completed successfully. Input: {@Input}. Result: {@Result}", request, input);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Parametric sweep pipeline faulted. Request: {@Request}. Input: {@Input}.", request, input);
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
}
