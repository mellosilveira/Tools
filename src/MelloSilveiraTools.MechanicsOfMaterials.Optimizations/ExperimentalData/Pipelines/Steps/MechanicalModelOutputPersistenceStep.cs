using MelloSilveiraTools.Core.Managers.File.Models;
using MelloSilveiraTools.Core.Managers.File.Providers;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Core.Pipelines.Steps;
using MelloSilveiraTools.Database.RelationalDatabase.Models.Entities;
using MelloSilveiraTools.Database.Repositories;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Serializers.Json;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Pipelines.Steps;

/// <summary>
/// Pipeline step that converges all simulation analysis branches (Identifier, Asymptote, Delta, CSV) 
/// and persists the final simulation entity to the database.
/// </summary>
/// <param name="logger">The structured logger instance.</param>
/// <param name="fileStorageService">File storage service for querying metadata.</param>
/// <param name="repository">The database repository provider for persistence.</param>
/// <param name="pipelineIdentifier">Unique business identifier correlating the pipeline run.</param>
/// <param name="mechanicalModelName">Name of the mechanical constitutive model evaluated.</param>
public sealed class MechanicalModelOutputPersistenceStep(
    ILogger<MechanicalModelOutputPersistenceStep> logger,
    IFileStorageService fileStorageService,
    IRepository repository,
    string pipelineIdentifier,
    string mechanicalModelName)
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

        StorageEntryMetadata experimentalDataFileMetadata = await fileStorageService.GetMetadataAsync(input.ExperimentalDataFileLocation, cancellationToken).ConfigureAwait(false);
        long experimentalDataFileMetadataEntityId = await InsertStorageEntryMetadataAsync(experimentalDataFileMetadata, cancellationToken).ConfigureAwait(false);

        long simulationOutputFileMetadataEntityId = await InsertStorageEntryMetadataAsync(input.SimulationOutputFileMetadata, cancellationToken).ConfigureAwait(false);

        MechanicalModelSimulationEntity entity = new()
        {
            Identifier = identifierHash,
            ExternalIdentifier = pipelineIdentifier,
            MechanicalModelName = mechanicalModelName,
            CurveFitIdentifier = input.CurveFitOutputIdentifier,
            AsymptoteTime = input.AsymptoteTime,
            InitialOutputJson = initialOutputJson,
            FinalOutputJson = finalOutputJson,
            AbsoluteDeltaOutputJson = absoluteDeltaOutputJson,
            PercentageDeltaOutputJson = percentageDeltaOutputJson,
            ExperimentalDataFileMetadataId = experimentalDataFileMetadataEntityId,
            SimulationOutputFileMetadataId = simulationOutputFileMetadataEntityId,
        };

        Result<long> insertResult = await repository.TryInsertAsync(entity, cancellationToken).ConfigureAwait(false);
        if (insertResult.IsConflict)
            logger.LogWarning(
                "Entity for mechanical model output already exists in the database. Pipeline identifier: {PipelineIdentifier}. Mechanical Model: {MechanicalModelName}. Identifier: {Identifier}", 
                pipelineIdentifier,
                mechanicalModelName,
                identifierHash);
        else if (!insertResult.Success)
            logger.LogError(
                "Failed to insert entity for mechanical model output. Pipeline identifier: {PipelineIdentifier}. Mechanical Model: {MechanicalModelName}. Identifier: {Identifier} Entity: {@Entity}. Result: {@InsertResult}", 
                pipelineIdentifier,
                mechanicalModelName,
                identifierHash,
                entity, 
                insertResult);

        return identifierHash;
    }

    private async Task<long> InsertStorageEntryMetadataAsync(StorageEntryMetadata storageEntryMetadata, CancellationToken cancellationToken)
    {
        StorageEntryMetadataEntity entity = StorageEntryMetadataEntity.Create(storageEntryMetadata, pipelineIdentifier);
        Result<long> fileResult = await repository.TryInsertAsync(entity, cancellationToken).ConfigureAwait(false);
        if (fileResult.IsConflict)
            logger.LogWarning(
                "Entity for storage entry metadata already exists in the database. Pipeline: {PipelineIdentifier}. Storage Entry Metadata entity: {@Entity}", 
                pipelineIdentifier,
                entity);
        else if (!fileResult.Success)
            logger.LogError(
                "Failed to insert entity for storage entry metadata. Pipeline: {PipelineIdentifier}. Entity: {@Entity}. Result: {@InsertResult}", 
                pipelineIdentifier, 
                entity, 
                fileResult);

        return fileResult.Data;
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }
}
