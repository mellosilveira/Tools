using MelloSilveiraTools.Core.Pipelines;
using MelloSilveiraTools.Core.Pipelines.Dataflow;
using MelloSilveiraTools.Core.Providers;
using MelloSilveiraTools.Mathematics.Extensions;
using MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.SensitivityAnalysis.Steps;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.Models;
using Microsoft.Extensions.Logging;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.SensitivityAnalysis;

/// <summary>
/// Orchestrates the One-At-A-Time (OAAT) Parametric Sweep using the TPL Dataflow framework.
/// Configures the linear enrichment flow, the internal 3-way fork for mechanical calculations, 
/// the synchronization joiner, and the terminal CSV writer.
/// </summary>
public sealed class ParametricSweepPipeline(
    ILogger<ParametricSweepPipeline> logger,
    IMechanicalModelCalculatorFacade calculator,
    ServiceLocator serviceLocator)
{
    /// <summary>
    /// Builds and executes the parametric sweep pipeline for a given analysis request.
    /// </summary>
    public IDataflowPipeline<ParametricSweepInput> Build(double initialTime, double finalTime)
    {
        SweepIngestionStep ingestion = new(serviceLocator);

        InitialStressCalculationStep initialStep = new(calculator, initialTime);
        FinalStressCalculationStep finalStep = new(calculator, finalTime);
        AsymptoteCalculationStep asymptoteStep = new(calculator);

        ParametricSweepCsvWriterStep writer = new();

        return PipelineFactory.StartDataflow<ParametricSweepInput>(logger)
            .AddStep(ingestion)
            .Fork(
                calculationBranch => calculationBranch
                    .AddDataMapping(envelope => envelope.SimulationInput)
                    .Fork(initialStep, finalStep, asymptoteStep),
                branch => branch
            )
            .AddDataMapping(tuple =>
            {
                double initialStress = tuple.Item1.Item1;
                double finalStress = tuple.Item1.Item2;
                (double asymptoteTime, double asymptoteStress) = tuple.Item1.Item3;

                double aboluteDeltaStress = finalStress - initialStress;
                double percentualDeltaStress = initialStress.PercentageDifference(finalStress);

                SweepSimulationEnvelope envelope = tuple.Item2;

                return new ParametricSweepFinalPayload(
                    envelope.VariableName,
                    envelope.VariableValue,
                    initialStress,
                    finalStress,
                    asymptoteTime,
                    asymptoteStress,
                    aboluteDeltaStress,
                    percentualDeltaStress
                );
            })
            .AddBroadcastStep(writer)
            .BuildTerminal();
    }
}
