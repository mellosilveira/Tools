using MelloSilveiraTools.Core.Commands;
using MelloSilveiraTools.Core.Files.Providers;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Core.Providers;
using MelloSilveiraTools.Core.Security;
using MelloSilveiraTools.Core.Validators;
using MelloSilveiraTools.Database.RelationalDatabase.Models.Entities;
using MelloSilveiraTools.Database.Repositories;
using Microsoft.Extensions.Logging;

namespace MelloSilveiraTools.WebApi.Application.Commands.StorageEntryMetadata.Upload;

/// <summary>
/// Overwrites only the physical file in <see cref="IFileStorageService"/>, deletes the old metadata record,
/// and inserts a new metadata record in the repository.
/// Enforces tenant ownership so non-superusers cannot overwrite public files (<c>UserId == null</c>) or files owned by other users.
/// </summary>
/// <param name="logger">Logger for repository and storage diagnostics.</param>
/// <param name="validator">Validator for update requests.</param>
/// <param name="repository">Relational repository for metadata persistence.</param>
/// <param name="serviceLocator">Service locator used to resolve the target <see cref="IFileStorageService"/>.</param>
/// <param name="currentUserProvider">Optional user context provider used to enforce file ownership before overwriting.</param>
public sealed class UpdateStorageEntryFileCommand(
    ILogger<UpdateStorageEntryFileCommand> logger,
    IValidator<UpdateStorageEntryFileRequest> validator,
    IRepository repository,
    ServiceLocator serviceLocator,
    ICurrentUserProvider? currentUserProvider = null)
    : CommandBaseWithData<UpdateStorageEntryFileRequest, StorageEntryResultData>(validator)
{
    /// <inheritdoc/>
    protected override async Task<Result<StorageEntryResultData>> ExecuteCommandAsync(
        UpdateStorageEntryFileRequest request,
        CancellationToken cancellationToken = default)
    {
        StorageEntryMetadataEntity? existing = await repository.GetAsync<StorageEntryMetadataEntity>(request.FileId, cancellationToken).ConfigureAwait(false);
        if (existing is null)
            return Result.CreateNotFound($"Stored file with id '{request.FileId}' was not found.");

        bool canModify = currentUserProvider is null
            || currentUserProvider.IsSuperuser()
            || (existing.UserId is long ownerId && currentUserProvider.GetCurrentUserId() == ownerId);

        if (!canModify)
            return Result.CreateForbidden("You do not have permission to overwrite a public file or a file owned by another user.");

        IFileStorageService fileStorageService = serviceLocator.GetRequiredKeyedService<IFileStorageService>(existing.StorageProvider);

        await using (Stream targetStream = await fileStorageService.OpenWriteAsync(existing.Location, cancellationToken).ConfigureAwait(false))
        {
            await request.File.CopyToAsync(targetStream, cancellationToken).ConfigureAwait(false);
        }

        Core.Files.Models.StorageEntryMetadata updatedMetadata = await fileStorageService.GetMetadataAsync(existing.Location, cancellationToken).ConfigureAwait(false);

        string contentType = request.File.Headers is null || string.IsNullOrWhiteSpace(request.File.ContentType) ? "application/octet-stream" : request.File.ContentType;
        StorageEntryMetadataEntity replacementEntity = StorageEntryMetadataEntity.Create(updatedMetadata, existing.ExternalIdentifier, contentType) with
        {
            UserId = existing.UserId
        };

        try
        {
            await repository.DeleteAsync<StorageEntryMetadataEntity>(existing.Id, cancellationToken).ConfigureAwait(false);

            Result<long> insertResult = await repository.TryInsertAsync(replacementEntity, cancellationToken).ConfigureAwait(false);
            if (!insertResult.Success)
            {
                logger.LogError("Failed to insert replacement metadata for storage entry. Existing id: '{FileId}'. {@ReplacementEntity} {@InsertResult}", existing.Id, replacementEntity, insertResult);
                return Result.CreateUnknownError("Failed to insert replacement metadata for storage entry.");
            }

            return new StorageEntryResultData(insertResult.Data, updatedMetadata);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Repository exception while overwriting metadata for storage entry '{FileId}'.{@ReplacementEntity}", existing.Id, replacementEntity);
            return Result.CreateUnknownError("It was not possible to override metadata.");
        }
    }
}
