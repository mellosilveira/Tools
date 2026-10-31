using MelloSilveiraTools.Core.Managers.File.Models;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Core.Pipelines.Steps;
using MelloSilveiraTools.Database.RelationalDatabase.Models.Entities;
using MelloSilveiraTools.Database.Repositories;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Serializers.Json;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.SensitivityAnalysis.Steps;

/// <summary>
/// Terminal pipeline step that persists the parametric sweep metadata entity (<see cref="ParametricSweepEntity"/>)
/// and registers all generated partitioned CSV files into the <c>stored_files</c> table (<see cref="StorageEntryMetadataEntity"/>).
/// </summary>
/// <param name="logger">The structured logger instance.</param>
/// <param name="repository">The database repository provider for persistence.</param>
public sealed class ParametricSweepPersistenceStep(
    ILogger<ParametricSweepPersistenceStep> logger,
    IRepository repository)
    : IAsyncPipelineStep<ParametricSweepPersistenceInput, string>
{
    private const string Context = "SensitivityAnalysis";

    /// <inheritdoc />
    public string Name => nameof(ParametricSweepPersistenceStep);

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">Thrown when insertion of the parametric sweep or stored file entity fails in the database.</exception>
    public async Task<string> ExecuteAsync(ParametricSweepPersistenceInput input, CancellationToken cancellationToken = default)
    {
        string baselineParametersJson = OptimizationJsonOptions.Serialize(input.SweepInput.Baseline);
        string variableRangesJson = OptimizationJsonOptions.Serialize(new
        {
            input.SweepInput.ConstantVariables,
            input.SweepInput.ArrayVariables,
            input.SweepInput.StrainRange
        });

        string rawDataToHash = string.Concat(
            input.SweepInput.MechanicalModelName,
            baselineParametersJson,
            variableRangesJson,
            FormattableString.Invariant($"{input.SweepInput.BaseStrain:G7}|{input.SweepInput.RampTimeConsideration}|{input.SweepInput.RampTime:G7}|{input.SweepInput.InitialTime:G7}|{input.SweepInput.FinalTime:G7}|{input.SweepInput.TimeStep:G7}|{input.SweepInput.AsymptoteTolerance:G7}"));
        string identifierHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawDataToHash)));

        foreach (StorageEntryMetadata fileMetadata in input.GeneratedFilesMetadata)
        {
            await InsertStoredFile(fileMetadata, input.ExternalIdentifier, cancellationToken).ConfigureAwait(false);
        }

        ParametricSweepEntity entity = new()
        {
            Identifier = identifierHash,
            ExternalIdentifier = input.ExternalIdentifier,
            MechanicalModelName = input.SweepInput.MechanicalModelName,
            BaseStrain = input.SweepInput.BaseStrain,
            RampTimeConsideration = input.SweepInput.RampTimeConsideration,
            RampTime = input.SweepInput.RampTime,
            InitialTime = input.SweepInput.InitialTime,
            FinalTime = input.SweepInput.FinalTime,
            TimeStep = input.SweepInput.TimeStep,
            AsymptoteTolerance = input.SweepInput.AsymptoteTolerance,
            BaselineParametersJson = baselineParametersJson,
            VariableRangesJson = variableRangesJson
        };

        Result<long> insertResult = await repository.TryInsertAsync(entity, cancellationToken).ConfigureAwait(false);
        if (insertResult.IsConflict)
            logger.LogWarning("Entity for parametric sweep already exists in the database. Identifier: {Identifier}", identifierHash);
        else if (!insertResult.Success)
        {
            logger.LogError("Failed to insert entity for parametric sweep. Entity: {@Entity}. Result: {@InsertResult}", entity, insertResult);
            throw new InvalidOperationException(insertResult.JoinedMessages);
        }

        return input.ExternalIdentifier;
    }

    private async Task<long> InsertStoredFile(StorageEntryMetadata fileMetadata, string externalIdentifier, CancellationToken cancellationToken)
    {
        StorageEntryMetadataEntity storedFileEntity = StorageEntryMetadataEntity.Create(fileMetadata, externalIdentifier);
        Result<long> fileResult = await repository.TryInsertAsync(storedFileEntity, cancellationToken).ConfigureAwait(false);
        if (fileResult.IsConflict)
            logger.LogWarning("Entity for stored file already exists in the database. Storage Entry Metadata: {@StorageEntryMetadata}", fileMetadata);
        else if (!fileResult.Success)
        {
            logger.LogError("Failed to insert stored file entity for parametric sweep. Entity: {@Entity}. Result: {@InsertResult}", storedFileEntity, fileResult);
            throw new InvalidOperationException(fileResult.JoinedMessages);
        }

        return fileResult.Data;
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }
}
