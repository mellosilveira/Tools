using MelloSilveiraTools.Core.Commands;
using MelloSilveiraTools.Core.Files.Providers;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Core.Providers;
using MelloSilveiraTools.Database.RelationalDatabase.Models.Entities;
using MelloSilveiraTools.Database.Repositories;

namespace MelloSilveiraTools.WebApi.Application.Commands.StorageEntryMetadata.Download;

/// <summary>
/// Resolves and opens a readable stream for a stored file identified by its metadata entity ID.
/// </summary>
/// <param name="repository">Relational repository for metadata lookup.</param>
/// <param name="serviceLocator">Service locator used to resolve the keyed <see cref="IFileStorageService"/>.</param>
public sealed class DownloadStoredFileCommand(
    IRepository repository,
    ServiceLocator serviceLocator)
    : CommandBaseWithData<DownloadStoredFileRequest, DownloadStoredFileResultData>
{
    /// <inheritdoc/>
    protected override async Task<Result<DownloadStoredFileResultData>> ExecuteCommandAsync(
        DownloadStoredFileRequest request,
        CancellationToken cancellationToken = default)
    {
        StorageEntryMetadataEntity? entity = await repository.GetAsync<StorageEntryMetadataEntity>(request.FileId, cancellationToken).ConfigureAwait(false);
        if (entity is null)
            return Result.CreateNotFound($"Stored file with id '{request.FileId}' was not found.");

        IFileStorageService? fileStorageService = serviceLocator.GetKeyedService<IFileStorageService>(entity.StorageProvider);
        if (fileStorageService is null)
            return Result.CreateNotFound($"Storage provider '{entity.StorageProvider}' is not configured.");

        bool exists = await fileStorageService.ExistsAsync(entity.Location, cancellationToken).ConfigureAwait(false);
        if (!exists)
            return Result.CreateNotFound($"Physical file for '{entity.FileName}' was not found on storage.");

        Stream fileStream = await fileStorageService.OpenReadAsync(entity.Location, cancellationToken).ConfigureAwait(false);
        return Result.CreateSuccessOk(new DownloadStoredFileResultData
        {
            FileStream = fileStream,
            ContentType = entity.ContentType,
            FileName = entity.FileName!,
            SizeBytes = entity.SizeBytes,
        });
    }
}
