using MelloSilveiraTools.Core.Managers.File;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Core.Pipelines;
using MelloSilveiraTools.Core.Pipelines.Dataflow;
using MelloSilveiraTools.Core.Pipelines.Models;
using MelloSilveiraTools.Core.Providers;
using MelloSilveiraTools.Database.Repositories;
using MelloSilveiraTools.Mathematics.Extensions;
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
/// <param name="mechanicalModelCalculatorFactory">Factory resolving mechanical model calculator facades.</param>
/// <param name="differentiation">Numerical differentiation provider for asymptote detection.</param>
/// <param name="serviceLocator">Service locator resolving model-specific sweep generators.</param>
/// <param name="fileStorageService">Storage service responsible for writing partitioned sensitivity CSV files.</param>
/// <param name="repository">Database repository provider for persisting sweep metadata and stored file records.</param>
/// <param name="loggerFactory">Optional logger factory for creating step-specific loggers.</param>
public sealed class ParametricSweepPipeline(
    ILogger<ParametricSweepPipeline> logger,
    IMechanicalModelCalculatorFactory mechanicalModelCalculatorFactory,
    IDifferentiation differentiation,
    ServiceLocator serviceLocator,
    IFileStorageService fileStorageService,
    IRepository repository,
    ILoggerFactory? loggerFactory = null)
    : IParametricSweepPipeline
{
    /// <inheritdoc/>
    public async Task<Result<string>> ProcessAsync(ParametricSweepInput input, CancellationToken cancellationToken = default)
    {
        string uniqueIdentifier = string.IsNullOrWhiteSpace(input.Identifier) ? Guid.NewGuid().ToString("N") : input.Identifier;

        input.Progress?.Report(new PipelineProgressEvent(
            nameof(ParametricSweepPipeline),
            "Parametric sweep pipeline started.",
            PipelineStepState.Started,
            Detail: uniqueIdentifier,
            Timestamp: DateTimeOffset.UtcNow));

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
            .StartDataflow<ParametricSweepInput>(logger, cancellationToken: cancellationToken, progress: input.Progress)
            .AddStep(ingestionStep)
            .Fork(
                calculationBranch => calculationBranch
                    .AddDataMapping(envelope => envelope.SimulationInput)
                    .Fork(initialStep, finalStep, asymptoteStep),
                branch => branch.AddDataMapping(envelope => (envelope.VariableName, envelope.VariableValue))
            )
            .AddDataMapping(tuple =>
            {
                double initialStress = tuple.Item1.Item1;
                double finalStress = tuple.Item1.Item2;
                (double asymptoteTime, double asymptoteStress) = tuple.Item1.Item3;

                double absoluteDeltaStress = finalStress - initialStress;
                double percentualDeltaStress = initialStress.PercentageDifference(finalStress);

                (string variableName, double variableValue) = tuple.Item2;

                return new ParametricSweepFinalPayload(
                    variableName,
                    variableValue,
                    initialStress,
                    finalStress,
                    asymptoteTime,
                    asymptoteStress,
                    absoluteDeltaStress,
                    percentualDeltaStress
                );
            })
            .AddStep<ParametricSweepFinalPayload>(writer)
            .AddCollectAllStep()
            .AddDataMapping(_ =>
            {
                writer.DisposeAsync().AsTask().GetAwaiter().GetResult();
                return new ParametricSweepPersistenceInput(
                    uniqueIdentifier,
                    input,
                    writer.GeneratedFilesMetadata);
            })
            .AddStep(persistenceStep)
            .BuildTerminal();

        await using (pipeline)
        {
            bool accepted = await pipeline.SendAsync(input, cancellationToken).ConfigureAwait(false);
            if (!accepted)
            {
                logger.LogError("Failed to ingest input into the parametric sweep pipeline. Identifier: {Identifier}. Input: {@Input}", uniqueIdentifier, input);
                input.Progress?.Report(new PipelineProgressEvent(
                    nameof(ParametricSweepPipeline),
                    "Failed to ingest input into the parametric sweep pipeline.",
                    PipelineStepState.Faulted,
                    Detail: uniqueIdentifier,
                    Timestamp: DateTimeOffset.UtcNow));
                return Result.CreateBadRequest("Failed to ingest input into the parametric sweep pipeline.");
            }

            pipeline.Complete();
            await pipeline.Completion.ConfigureAwait(false);

            input.Progress?.Report(new PipelineProgressEvent(
                nameof(ParametricSweepPipeline),
                "Parametric sweep pipeline completed.",
                PipelineStepState.Completed,
                Detail: uniqueIdentifier,
                Timestamp: DateTimeOffset.UtcNow));

            return uniqueIdentifier;
        }
    }
}
