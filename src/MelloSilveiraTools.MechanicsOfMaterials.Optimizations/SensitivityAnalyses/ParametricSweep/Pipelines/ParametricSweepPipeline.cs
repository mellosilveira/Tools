using MelloSilveiraTools.Core.Managers.File.Providers;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Core.Pipelines;
using MelloSilveiraTools.Core.Pipelines.Dataflow;
using MelloSilveiraTools.Core.Providers;
using MelloSilveiraTools.Database.Repositories;
using MelloSilveiraTools.Mathematics.NumericalMethods.Differentiations;
using MelloSilveiraTools.Mathematics.NumericalMethods.RootFindingAlgorithms;
using MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.ParametricSweep.Pipelines.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.ParametricSweep.Pipelines.Steps;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.ParametricSweep.Pipelines.Steps.Generators;
using Microsoft.Extensions.Logging;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.ParametricSweep.Pipelines;

/// <summary>
/// Orchestrates the One-At-A-Time (OAAT) Parametric Sweep using the TPL Dataflow framework.
/// Configures a streamlined linear flow: parameter variation ingestion as a generator step,
/// simulation of stresses and asymptotes, partitioned CSV serialization, item accumulation, and terminal persistence.
/// </summary>
/// <param name="loggerFactory">Logger factory for creating step-specific loggers.</param>
/// <param name="mechanicalModelCalculatorFactory">Factory resolving mechanical model calculator facades.</param>
/// <param name="differentiation">Numerical differentiation provider for asymptote detection.</param>
/// <param name="fileStorageService">Storage service responsible for writing partitioned sensitivity CSV files.</param>
/// <param name="repository">Database repository provider for persisting sweep metadata and stored file records.</param>
/// <param name="pipelineFactory">Factory responsible for bootstrapping Dataflow execution topologies.</param>
/// <param name="serviceLocator">Service locator resolving model-specific sweep generator steps and root-finding engines.</param>
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
    : IPipeline<ParametricSweepPipelineInput, string>
{
    /// <inheritdoc/>
    public async Task<Result<string>> ProcessAsync(ParametricSweepPipelineInput input, CancellationToken cancellationToken = default)
    {
        ILogger<ParametricSweepPipeline> logger = loggerFactory.CreateLogger<ParametricSweepPipeline>();

        IMechanicalModelCalculatorFacade calculator = mechanicalModelCalculatorFactory.CreateCalculatorFacade(input.MechanicalModelName);
        IParametricSweepGeneratorStep generator = serviceLocator.GetRequiredKeyedService<IParametricSweepGeneratorStep>(input.MechanicalModelName);
        IRootFinding rootFinding = serviceLocator.GetRequiredKeyedService<IRootFinding>(input.RootFindingAlgorithm);

        IDataflowPipeline<ParametricSweepPipelineInput> pipeline = pipelineFactory
            .CreateDataflow<ParametricSweepPipelineInput>(logger, input.Identifier, initialBufferSize: settings.IngestionBufferCapacity, cancellationToken: cancellationToken)
            .AddStep(generator)
            .AddStep(new ParametricSweepSimulationStep(calculator, differentiation, rootFinding, input.BaseRootFindingInput, input.InitialTime, input.FinalTime))
            .AddStep(new ParametricSweepCsvWriterStep(fileStorageService, input.Identifier))
            .AddCollectAllStep()
            .AddStep(new ParametricSweepPersistenceStep(loggerFactory.CreateLogger<ParametricSweepPersistenceStep>(), repository, input))
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
}
