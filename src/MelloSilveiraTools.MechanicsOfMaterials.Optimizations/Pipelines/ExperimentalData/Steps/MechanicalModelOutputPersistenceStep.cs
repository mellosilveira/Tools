using MelloSilveiraTools.Core.Managers.File;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Core.Pipelines.Steps;
using MelloSilveiraTools.Database.RelationalDatabase.Models.Entities;
using MelloSilveiraTools.Database.Repositories;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Models;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Steps;

/// <summary>
/// Pipeline step that converges all simulation analysis branches (Identifier, Asymptote, Delta, CSV) 
/// and persists the final simulation entity to the database.
/// </summary>
/// <param name="logger">The structured logger instance.</param>
/// <param name="repository">The database repository provider for persistence.</param>
public sealed class MechanicalModelOutputPersistenceStep(
    ILogger<MechanicalModelOutputPersistenceStep> logger,
    IRepository repository)
    : IAsyncPipelineStep<MechanicalModelOutputPersistenceInput, string>
{
    /// <inheritdoc />
    public string Name => nameof(MechanicalModelOutputPersistenceStep);

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">Thrown when insertion of the simulation output entity fails in the database.</exception>
    public async Task<string> ExecuteAsync(MechanicalModelOutputPersistenceInput input, CancellationToken cancellationToken = default)
    {
        string initialOutputJson = OptimizationJsonOptions.Serialize(input.SimulationDeltaOutput.InitialOutput);
        string finalOutputJson = OptimizationJsonOptions.Serialize(input.SimulationDeltaOutput.FinalOutput);
        string absoluteDeltaOutputJson = OptimizationJsonOptions.Serialize(input.SimulationDeltaOutput.AbsoluteDelta);
        string percentageDeltaOutputJson = OptimizationJsonOptions.Serialize(input.SimulationDeltaOutput.PercentageDelta);

        string rawDataToHash = string.Concat(initialOutputJson, finalOutputJson, absoluteDeltaOutputJson, percentageDeltaOutputJson);
        string identifierHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawDataToHash)));

        MechanicalModelSimulationEntity entity = new()
        {
            MechanicalModelName = input.MechanicalModelName,
            Identifier = identifierHash,
            CurveFitIdentifier = input.CurveFitIdentifier,
            ExternalIdentifier = input.ExternalIdentifier,
            OutputFileName = input.FileMetadata.FileName,
            AsymptoteTime = input.AsymptoteTime,
            InitialOutputJson = initialOutputJson,
            FinalOutputJson = finalOutputJson,
            AbsoluteDeltaOutputJson = absoluteDeltaOutputJson,
            PercentageDeltaOutputJson = percentageDeltaOutputJson
        };

        Result<long> insertResult = await repository.TryInsertAsync(entity, cancellationToken).ConfigureAwait(false);
        if (insertResult.IsConflict)
        {
            logger.LogWarning("Entity for mechanical model output already exists in the database. Identifier: {Identifier}", identifierHash);
        }
        else if (!insertResult.Success)
        {
            logger.LogError("Failed to insert entity for mechanical model output. Entity: {@Entity}. Result: {@InsertResult}", entity, insertResult);
            throw new InvalidOperationException(string.Join(Environment.NewLine, insertResult.Messages));
        }

        StoredFileEntity storedFile = StoredFileEntity.Create(
            input.ExternalIdentifier ?? identifierHash,
            "ExperimentalDataSimulation",
            input.FileMetadata);

        Result<long> fileResult = await repository.TryInsertAsync(storedFile, cancellationToken).ConfigureAwait(false);
        if (!fileResult.Success && !fileResult.IsConflict)
        {
            logger.LogWarning("Failed to persist stored file record for simulation output: {Location}. Messages: {Messages}", input.FileMetadata.FullPath, string.Join("; ", fileResult.Messages));
        }

        return identifierHash;
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }
}
