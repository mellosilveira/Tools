using MelloSilveiraTools.Core.Commands;
using MelloSilveiraTools.Core.Files.Providers;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Core.Providers;
using MelloSilveiraTools.Core.Validators;
using MelloSilveiraTools.Database.RelationalDatabase.Models.Entities;
using MelloSilveiraTools.Database.Repositories;
using Microsoft.Extensions.Logging;

namespace MelloSilveiraTools.WebApi.Application.Commands.StorageEntryMetadata.Delete;

/// <summary>
/// Idempotently deletes a file from both <see cref="IFileStorageService"/> and the database repository,
/// failing only if the target is missing in both locations or if deletion fails in either store.
/// </summary>
/// <param name="logger">Logger for deletion diagnostics.</param>
/// <param name="validator">Validator for delete requests.</param>
/// <param name="repository">Relational repository for metadata deletion.</param>
/// <param name="serviceLocator">Service locator used to resolve <see cref="IFileStorageService"/>.</param>
public sealed class DeleteStorageEntryCommand(
    ILogger<DeleteStorageEntryCommand> logger,
    IValidator<DeleteStorageEntryRequest> validator,
    IRepository repository,
    ServiceLocator serviceLocator)
    : CommandBaseWithDefaultResponse<DeleteStorageEntryRequest>(validator)
{
    /// <inheritdoc/>
    protected override async Task<Result> ExecuteCommandAsync(
        DeleteStorageEntryRequest request,
        CancellationToken cancellationToken = default)
    {
        IFileStorageService fileStorageService = request.StorageProvider.HasValue
            ? serviceLocator.GetRequiredKeyedService<IFileStorageService>(request.StorageProvider.Value)
            : serviceLocator.GetRequiredService<IFileStorageService>();

        StorageEntryMetadataEntity? entity = request.FileId > 0 ? await repository.GetAsync<StorageEntryMetadataEntity>(request.FileId.Value, cancellationToken).ConfigureAwait(false) : null;

        string? targetLocation = !string.IsNullOrWhiteSpace(entity?.Location) ? entity.Location : request.Location;

        bool existsInDb = entity is not null;
        bool existsInStorage = !string.IsNullOrWhiteSpace(targetLocation) && await fileStorageService.ExistsAsync(targetLocation, cancellationToken).ConfigureAwait(false);

        if (!existsInDb && !existsInStorage)
            return Result.CreateNotFound("The requested file was not found in storage or in the database.");

        if (existsInStorage)
        {
            bool deletedFromStorage = await fileStorageService.DeleteAsync(targetLocation!, cancellationToken).ConfigureAwait(false);
            if (!deletedFromStorage)
            {
                logger.LogError("Failed to delete physical file at '{Location}' from {Provider}.", targetLocation, fileStorageService.Provider);
                return Result.CreateError(StatusCode.UnknownError, $"Failed to delete file at '{targetLocation}' from storage.");
            }
        }

        if (existsInDb)
            await repository.DeleteAsync<StorageEntryMetadataEntity>(entity!.Id, cancellationToken).ConfigureAwait(false);

        return Result.CreateSuccessOk();
    }
}
