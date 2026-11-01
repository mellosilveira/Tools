using MelloSilveiraTools.Core.Managers.File.Providers;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Core.Pipelines;
using MelloSilveiraTools.Core.Pipelines.Dataflow;
using MelloSilveiraTools.Core.Pipelines.Steps;
using MelloSilveiraTools.Core.Providers;
using MelloSilveiraTools.Database.Repositories;
using MelloSilveiraTools.Mathematics.Extensions;
using MelloSilveiraTools.Mathematics.NumericalMethods.Differentiations;
using MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.Pipelines.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.Pipelines.Steps;
using Microsoft.Extensions.Logging;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.Pipelines;

/// <summary>
/// Orchestrates the One-At-A-Time (OAAT) Parametric Sweep using the TPL Dataflow framework.
/// Configures a streamlined linear flow: parameter variation ingestion, concurrent evaluation of stresses,
/// partitioned CSV serialization, and terminal database persistence.
/// </summary>
/// <param name="loggerFactory">Optional logger factory for creating step-specific loggers.</param>
/// <param name="mechanicalModelCalculatorFactory">Factory resolving mechanical model calculator facades.</param>
/// <param name="differentiation">Numerical differentiation provider for asymptote detection.</param>
/// <param name="fileStorageService">Storage service responsible for writing partitioned sensitivity CSV files.</param>
/// <param name="repository">Database repository provider for persisting sweep metadata and stored file records.</param>
/// <param name="pipelineFactory">Factory responsible for bootstrapping Dataflow execution topologies.</param>
/// <param name="serviceLocator">Service locator resolving model-specific sweep generators.</param>
/// <param name="settings">Mandatory execution topology settings for the parametric sweep pipeline.</param>
public sealed class ParametricSweepPipeline(
    ILoggerFactory loggerFactory,
    IMechanicalModelCalculatorFactory mechanicalModelCalculatorFactory,
    IDifferentiation differentiation,
    IFileStorageService fileStorageService,
    IRepository repository,
    PipelineFactory pipelineFactory,
    ServiceLocator serviceLocator,
    ParametricSweepSettings settings)
    : IPipeline<ParametricSweepInput, string>
{
    /// <inheritdoc/>
    public async Task<Result<string>> ProcessAsync(ParametricSweepInput input, CancellationToken cancellationToken = default)
    {
        ILogger<ParametricSweepPipeline> logger = loggerFactory.CreateLogger<ParametricSweepPipeline>();
        IMechanicalModelCalculatorFacade calculator = mechanicalModelCalculatorFactory.CreateCalculatorFacade(input.MechanicalModelName);

        ParametricSweepCsvWriterStep writer = new(fileStorageService, input.Identifier);

        IDataflowPipeline<ParametricSweepInput> pipeline = pipelineFactory
            .CreateDataflow<ParametricSweepInput>(logger, input.Identifier, initialBufferSize: settings.IngestionBufferCapacity, cancellationToken: cancellationToken)
            .AddStep(new SweepIngestionStep(serviceLocator))
            .Fork(
                calculationBranch => calculationBranch
                    .AddDataMapping(envelope => envelope.SimulationInput)
                    .Fork(
                        new InitialStressCalculationStep(calculator, input.InitialTime),
                        new FinalStressCalculationStep(calculator, input.FinalTime),
                        new AsymptoteCalculationStep(calculator, differentiation, input.MaxBracketIterations, input.BinarySearchIterations, input.AsymptoteTolerance)),
                branch => branch.AddDataMapping(envelope => (envelope.VariableName, envelope.VariableValue))
            )
            .AddDataMapping(tuple =>
            {
                ((double initialStress, double finalStress, (double asymptoteTime, double asymptoteStress)), (string variableName, double variableValue)) = tuple;
                return new ParametricSweepFinalPayload(
                    variableName,
                    variableValue,
                    initialStress,
                    finalStress,
                    asymptoteTime,
                    asymptoteStress,
                    finalStress - initialStress,
                    initialStress.PercentageDifference(finalStress));
            })
            .AddStep(new ParametricSweepCsvWriterStep(fileStorageService, input.Identifier))
            .AddCollectAllStep()
            .AddStep(new ParametricSweepFinalizationStep(writer, input.Identifier, input))
            .AddStep(new ParametricSweepPersistenceStep(loggerFactory.CreateLogger<ParametricSweepPersistenceStep>(), repository))
            .BuildTerminal();

        await using (pipeline)
        {
            bool accepted = await pipeline.SendAsync(input, cancellationToken).ConfigureAwait(false);
            if (!accepted)
            {
                logger.LogError("Failed to ingest input into the parametric sweep pipeline. Input: {@Input}", input);
                return Result.CreateBadRequest("Failed to ingest input into the parametric sweep pipeline.");
            }

            pipeline.Complete();
            await pipeline.Completion.ConfigureAwait(false);

            return input.Identifier;
        }
    }

    private sealed class ParametricSweepFinalizationStep(
        ParametricSweepCsvWriterStep writer,
        string identifier,
        ParametricSweepInput input)
        : IAsyncPipelineStep<IReadOnlyList<ParametricSweepFinalPayload>, ParametricSweepPersistenceInput>
    {
        public string Name => nameof(ParametricSweepFinalizationStep);

        public async Task<ParametricSweepPersistenceInput> ExecuteAsync(
            IReadOnlyList<ParametricSweepFinalPayload> payloads,
            CancellationToken cancellationToken = default)
        {
            await writer.DisposeAsync().ConfigureAwait(false);
            return new ParametricSweepPersistenceInput(
                identifier,
                input,
                writer.GeneratedFilesMetadata);
        }

        public ValueTask DisposeAsync()
        {
            GC.SuppressFinalize(this);
            return ValueTask.CompletedTask;
        }
    }
}
