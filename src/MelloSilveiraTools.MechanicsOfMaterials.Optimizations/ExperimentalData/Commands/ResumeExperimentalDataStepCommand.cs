using MelloSilveiraTools.Core.Application.Commands;
using MelloSilveiraTools.Core.Managers.File.Models;
using MelloSilveiraTools.Core.Managers.File.Providers;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Core.Providers;
using MelloSilveiraTools.Core.Validators;
using MelloSilveiraTools.Database.Repositories;
using MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels.TypeResolvers;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Controllers;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Factories;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Pipelines.Steps;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Pipelines.Steps.CurveFitter;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Readers;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Serialization;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Services;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Serializers.Json;
using Microsoft.Extensions.Logging;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Commands;

/// <summary>
/// Application command that orchestrates retrying or resuming an experimental data analysis from a specific pipeline step.
/// </summary>
/// <param name="logger">The structured logger instance.</param>
/// <param name="loggerFactory">The logger factory for creating step-specific loggers.</param>
/// <param name="fileStorageService">The file storage service used for reading and writing files.</param>
/// <param name="mechanicalModelCalculatorFactory">Factory resolving mechanical model calculators.</param>
/// <param name="mechanicalModelStepFactory">Factory resolving model-specific curve fitter steps.</param>
/// <param name="repository">Database repository provider for persisting curve fits and simulations.</param>
/// <param name="stepExecutionManager">Manager for checkpoint persistence and rollback operations.</param>
/// <param name="serviceLocator">Service locator resolving keyed model type resolvers.</param>
/// <param name="validator">Optional request validator.</param>
public sealed class ResumeExperimentalDataStepCommand(
    ILogger<ResumeExperimentalDataStepCommand> logger,
    ILoggerFactory loggerFactory,
    IFileStorageService fileStorageService,
    IMechanicalModelCalculatorFactory mechanicalModelCalculatorFactory,
    IMechanicalModelStepFactory mechanicalModelStepFactory,
    IRepository repository,
    IStepExecutionManager stepExecutionManager,
    ServiceLocator serviceLocator,
    IValidator<ResumeStepRequest>? validator = null)
    : CommandBaseWithData<ResumeStepRequest, string>(validator)
{
    /// <inheritdoc/>
    protected override async Task<Result<string>> ExecuteCommandAsync(ResumeStepRequest request, CancellationToken cancellationToken = default)
    {
        string pipelineIdentifier = request.Identifier;
        string fromStepName = request.FromStepName;

        IReadOnlyList<AnalysisStepExecutionEntity> steps = await stepExecutionManager.GetStepExecutionsAsync(pipelineIdentifier, cancellationToken).ConfigureAwait(false);

        try
        {
            if (string.Equals(fromStepName, "CurveFitter", StringComparison.OrdinalIgnoreCase))
            {
                AnalysisStepExecutionEntity? segmenterStep = steps.FirstOrDefault(s => s.StepOrder == 1);
                AnalysisStepExecutionEntity? curveFitStep = steps.FirstOrDefault(s => s.StepOrder == 2);
                if (segmenterStep?.OutputData is null || curveFitStep?.InputData is null)
                {
                    return Result.CreateBadRequest("Cannot resume curve fitting: previous step checkpoints are missing.");
                }

                SegmenterStepCheckpointOutput? segOutput = StepPayloadCompressor.Decompress<SegmenterStepCheckpointOutput>(segmenterStep.OutputData);
                CurveFitterStepCheckpointInput? fitInput = StepPayloadCompressor.Decompress<CurveFitterStepCheckpointInput>(curveFitStep.InputData);

                if (segOutput is null || fitInput is null)
                {
                    return Result.CreateBadRequest("Failed to decompress step checkpoint payloads.");
                }

                await stepExecutionManager.RollbackFromStepAsync(pipelineIdentifier, 2, cancellationToken).ConfigureAwait(false);
                IReadOnlyList<CurveSegment> segments = await CurveSegmentCsvReader.ReadSegmentsAsync(fileStorageService, segOutput.SegmentedFileLocation, cancellationToken).ConfigureAwait(false);

                IMechanicalModelCurveFitterStep curveFitterStep = mechanicalModelStepFactory.CreateCurveFitterStep(fitInput.MechanicalModelName, fitInput.TargetSegments, fitInput.CurveFitterType, fitInput.TargetRSquared);
                await stepExecutionManager.RecordStepStartedAsync(pipelineIdentifier, "CurveFitter", 2, fitInput, cancellationToken).ConfigureAwait(false);

                CurveFitOutputIdentifierBuilderStep identifierBuilder = new();
                CurveFitOutputPersistenceStep persistenceStep = new(loggerFactory.CreateLogger<CurveFitOutputPersistenceStep>(), repository, pipelineIdentifier, fitInput.MechanicalModelName);

                await using (curveFitterStep)
                {
                    foreach (CurveSegment segment in segments)
                    {
                        await foreach (MechanicalModelCurveFitOutput curveFitOutput in curveFitterStep.ExecuteAsync(segment, cancellationToken).ConfigureAwait(false))
                        {
                            string fitIdentifier = identifierBuilder.Execute(curveFitOutput);
                            await persistenceStep.ExecuteAsync((fitIdentifier, curveFitOutput), cancellationToken).ConfigureAwait(false);
                            await ExecuteSimulationAndPersistenceAsync(pipelineIdentifier, segOutput.SegmentedFileLocation, fitInput.MechanicalModelName, fitIdentifier, curveFitOutput, cancellationToken).ConfigureAwait(false);
                        }
                    }
                }

                return pipelineIdentifier;
            }

            if (string.Equals(fromStepName, "Simulation", StringComparison.OrdinalIgnoreCase))
            {
                AnalysisStepExecutionEntity? curveFitStep = steps.FirstOrDefault(s => s.StepOrder == 2);
                AnalysisStepExecutionEntity? segmenterStep = steps.FirstOrDefault(s => s.StepOrder == 1);
                if (curveFitStep?.OutputData is null || segmenterStep?.OutputData is null)
                {
                    return Result.CreateBadRequest("Cannot resume simulation: previous step checkpoint outputs are missing.");
                }

                CurveFitterStepCheckpointOutput? fitOutput = StepPayloadCompressor.Decompress<CurveFitterStepCheckpointOutput>(curveFitStep.OutputData);
                SegmenterStepCheckpointOutput? segOutput = StepPayloadCompressor.Decompress<SegmenterStepCheckpointOutput>(segmenterStep.OutputData);
                CurveFitterStepCheckpointInput? fitInput = StepPayloadCompressor.Decompress<CurveFitterStepCheckpointInput>(curveFitStep.InputData);

                if (fitOutput is null || segOutput is null || fitInput is null)
                {
                    return Result.CreateBadRequest("Failed to decompress step checkpoint payloads.");
                }

                await stepExecutionManager.RollbackFromStepAsync(pipelineIdentifier, 3, cancellationToken).ConfigureAwait(false);

                IMechanicalModelTypeResolver mechanicalModelTypeResolver = serviceLocator.GetRequiredKeyedService<IMechanicalModelTypeResolver>(fitInput.MechanicalModelName);
                IReadOnlyList<CurveSegment> segments = await CurveSegmentCsvReader.ReadSegmentsAsync(fileStorageService, segOutput.SegmentedFileLocation, cancellationToken).ConfigureAwait(false);

                MechanicalModelCurveFitOutput curveFitOutput = new(
                    segments,
                    OptimizationJsonOptions.Deserialize(fitOutput.ConstitutiveParametersJson, mechanicalModelTypeResolver.ConstitutiveParameters) as ConstitutiveParameters ?? throw new InvalidOperationException("Failed to deserialize constitutive parameters."),
                    new AcceptedRange(segments[0].ExperimentalStrain[0], segments[^1].ExperimentalStrain[^1]),
                    LoadResponseRelationship.StressStrain,
                    ViscoelasticEffect.Relaxation,
                    RampTimeConsideration.Disregard,
                    fitOutput.RSquared,
                    fitOutput.FinalError,
                    fitOutput.Iterations);

                await ExecuteSimulationAndPersistenceAsync(pipelineIdentifier, segOutput.SegmentedFileLocation, fitInput.MechanicalModelName, fitOutput.CurveFitIdentifier, curveFitOutput, cancellationToken).ConfigureAwait(false);

                return pipelineIdentifier;
            }

            return Result.CreateBadRequest($"Unsupported resume step: '{fromStepName}'.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Step resume faulted for identifier: {Identifier}, step: {StepName}.", pipelineIdentifier, fromStepName);
            await stepExecutionManager.RecordCurrentRunningStepFaultedAsync(pipelineIdentifier, ex.Message, cancellationToken).ConfigureAwait(false);
            throw;
        }
    }

    private async Task ExecuteSimulationAndPersistenceAsync(
        string pipelineIdentifier,
        string segmentedFileLocation,
        string mechanicalModelName,
        string curveFitIdentifier,
        MechanicalModelCurveFitOutput curveFitOutput,
        CancellationToken cancellationToken)
    {
        IMechanicalModelTypeResolver mechanicalModelTypeResolver = serviceLocator.GetRequiredKeyedService<IMechanicalModelTypeResolver>(mechanicalModelName);
        MechanicalModelSimulationStep simulationStep = new(pipelineIdentifier, fileStorageService, mechanicalModelTypeResolver, mechanicalModelCalculatorFactory, experimentalDataFileLocation: segmentedFileLocation);
        MechanicalModelOutputPersistenceStep persistenceStep = new(loggerFactory.CreateLogger<MechanicalModelOutputPersistenceStep>(), fileStorageService, repository, pipelineIdentifier, mechanicalModelName);

        await stepExecutionManager.RecordStepStartedAsync(pipelineIdentifier, "Simulation", 3, new SimulationStepCheckpointInput(segmentedFileLocation, curveFitIdentifier, null, null, 10), cancellationToken).ConfigureAwait(false);

        (double? asymptoteTime, SimulationDeltaOutput deltaOutput, StorageEntryMetadata outputFileMetadata) = await simulationStep.ExecuteAsync(curveFitOutput, cancellationToken).ConfigureAwait(false);

        MechanicalModelOutputPersistenceInput persistenceInput = new(
            segmentedFileLocation,
            curveFitIdentifier,
            asymptoteTime,
            deltaOutput,
            outputFileMetadata);

        await persistenceStep.ExecuteAsync(persistenceInput, cancellationToken).ConfigureAwait(false);
    }
}
