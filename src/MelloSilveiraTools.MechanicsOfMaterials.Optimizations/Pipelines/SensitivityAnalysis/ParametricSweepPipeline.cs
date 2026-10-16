using MelloSilveiraTools.Core.Managers.File;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Core.Pipelines;
using MelloSilveiraTools.Core.Pipelines.Dataflow;
using MelloSilveiraTools.Core.Providers;
using MelloSilveiraTools.Mathematics.Extensions;
using MelloSilveiraTools.Mathematics.NumericalMethods.Differentiations;
using MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.SensitivityAnalysis.Steps;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.Models;
using Microsoft.Extensions.Logging;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.SensitivityAnalysis;

/// <summary>
/// Orchestrates the One-At-A-Time (OAAT) Parametric Sweep using the TPL Dataflow framework.
/// Configures a streamlined linear flow: parameter variation ingestion, concurrent evaluation of stresses,
/// and terminal partitioned CSV serialization.
/// </summary>
public sealed class ParametricSweepPipeline(
    ILogger<ParametricSweepPipeline> logger,
    IMechanicalModelCalculatorFactory mechanicalModelCalculatorFactory,
    IDifferentiation differentiation,
    ServiceLocator serviceLocator,
    IFileStorageService? fileStorageService = null)
{
    /// <summary>
    /// Builds and executes the parametric sweep pipeline for a given analysis request.
    /// </summary>
    public async Task<Result<string>> ProcessAsync(ParametricSweepInput input, CancellationToken cancellationToken = default)
    {
        string uniqueIdentifier = string.IsNullOrWhiteSpace(input.Identifier) ? Guid.NewGuid().ToString("N") : input.Identifier;

        SweepIngestionStep ingestionStep = new(serviceLocator);

        IMechanicalModelCalculatorFacade calculator = mechanicalModelCalculatorFactory.CreateCalculatorFacade(input.MechanicalModelName);
        InitialStressCalculationStep initialStep = new(calculator, input.InitialTime);
        FinalStressCalculationStep finalStep = new(calculator, input.FinalTime);
        AsymptoteCalculationStep asymptoteStep = new(calculator, differentiation, input.AsymptoteTolerance);

        ParametricSweepCsvWriterStep writer = new(input.OutputDirectory, fileStorageService);

        IDataflowPipeline<ParametricSweepInput> pipeline = PipelineFactory.StartDataflow<ParametricSweepInput>(logger, cancellationToken: cancellationToken)
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
            .AddBroadcastStep(writer)
            .BuildTerminal();

        await using (pipeline)
        {
            bool accepted = await pipeline.SendAsync(input, cancellationToken).ConfigureAwait(false);
            if (!accepted)
            {
                logger.LogError("Failed to ingest input into the parametric sweep pipeline. Identifier: {Identifier}. Input: {@Input}", uniqueIdentifier, input);
                return Result.CreateBadRequest("Failed to ingest input into the parametric sweep pipeline.");
            }

            pipeline.Complete();
            await pipeline.Completion.ConfigureAwait(false);

            return uniqueIdentifier;
        }
    }
}
