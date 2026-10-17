using MelloSilveiraTools.Core.Managers.File;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Core.Pipelines;
using MelloSilveiraTools.Core.Pipelines.Dataflow;
using MelloSilveiraTools.Core.Pipelines.Models;
using MelloSilveiraTools.Database.RelationalDatabase.Models.Entities;
using MelloSilveiraTools.Database.Repositories;
using MelloSilveiraTools.Mathematics.NumericalMethods.Differentiations;
using MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Factories;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Steps;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Steps.CurveFitter;
using Microsoft.Extensions.Logging;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData;

/// <summary>
/// Service responsible for orchestrating the ingestion, validation, and physical phase segmentation of experimental raw data streams.
/// </summary>
/// <param name="loggerFactory">The logger factory for creating step-specific loggers.</param>
/// <param name="fileStorageService">The file storage service used for file writing steps.</param>
/// <param name="differentiation">Numerical differentiation provider used for segment classification.</param>
/// <param name="stepFactory">Factory resolving model-specific curve fitter steps.</param>
/// <param name="repository">Database repository provider for persisting curve fits and simulations.</param>
/// <param name="calculatorFactory">Factory resolving mechanical model calculators.</param>
/// <param name="settings">Pipeline configuration settings and step options.</param>
public sealed class ExperimentalDataProcessingPipeline(
    ILoggerFactory loggerFactory,
    IFileStorageService fileStorageService,
    IDifferentiation differentiation,
    IMechanicalModelStepFactory stepFactory,
    IRepository repository,
    IMechanicalModelCalculatorFactory calculatorFactory,
    ExperimentalDataSettings settings)
    : IExperimentalDataProcessingPipeline
{
    /// <inheritdoc/>
    public async Task<Result<string>> ProcessAsync(ExperimentalDataProcessingInput input, CancellationToken cancellationToken = default)
    {
        ILogger<ExperimentalDataProcessingPipeline> logger = loggerFactory.CreateLogger<ExperimentalDataProcessingPipeline>();

        string uniqueIdentifier = string.IsNullOrWhiteSpace(input.Identifier) ? Guid.NewGuid().ToString("N") : input.Identifier;

        input.Progress?.Report(new PipelineProgressEvent(
            nameof(ExperimentalDataProcessingPipeline),
            "Experimental data processing pipeline started.",
            PipelineStepState.Started,
            Detail: uniqueIdentifier,
            Timestamp: DateTimeOffset.UtcNow));

        ExperimentalDataSegmenterStep segmenterStep = new(loggerFactory.CreateLogger<ExperimentalDataSegmenterStep>(), differentiation);
        ExperimentalDataFileWriterStep experimentalDataFileWriterStep = new(fileStorageService, uniqueIdentifier);
        ExperimentalDataDownsamplerStep downsamplerStep = new(input.DownsamplingSkipTimeStep ?? 0);
        CurveSegmentAccumulatorStep accumulatorStep = new();
        IMechanicalModelCurveFitterStep curveFitterStep = stepFactory.Create(input.MechanicalModelName, input.TargetSegments, input.CurveFitterType, input.TargetRSquared);
        IdentifierBuilderStep identifierBuilderStep = new();
        CurveFitOutputPersistenceStep curveFitPersistenceStep = new(loggerFactory.CreateLogger<CurveFitOutputPersistenceStep>(), repository);
        NumericalSimulationStep numericalSimulationStep = new(calculatorFactory, input.SimulationTimeStep, input.FinalSimulationTime);
        AsymptoteMonitoringStep asymptoteStep = new(input.AsymptoteConsecutivePointsThreshold);
        DeltaCalculatorStep deltaStep = new();
        MechanicalModelOutputFileWriterStep mechanicalModelOutputFileWriterStep = new(fileStorageService, uniqueIdentifier);
        MechanicalModelOutputPersistenceStep mechanicalModelOutputPersistenceStep = new(loggerFactory.CreateLogger<MechanicalModelOutputPersistenceStep>(), repository);

        IDataflowPipeline<ExperimentalDataSegmenterInput> pipeline = PipelineFactory
            .StartDataflow<ExperimentalDataSegmenterInput>(logger, cancellationToken: cancellationToken, progress: input.Progress)
            .WithLoggingErrors()
            .AddStep(segmenterStep, settings.SegmenterOptions)
            .AddBroadcastStep(experimentalDataFileWriterStep, options: settings.FileWriterOptions)
            .AddStep(downsamplerStep, PipelineStepOptions.Synchronous)
            .AddStep(accumulatorStep, PipelineStepOptions.Synchronous)
            .AddStep(curveFitterStep, settings.CurveFitterOptions)
            .Fork(
                curveFit => curveFit,
                identifier => identifier.AddStep(identifierBuilderStep),
                simulation => simulation
                    .AddStep(numericalSimulationStep)
                    .Fork(asymptoteStep, deltaStep, mechanicalModelOutputFileWriterStep)
            )
            .AddBroadcastStep(curveFitPersistenceStep, t => (t.Item2, t.Item1))
            .AddDataMapping(t => new MechanicalModelOutputPersistenceInput(
                MechanicalModelName: input.MechanicalModelName,
                CurveFitIdentifier: t.Item2,
                AsymptoteTime: t.Item3.Item1,
                SimulationDeltaOutput: t.Item3.Item2,
                FileMetadata: t.Item3.Item3,
                ExternalIdentifier: uniqueIdentifier))
            .AddStep(mechanicalModelOutputPersistenceStep)
            .BuildTerminal();

        await using (pipeline)
        {
            bool accepted = await pipeline.SendAsync(input.ToSegmenterInput(), cancellationToken).ConfigureAwait(false);
            if (!accepted)
            {
                logger.LogError("Failed to ingest experimental data into the processing pipeline. Identifier: {Identifier}. Input: {@Input}", uniqueIdentifier, input);
                input.Progress?.Report(new PipelineProgressEvent(
                    nameof(ExperimentalDataProcessingPipeline),
                    "Failed to ingest experimental data into the processing pipeline.",
                    PipelineStepState.Faulted,
                    Detail: uniqueIdentifier,
                    Timestamp: DateTimeOffset.UtcNow));
                return Result.CreateBadRequest("Failed to ingest experimental data into the processing pipeline.");
            }

            pipeline.Complete();
            await pipeline.Completion.ConfigureAwait(false);
        }

        FileMetadata rawProcessedMetadata = experimentalDataFileWriterStep.FileMetadata;
        StoredFileEntity storedFile = StoredFileEntity.Create(uniqueIdentifier, "ExperimentalDataRawProcessed", rawProcessedMetadata);

        Result<long> fileResult = await repository.TryInsertAsync(storedFile, cancellationToken).ConfigureAwait(false);
        if (!fileResult.Success && !fileResult.IsConflict)
        {
            logger.LogWarning("Failed to persist stored file record for processed experimental data: {Location}. Messages: {Messages}", rawProcessedMetadata.FullPath, string.Join("; ", fileResult.Messages));
        }

        input.Progress?.Report(new PipelineProgressEvent(
            nameof(ExperimentalDataProcessingPipeline),
            "Experimental data processing pipeline completed.",
            PipelineStepState.Completed,
            Detail: uniqueIdentifier,
            Timestamp: DateTimeOffset.UtcNow));

        return uniqueIdentifier;
    }
}
