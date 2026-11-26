using MelloSilveiraTools.Core.Commands;
using MelloSilveiraTools.Core.Files.Providers;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Core.Providers;
using MelloSilveiraTools.Database.RelationalDatabase.Models.Entities;
using MelloSilveiraTools.Database.RelationalDatabase.Models.Filters;
using MelloSilveiraTools.Database.Repositories;
using MelloSilveiraTools.WebApi.Application.Commands.StorageEntryMetadata;

namespace MelloSilveiraTools.WebApi.Application.Commands.StorageEntryMetadata.Read;

/// <summary>
/// Reads file metadata from both the database repository and <see cref="IFileStorageService"/>.
/// </summary>
/// <param name="repository">Relational repository for persisted metadata.</param>
/// <param name="serviceLocator">Service locator used to resolve <see cref="IFileStorageService"/>.</param>
public sealed class ReadStorageEntriesCommand(
    IRepository repository,
    ServiceLocator serviceLocator)
    : ListedCommandBase<ReadStorageEntriesRequest, StorageEntryResultData>
{
    /// <inheritdoc/>
    protected override async Task<ListedResult<StorageEntryResultData>> ExecuteCommandAsync(ReadStorageEntriesRequest request, CancellationToken cancellationToken = default)
    {
        IFileStorageService fileStorageService = request.StorageProvider.HasValue
            ? serviceLocator.GetRequiredKeyedService<IFileStorageService>(request.StorageProvider.Value)
            : serviceLocator.GetRequiredService<IFileStorageService>();

        StorageEntryMetadataFilter filter = new()
        {
            ExternalIdentifier = request.ExternalIdentifier,
            Identifier = request.Identifier,
            Context = request.Context,
            StorageProvider = request.StorageProvider,
        };

        List<StorageEntryResultData> resultData = [];
        await foreach (StorageEntryMetadataEntity entity in repository.GetAsync<StorageEntryMetadataEntity, StorageEntryMetadataFilter>(filter, cancellationToken: cancellationToken))
        {
            try
            {
                Core.Files.Models.StorageEntryMetadata metadata = await fileStorageService.GetMetadataAsync(entity.Location, cancellationToken).ConfigureAwait(false);
                resultData.Add(new StorageEntryResultData(entity.Id, metadata));
            }
            catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException)
            {
                // Skip database entries whose physical file does not exist on the current host/container storage.
            }
        }

        return resultData;
    }
}
