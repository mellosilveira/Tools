using MelloSilveiraTools.Core.Managers.File.Providers;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Core.Pipelines;
using MelloSilveiraTools.Core.Pipelines.Dataflow;
using MelloSilveiraTools.Core.Pipelines.Models;
using MelloSilveiraTools.Core.Pipelines.Notifiers;
using MelloSilveiraTools.Core.Pipelines.Telemetry;
using MelloSilveiraTools.Core.Providers;
using MelloSilveiraTools.Database.Repositories;
using MelloSilveiraTools.Mathematics.NumericalMethods.Differentiations;
using MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.SensitivityAnalysis.Steps;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.SensitivityAnalysis;

/// <summary>
/// Orchestrates the One-At-A-Time (OAAT) Parametric Sweep using the TPL Dataflow framework.
/// Configures a streamlined linear flow: parameter variation ingestion, concurrent evaluation of stresses,
/// partitioned CSV serialization, and terminal database persistence.
/// </summary>
/// <param name="logger">The structured logger for the pipeline.</param>
/// <param name="progressNotifier">The progress notifier for emitting real-time pipeline execution events.</param>
/// <param name="mechanicalModelCalculatorFactory">Factory resolving mechanical model calculator facades.</param>
/// <param name="differentiation">Numerical differentiation provider for asymptote detection.</param>
/// <param name="fileStorageService">Storage service responsible for writing partitioned sensitivity CSV files.</param>
/// <param name="repository">Database repository provider for persisting sweep metadata and stored file records.</param>
/// <param name="serviceLocator">Service locator resolving model-specific sweep generators.</param>
/// <param name="timeProvider">Mandatory time provider used for timestamping pipeline events and telemetry.</param>
/// <param name="loggerFactory">Optional logger factory for creating step-specific loggers.</param>
/// <param name="notificationSettings">Optional settings defining adaptive throttling thresholds and item notification intervals for pipeline progress.</param>
public sealed class ParametricSweepPipeline(
    ILogger<ParametricSweepPipeline> logger,
    IPipelineProgressNotifier progressNotifier,
    IMechanicalModelCalculatorFactory mechanicalModelCalculatorFactory,
    IDifferentiation differentiation,
    IFileStorageService fileStorageService,
    IRepository repository,
    ServiceLocator serviceLocator,
    TimeProvider timeProvider,
    ILoggerFactory? loggerFactory = null,
    PipelineProgressNotificationSettings? notificationSettings = null)
    : IParametricSweepPipeline
{
    /// <inheritdoc/>
    public async Task<Result<string>> ProcessAsync(ParametricSweepInput input, CancellationToken cancellationToken = default)
    {
        string uniqueIdentifier = string.IsNullOrWhiteSpace(input.Identifier) ? Guid.NewGuid().ToString("N") : input.Identifier;

        await progressNotifier.NotifyAsync(
            new PipelineProgressEvent
            {
                StepName = nameof(ParametricSweepPipeline),
                Description = "Parametric sweep pipeline started.",
                State = PipelineStepState.Started,
                Timestamp = timeProvider.GetUtcNow(),
                Details = uniqueIdentifier
            },
            cancellationToken).ConfigureAwait(false);

        SweepIngestionStep ingestionStep = new(serviceLocator);

        IMechanicalModelCalculatorFacade calculator = mechanicalModelCalculatorFactory.CreateCalculatorFacade(input.MechanicalModelName);
        InitialStressCalculationStep initialStep = new(calculator, input.InitialTime);
        FinalStressCalculationStep finalStep = new(calculator, input.FinalTime);
        AsymptoteCalculationStep asymptoteStep = new(calculator, differentiation, input.AsymptoteTolerance);

        ParametricSweepCsvWriterStep writer = new(fileStorageService, uniqueIdentifier);
        ILogger<ParametricSweepPersistenceStep> persistenceLogger = loggerFactory?.CreateLogger<ParametricSweepPersistenceStep>()
            ?? NullLogger<ParametricSweepPersistenceStep>.Instance;
        ParametricSweepPersistenceStep persistenceStep = new(persistenceLogger, repository);

        IDataflowPipeline<ParametricSweepInput> pipeline = PipelineFactory
            .StartDataflow<ParametricSweepInput>(logger, progressNotifier, timeProvider, notificationSettings, cancellationToken: cancellationToken)
            .AddStep(ingestionStep)
            .Fork(
                calculationBranch => calculationBranch
                    .AddDataMapping(envelope => envelope.SimulationInput)
                    .Fork(initialStep, finalStep, asymptoteStep),
                branch => branch.AddDataMapping(envelope => (envelope.VariableName, envelope.VariableValue))
            )
            .AddDataMapping(tuple => ParametricSweepFinalPayload.Create(tuple.Item1, tuple.Item2))
            .AddCollectAllStep()
            .AddStep(writer)
            .AddDataMapping(metadata => new ParametricSweepPersistenceInput(uniqueIdentifier, input, metadata))
            .AddStep(persistenceStep)
            .BuildTerminal();

        await using (pipeline)
        {
            bool accepted = await pipeline.SendAsync(input, cancellationToken).ConfigureAwait(false);
            if (!accepted)
            {
                logger.LogError("Failed to ingest input into the parametric sweep pipeline. Identifier: {Identifier}. Input: {@Input}", uniqueIdentifier, input);
                await progressNotifier.NotifyAsync(
                    new PipelineProgressEvent
                    {
                        StepName = nameof(ParametricSweepPipeline),
                        Description = "Failed to ingest input into the parametric sweep pipeline.",
                        State = PipelineStepState.Faulted,
                        Timestamp = timeProvider.GetUtcNow(),
                        Details = uniqueIdentifier
                    },
                    cancellationToken).ConfigureAwait(false);
                return Result.CreateBadRequest("Failed to ingest input into the parametric sweep pipeline.");
            }

            pipeline.Complete();
            await pipeline.Completion.ConfigureAwait(false);

            await progressNotifier.NotifyAsync(
                new PipelineProgressEvent
                {
                    StepName = nameof(ParametricSweepPipeline),
                    Description = "Parametric sweep pipeline completed.",
                    State = PipelineStepState.Completed,
                    Timestamp = timeProvider.GetUtcNow(),
                    Details = uniqueIdentifier
                },
                cancellationToken).ConfigureAwait(false);

            return uniqueIdentifier;
        }
    }
}
