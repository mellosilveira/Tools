using MelloSilveiraTools.Core.Managers.File.Providers;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Core.Pipelines;
using MelloSilveiraTools.Core.Pipelines.Dataflow;
using MelloSilveiraTools.Core.Pipelines.Models;
using MelloSilveiraTools.Core.Pipelines.Notifiers;
using MelloSilveiraTools.Core.Pipelines.Telemetry;
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
/// <param name="progressNotifier">The progress notifier for emitting real-time pipeline execution events.</param>
/// <param name="fileStorageService">The file storage service used for file writing steps.</param>
/// <param name="differentiation">Numerical differentiation provider used for segment classification.</param>
/// <param name="stepFactory">Factory resolving model-specific curve fitter steps.</param>
/// <param name="repository">Database repository provider for persisting curve fits and simulations.</param>
/// <param name="calculatorFactory">Factory resolving mechanical model calculators.</param>
/// <param name="timeProvider">Mandatory time provider used for timestamping pipeline events and telemetry.</param>
/// <param name="settings">Pipeline configuration settings and step options.</param>
/// <param name="notificationSettings">Optional settings defining adaptive throttling thresholds and item notification intervals for pipeline progress.</param>
public sealed class ExperimentalDataProcessingPipeline(
    ILoggerFactory loggerFactory,
    IPipelineProgressNotifier progressNotifier,
    IFileStorageService fileStorageService,
    IDifferentiation differentiation,
    IMechanicalModelStepFactory stepFactory,
    IRepository repository,
    IMechanicalModelCalculatorFactory calculatorFactory,
    TimeProvider timeProvider,
    ExperimentalDataSettings settings,
    PipelineProgressNotificationSettings? notificationSettings = null)
    : IExperimentalDataProcessingPipeline
{
    private const string RawProcessedFileContext = "ExperimentalDataRawProcessed";

    /// <inheritdoc/>
    public async Task<Result<string>> ProcessAsync(ExperimentalDataProcessingInput input, CancellationToken cancellationToken = default)
    {
        ILogger<ExperimentalDataProcessingPipeline> logger = loggerFactory.CreateLogger<ExperimentalDataProcessingPipeline>();

        string uniqueIdentifier = string.IsNullOrWhiteSpace(input.Identifier) ? Guid.NewGuid().ToString("N") : input.Identifier;

        await progressNotifier.NotifyAsync(
            new PipelineProgressEvent
            {
                StepName = nameof(ExperimentalDataProcessingPipeline),
                Description = "Experimental data processing pipeline started.",
                State = PipelineStepState.Started,
                Timestamp = timeProvider.GetUtcNow(),
                Details = uniqueIdentifier
            },
            cancellationToken).ConfigureAwait(false);

        ExperimentalDataSegmenterStep segmenterStep = new(loggerFactory.CreateLogger<ExperimentalDataSegmenterStep>(), differentiation);
        ExperimentalDataFileWriterStep experimentalDataFileWriterStep = new(fileStorageService, uniqueIdentifier);
        StoredFilePersistenceStep storedFilePersistenceStep = new(loggerFactory.CreateLogger<StoredFilePersistenceStep>(), repository, uniqueIdentifier, RawProcessedFileContext);
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
            .StartDataflow<ExperimentalDataSegmenterInput>(logger, progressNotifier, timeProvider, notificationSettings, cancellationToken: cancellationToken)
            .WithLoggingErrors()
            .AddStep(segmenterStep, settings.SegmenterOptions)
            .AddBroadcastFork(
                fileBranch => fileBranch
                    .AddStep(experimentalDataFileWriterStep, settings.FileWriterOptions)
                    .AddStep(storedFilePersistenceStep),
                options: settings.FileWriterOptions)
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
                StorageEntryMetadata: t.Item3.Item3,
                ExternalIdentifier: uniqueIdentifier))
            .AddStep(mechanicalModelOutputPersistenceStep)
            .BuildTerminal();

        await using (pipeline)
        {
            bool accepted = await pipeline.SendAsync(input.ToSegmenterInput(), cancellationToken).ConfigureAwait(false);
            if (!accepted)
            {
                logger.LogError("Failed to ingest experimental data into the processing pipeline. Identifier: {Identifier}. Input: {@Input}", uniqueIdentifier, input);
                await progressNotifier.NotifyAsync(
                    new PipelineProgressEvent
                    {
                        StepName = nameof(ExperimentalDataProcessingPipeline),
                        Description = "Failed to ingest experimental data into the processing pipeline.",
                        State = PipelineStepState.Faulted,
                        Timestamp = timeProvider.GetUtcNow(),
                        Details = uniqueIdentifier
                    },
                    cancellationToken).ConfigureAwait(false);
                return Result.CreateBadRequest("Failed to ingest experimental data into the processing pipeline.");
            }

            pipeline.Complete();
            await pipeline.Completion.ConfigureAwait(false);
        }

        await progressNotifier.NotifyAsync(
            new PipelineProgressEvent
            {
                StepName = nameof(ExperimentalDataProcessingPipeline),
                Description = "Experimental data processing pipeline completed.",
                State = PipelineStepState.Completed,
                Timestamp = timeProvider.GetUtcNow(),
                Details = uniqueIdentifier
            },
            cancellationToken).ConfigureAwait(false);

        return uniqueIdentifier;
    }
}
