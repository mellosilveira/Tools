using MelloSilveiraTools.Core.Managers.File;
using MelloSilveiraTools.Core.Pipelines.Steps;
using MelloSilveiraTools.Database.Repositories;
using MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Converters;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Handlers;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Models;
using System.Text.Json;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Steps;

/// <summary>
/// Pipeline step that acts as an orchestrator for the forward numerical simulation phase.
/// Composes internal stream handlers for generating CSV files, detecting asymptotes, and computing deltas
/// without broadcasting to the larger TPL Dataflow pipeline.
/// </summary>
/// <param name="fileManager">The file manager service used by CSV writers.</param>
/// <param name="calculatorFactory">The factory creating optimized mechanical model calculation facades.</param>
/// <param name="repository">The database repository used to persist simulation entities.</param>
/// <param name="outputFileUri">The destination directory URI for simulation output files.</param>
/// <param name="asymptoteConsecutivePointsThreshold">The consecutive stabilized points threshold for asymptote detection.</param>
public class SimulationOrchestratorStep(
    IFileManager fileManager,
    IMechanicalModelCalculatorFactory calculatorFactory,
    IRepository repository,
    string outputFileUri,
    int asymptoteConsecutivePointsThreshold = 10)
    : IAsyncPipelineStep<MechanicalModelSimulationStepInput, MechanicalModelCurveFitOutput>
{
    private static readonly JsonSerializerOptions JsonOptions = new() { Converters = { new SignificantFiguresDoubleJsonConverter(7) } };

    /// <inheritdoc />
    public string Name => nameof(SimulationOrchestratorStep);

    /// <inheritdoc />
    public async Task<MechanicalModelCurveFitOutput> ExecuteAsync(MechanicalModelSimulationStepInput input, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Create the calculation facade tailored to the specific model input
        IMechanicalModelCalculatorFacade facade = calculatorFactory.CreateCalculatorFacade(input.MechanicalModelInput);

        // Instantiate local stream handlers for reactive, single-pass simulation telemetry and data generation
        await using SimulationCsvHandler csvHandler = new(
            fileManager,
            outputFileUri,
            input.CurveFitIdentifier,
            input.CurveFit.MechanicalModelName,
            input.CurveFit.MechanicalBehaviorType,
            input.CurveFit.ViscoelasticEffect);

        await using SimulationAsymptoteHandler asymptoteHandler = new(input.CurveFit.ViscoelasticEffect, asymptoteConsecutivePointsThreshold);

        await using SimulationDeltaHandler deltaHandler = new();

        // Forward numerical simulation loop evaluating model output across experimental/extended time points
        double[] times = input.ExperimentalTimePoints;
        for (int i = 0; i < times.Length; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            MechanicalModelOutput output = facade.Calculate(times[i]);

            await csvHandler.OnNextAsync(output, i).ConfigureAwait(false);
            await asymptoteHandler.OnNextAsync(output, i).ConfigureAwait(false);
            await deltaHandler.OnNextAsync(output, i).ConfigureAwait(false);
        }

        // Finalize all stream handlers and collect results
        FileData fileData = await csvHandler.CompleteAsync().ConfigureAwait(false);
        double? asymptoteTime = await asymptoteHandler.CompleteAsync().ConfigureAwait(false);
        SimulationDeltaResult delta = await deltaHandler.CompleteAsync().ConfigureAwait(false);

        string identifier = input.CurveFitIdentifier;

        MechanicalModelSimulationEntity simulationEntity = new()
        {
            Identifier = identifier,
            CurveFitIdentifier = identifier,
            MechanicalModelName = input.CurveFit.MechanicalModelName,
            OutputFileName = fileData.Name,
            AsymptoteTime = asymptoteTime,
            InitialOutputJson = JsonSerializer.Serialize((object)delta.InitialOutput, JsonOptions),
            FinalOutputJson = JsonSerializer.Serialize((object)delta.FinalOutput, JsonOptions),
            AbsoluteDeltaOutputJson = JsonSerializer.Serialize((object)delta.AbsoluteDelta, JsonOptions),
            PercentageDeltaOutputJson = JsonSerializer.Serialize((object)delta.PercentageDelta, JsonOptions)
        };

        await repository.TryInsertAsync(simulationEntity, cancellationToken).ConfigureAwait(false);

        return input.CurveFit;
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }
}
