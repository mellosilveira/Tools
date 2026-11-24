using MelloSilveiraTools.Core.Commands;
using MelloSilveiraTools.Core.Files.Providers;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Core.Providers;
using MelloSilveiraTools.Core.Validators;
using MelloSilveiraTools.Database.RelationalDatabase.Models.Entities;
using MelloSilveiraTools.Database.Repositories;
using Microsoft.Extensions.Logging;

namespace MelloSilveiraTools.WebApi.Application.Commands.StorageEntryMetadata.Create;

/// <summary>
/// Stores a file in the resolved <see cref="IFileStorageService"/> and persists its <see cref="StorageEntryMetadataEntity"/> record.
/// </summary>
/// <param name="logger">Logger for storage and persistence diagnostics.</param>
/// <param name="validator">Validator for create requests.</param>
/// <param name="repository">Relational repository for metadata persistence.</param>
/// <param name="serviceLocator">Service locator used to resolve keyed or default <see cref="IFileStorageService"/>.</param>
public sealed class AddStorageEntryCommand(
    ILogger<AddStorageEntryCommand> logger,
    IValidator<AddStorageEntryRequest> validator,
    IRepository repository,
    ServiceLocator serviceLocator)
    : CommandBaseWithData<AddStorageEntryRequest, StorageEntryResultData>(validator)
{
    /// <inheritdoc/>
    protected override async Task<Result<StorageEntryResultData>> ExecuteCommandAsync(AddStorageEntryRequest request, CancellationToken cancellationToken = default)
    {
        IFileStorageService fileStorageService = request.StorageProvider.HasValue
            ? serviceLocator.GetRequiredKeyedService<IFileStorageService>(request.StorageProvider.Value)
            : serviceLocator.GetRequiredService<IFileStorageService>();

        string fileName = Path.GetFileName(request.File.FileName);

        (Stream targetStream, string location) = await fileStorageService
            .CreateWriteAsync(request.Identifier, request.Context, fileName, cancellationToken)
            .ConfigureAwait(false);

        try
        {
            await using (targetStream.ConfigureAwait(false))
            {
                await request.File.CopyToAsync(targetStream, cancellationToken).ConfigureAwait(false);
            }

            Core.Files.Models.StorageEntryMetadata metadata = await fileStorageService.GetMetadataAsync(location, cancellationToken).ConfigureAwait(false);

            string contentType = request.File.Headers is null || string.IsNullOrWhiteSpace(request.File.ContentType) ? "application/octet-stream" : request.File.ContentType;
            StorageEntryMetadataEntity entity = StorageEntryMetadataEntity.Create(metadata, request.ExternalIdentifier, contentType);

            Result<long> insertResult = await repository.TryInsertAsync(entity, cancellationToken).ConfigureAwait(false);
            if (!insertResult.Success)
            {
                await fileStorageService.DeleteAsync(location, cancellationToken).ConfigureAwait(false);
                return Result.CreateError(insertResult.StatusCode, $"Failed to persist metadata: {insertResult.JoinedMessages}");
            }

            return new StorageEntryResultData(insertResult.Data, metadata);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to write file '{FileName}' to storage at '{Location}'.", fileName, location);

            await fileStorageService.DeleteAsync(location, cancellationToken).ConfigureAwait(false);
            return Result.CreateUnknownError($"Failed to write file to storage: {ex.Message}");
        }
    }
}
