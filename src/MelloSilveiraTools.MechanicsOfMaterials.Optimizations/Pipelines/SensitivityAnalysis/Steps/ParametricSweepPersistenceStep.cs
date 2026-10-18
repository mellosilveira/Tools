using MelloSilveiraTools.Core.Managers.File;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Core.Pipelines.Steps;
using MelloSilveiraTools.Database.RelationalDatabase.Models.Entities;
using MelloSilveiraTools.Database.Repositories;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.SensitivityAnalysis.Steps;

/// <summary>
/// Terminal pipeline step that persists the parametric sweep metadata entity (<see cref="ParametricSweepEntity"/>)
/// and registers all generated partitioned CSV files into the <c>stored_files</c> table (<see cref="StoredFileEntity"/>).
/// </summary>
/// <param name="logger">The structured logger instance.</param>
/// <param name="repository">The database repository provider for persistence.</param>
public sealed class ParametricSweepPersistenceStep(
    ILogger<ParametricSweepPersistenceStep> logger,
    IRepository repository)
    : IAsyncPipelineStep<ParametricSweepPersistenceInput, string>
{
    // TODO: FAZER IGUAL PASSO DE PERSISTIR MECHANICAL MODEL OUTPUT DA ANALISE.

    private const string Context = "SensitivityAnalysis";

    /// <inheritdoc />
    public string Name => nameof(ParametricSweepPersistenceStep);

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">Thrown when insertion of the parametric sweep entity fails in the database.</exception>
    public async Task<string> ExecuteAsync(ParametricSweepPersistenceInput input, CancellationToken cancellationToken = default)
    {
        string baselineParametersJson = OptimizationJsonOptions.Serialize(input.SweepInput.Baseline);
        string variableRangesJson = OptimizationJsonOptions.Serialize(new
        {
            input.SweepInput.ConstantVariables,
            input.SweepInput.ArrayVariables,
            input.SweepInput.StrainRange
        });

        ParametricSweepEntity entity = new()
        {
            ExternalIdentifier = input.ExternalIdentifier,
            MechanicalModelName = input.SweepInput.MechanicalModelName,
            BaseStrain = input.SweepInput.BaseStrain,
            InitialTime = input.SweepInput.InitialTime,
            FinalTime = input.SweepInput.FinalTime,
            TimeStep = input.SweepInput.TimeStep,
            AsymptoteTolerance = input.SweepInput.AsymptoteTolerance,
            BaselineParametersJson = baselineParametersJson,
            VariableRangesJson = variableRangesJson
        };

        Result<long> insertResult = await repository.TryInsertAsync(entity, cancellationToken).ConfigureAwait(false);
        if (insertResult.IsConflict)
        {
            logger.LogWarning("Entity for parametric sweep already exists in the database. ExternalIdentifier: {ExternalIdentifier}", input.ExternalIdentifier);
        }
        else if (!insertResult.Success)
        {
            logger.LogError("Failed to insert entity for parametric sweep. Entity: {@Entity}. Result: {@InsertResult}", entity, insertResult);
            throw new InvalidOperationException(string.Join(Environment.NewLine, insertResult.Messages));
        }

        foreach (FileMetadata fileMetadata in input.GeneratedFilesMetadata)
        {
            StoredFileEntity storedFile = StoredFileEntity.Create(input.ExternalIdentifier, Context, fileMetadata);

            Result<long> fileResult = await repository.TryInsertAsync(storedFile, cancellationToken).ConfigureAwait(false);
            if (!fileResult.Success && !fileResult.IsConflict)
            {
                logger.LogWarning(
                    "Failed to persist stored file record for sensitivity analysis output: {Location}. Messages: {Messages}",
                    fileMetadata.FullPath,
                    string.Join("; ", fileResult.Messages));
            }
        }

        return input.ExternalIdentifier;
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }
}
