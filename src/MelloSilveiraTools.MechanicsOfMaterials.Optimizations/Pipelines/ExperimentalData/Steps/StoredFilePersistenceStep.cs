using MelloSilveiraTools.Core.Managers.File.Models;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Core.Pipelines.Steps;
using MelloSilveiraTools.Database.RelationalDatabase.Models.Entities;
using MelloSilveiraTools.Database.Repositories;
using Microsoft.Extensions.Logging;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Steps;

/// <summary>
/// Pipeline step responsible for persisting a <see cref="StoredFileEntity"/> metadata record into the database.
/// </summary>
/// <param name="logger">The structured logger instance.</param>
/// <param name="repository">The database repository provider for persistence.</param>
/// <param name="externalIdentifier">Unique business or run identifier correlating the stored file.</param>
/// <param name="context">Domain context categorizing the stored file artifact.</param>
public sealed class StoredFilePersistenceStep(
    ILogger<StoredFilePersistenceStep> logger,
    IRepository repository,
    string externalIdentifier,
    string context)
    : IAsyncPipelineStep<StorageEntryMetadata, long>
{
    /// <inheritdoc />
    public string Name => nameof(StoredFilePersistenceStep);

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">Thrown when insertion of the stored file entity fails in the database.</exception>
    public async Task<long> ExecuteAsync(StorageEntryMetadata input, CancellationToken cancellationToken = default)
    {
        StoredFileEntity storedFileEntity = StoredFileEntity.Create(externalIdentifier, context, input);
        Result<long> fileResult = await repository.TryInsertAsync(storedFileEntity, cancellationToken).ConfigureAwait(false);

        if (fileResult.IsConflict)
            logger.LogWarning("Entity for stored file already exists in the database. Storage Entry Metadata: {@StorageEntryMetadata}", input);
        else if (!fileResult.Success)
        {
            logger.LogError("Failed to insert entity for stored file. Entity: {@Entity}. Result: {@InsertResult}", storedFileEntity, fileResult);
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
