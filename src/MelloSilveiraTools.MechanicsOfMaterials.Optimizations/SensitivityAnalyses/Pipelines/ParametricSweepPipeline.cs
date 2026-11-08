using MelloSilveiraTools.Core.Managers.File.Providers;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Core.Pipelines;
using MelloSilveiraTools.Core.Pipelines.Dataflow;
using MelloSilveiraTools.Core.Providers;
using MelloSilveiraTools.Database.Repositories;
using MelloSilveiraTools.Mathematics.Extensions;
using MelloSilveiraTools.Mathematics.NumericalMethods.Differentiations;
using MelloSilveiraTools.Mathematics.NumericalMethods.RootFindingAlgorithms;
using MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.Generators;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.Pipelines.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.Pipelines.Steps;
using Microsoft.Extensions.Logging;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.Pipelines;

/// <summary>
/// Orchestrates the One-At-A-Time (OAAT) Parametric Sweep using the TPL Dataflow framework.
/// Configures a streamlined linear flow: parameter variation ingestion, concurrent evaluation of stresses,
/// partitioned CSV serialization, and terminal database persistence.
/// </summary>
/// <param name="loggerFactory">Logger factory for creating step-specific loggers.</param>
/// <param name="mechanicalModelCalculatorFactory">Factory resolving mechanical model calculator facades.</param>
/// <param name="differentiation">Numerical differentiation provider for asymptote detection.</param>
/// <param name="fileStorageService">Storage service responsible for writing partitioned sensitivity CSV files.</param>
/// <param name="repository">Database repository provider for persisting sweep metadata and stored file records.</param>
/// <param name="pipelineFactory">Factory responsible for bootstrapping Dataflow execution topologies.</param>
/// <param name="serviceLocator">Service locator resolving model-specific sweep generators and root-finding engines.</param>
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
        IParametricSweepGenerator generator = serviceLocator.GetRequiredKeyedService<IParametricSweepGenerator>(input.MechanicalModelName);
        IRootFinding rootFinding = serviceLocator.GetRequiredKeyedService<IRootFinding>(input.RootFindingAlgorithm);

        IDataflowPipeline<ParametricSweepInput> pipeline = pipelineFactory
            .CreateDataflow<ParametricSweepInput>(logger, input.Identifier, initialBufferSize: settings.IngestionBufferCapacity, cancellationToken: cancellationToken)
            .Fork(
                branch => branch.AddDataMapping(envelope => (envelope.VariableName, envelope.VariableValue)),
                calculationBranch => calculationBranch
                    .AddDataMapping(envelope => envelope.SimulationInput)
                    .Fork(
                        new InitialStressCalculationStep(calculator, input.InitialTime),
                        new FinalStressCalculationStep(calculator, input.FinalTime),
                        new AsymptoteCalculationStep(calculator, differentiation, rootFinding, input.BaseRootFindingInput))
            )
            .AddDataMapping(tuple =>
            {
                ((string variableName, double variableValue), (double initialStress, double finalStress, (double asymptoteTime, double asymptoteError, double asymptoteStress))) = tuple;
                return new ParametricSweepOutput(
                    variableName,
                    variableValue,
                    initialStress,
                    finalStress,
                    asymptoteTime,
                    asymptoteError,
                    asymptoteStress,
                    finalStress - initialStress,
                    initialStress.PercentageDifference(finalStress));
            })
            .AddGroupWhileStep(output => output.VariableName)
            .AddStep(new ParametricSweepCsvWriterStep(fileStorageService, input.Identifier))
            .AddCollectAllStep()
            .AddDataMapping(generatedFiles => new ParametricSweepPersistenceInput(input.Identifier, input, generatedFiles))
            .AddStep(new ParametricSweepPersistenceStep(loggerFactory.CreateLogger<ParametricSweepPersistenceStep>(), repository))
            .BuildTerminal();

        await using (pipeline)
        {
            foreach ((string variableName, IEnumerable<ParametricSweepInput> pipelineInputs) in generator.GenerateCombinations(input))
            {
                foreach (ParametricSweepInput pipelineInput in pipelineInputs)
                {
                    bool accepted = await pipeline.SendAsync(pipelineInput, cancellationToken).ConfigureAwait(false);
                    if (!accepted)
                        logger.LogError("Failed to ingest input into the parametric sweep pipeline for variable {variableName}. Input: {@Input}", variableName, pipelineInput);
                }
            }

            pipeline.Complete();
            await pipeline.Completion.ConfigureAwait(false);

            return input.Identifier;
        }
    }
}
