using MelloSilveiraTools.Core.Managers.File.Models;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Core.Pipelines.Steps;
using MelloSilveiraTools.Database.RelationalDatabase.Models.Entities;
using MelloSilveiraTools.Database.Repositories;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Models.Range;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.ParametricSweep.Pipelines.Entities;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.ParametricSweep.Pipelines.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Serializers.Json;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.ParametricSweep.Pipelines.Steps;

/// <summary>
/// Terminal pipeline step that registers all generated partitioned CSV files into the <c>stored_files</c> table (<see cref="StorageEntryMetadataEntity"/>)
/// and persists the parametric sweep metadata entity (<see cref="ParametricSweepEntity"/>) correlating each swept variable with its range and file metadata.
/// </summary>
/// <param name="logger">The structured logger instance.</param>
/// <param name="repository">The database repository provider for persistence.</param>
/// <param name="sweepInput">The input configuration of the parametric sweep.</param>
public sealed class ParametricSweepPersistenceStep(
    ILogger<ParametricSweepPersistenceStep> logger,
    IRepository repository,
    ParametricSweepPipelineInput sweepInput)
    : IAsyncPipelineStep<(string VariableName, RangeParameters VariableRange, StorageEntryMetadata VariableOutputFile)[], long>
{
    /// <inheritdoc />
    public string Name => nameof(ParametricSweepPersistenceStep);

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">Thrown when insertion of the stored file entity or parametric sweep entity fails in the database.</exception>
    public async Task<long> ExecuteAsync((string VariableName, RangeParameters VariableRange, StorageEntryMetadata VariableOutputFile)[] input, CancellationToken cancellationToken = default)
    {
        Dictionary<string, (RangeParameters Range, long? StoredFileId)> sweptVariables = new(input.Length, StringComparer.Ordinal);

        foreach ((string variableName, RangeParameters variableRange, StorageEntryMetadata variableOutputFile) in input)
        {
            long? storedFileId = await InsertStoredFileAsync(variableOutputFile, sweepInput.Identifier, cancellationToken).ConfigureAwait(false);
            sweptVariables[variableName] = (variableRange, storedFileId);
        }

        return await PersistSweepEntityAsync(sweptVariables, cancellationToken).ConfigureAwait(false);
    }

    private async Task<long?> InsertStoredFileAsync(StorageEntryMetadata fileMetadata, string externalIdentifier, CancellationToken cancellationToken)
    {
        StorageEntryMetadataEntity storedFileEntity = StorageEntryMetadataEntity.Create(fileMetadata, externalIdentifier);
        Result<long> fileResult = await repository.TryInsertAsync(storedFileEntity, cancellationToken).ConfigureAwait(false);

        if (!fileResult.Success)
        {
            logger.LogError("Failed to insert stored file entity for parametric sweep. Entity: {@Entity}. Result: {@InsertResult}", storedFileEntity, fileResult);
            return null;
        }

        if (fileResult.IsConflict)
            logger.LogWarning("Entity for stored file already exists in the database. Storage Entry Metadata: {@StorageEntryMetadata}", fileMetadata);

        return fileResult.Data;
    }

    /// <summary>
    /// Persists the aggregated <see cref="ParametricSweepEntity"/> into the repository.
    /// </summary>
    /// <param name="sweptVariables">The materialized swept variable names mapped to their numerical range parameters and generated storage file entity identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The persisted database entity identifier.</returns>
    /// <remarks>
    /// When generating the deterministic configuration hash (<see cref="ParametricSweepEntity.Identifier"/>),
    /// the range definitions (<c>ConstantVariables</c>, <c>ArrayVariables</c>, and <c>StrainRange</c>) are explicitly stripped from <c>sweepInput</c>.
    /// This avoids duplicating range representations in the hash input and eliminates risks of state divergence,
    /// since all effective variation ranges are already materialized by upstream pipeline steps and tracked as the single source of truth in <paramref name="sweptVariables"/>.
    /// </remarks>
    private async Task<long> PersistSweepEntityAsync(IReadOnlyDictionary<string, (RangeParameters Range, long? StoredFileId)> sweptVariables, CancellationToken cancellationToken)
    {
        // Strip range collections from the baseline sweep input to avoid duplicate range representations in the deterministic hash.
        // The authoritative swept variable ranges and their persisted file references are supplied directly via sweptVariables.
        string sweepInputJson = OptimizationJsonOptions.Serialize(sweepInput with
        {
            ConstantVariables = null!,
            ArrayVariables = null!,
            StrainRange = null
        });
        string variableRangesJson = OptimizationJsonOptions.Serialize(sweptVariables);

        string rawDataToHash = string.Concat(sweepInputJson, variableRangesJson);
        string identifierHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawDataToHash)));
        ParametricSweepEntity entity = new()
        {
            Identifier = identifierHash,
            ExternalIdentifier = sweepInput.Identifier,
            MechanicalModelName = sweepInput.MechanicalModelName,
            BaseStrain = sweepInput.BaseStrain,
            RampTimeConsideration = sweepInput.RampTimeConsideration,
            RampTime = sweepInput.RampTime,
            InitialTime = sweepInput.InitialTime,
            FinalTime = sweepInput.FinalTime,
            TimeStep = sweepInput.TimeStep,
            RootFindingAlgorithm = sweepInput.RootFindingAlgorithm,
            BaseRootFindingInputJson = OptimizationJsonOptions.Serialize(sweepInput.BaseRootFindingInput),
            BaselineParametersJson = OptimizationJsonOptions.Serialize(sweepInput.Baseline),
            SweptVariablesJson = variableRangesJson
        };

        Result<long> insertResult = await repository.TryInsertAsync(entity, cancellationToken).ConfigureAwait(false);
        if (insertResult.IsConflict)
            logger.LogWarning("Entity for parametric sweep already exists in the database. Identifier: {Identifier}", identifierHash);
        else if (!insertResult.Success)
            logger.LogError("Failed to insert entity for parametric sweep. Entity: {@Entity}. Result: {@InsertResult}", entity, insertResult);

        return insertResult.Data;
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }
}
