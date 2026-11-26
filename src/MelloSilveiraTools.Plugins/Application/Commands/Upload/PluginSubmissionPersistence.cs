using MelloSilveiraTools.Core.Files.Providers;
using MelloSilveiraTools.Database.RelationalDatabase.Models.Entities;
using MelloSilveiraTools.Database.Repositories;
using MelloSilveiraTools.Plugins.Infrastructure;
using MelloSilveiraTools.Plugins.Infrastructure.Models;
using MelloSilveiraTools.Plugins.Infrastructure.Persistences;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace MelloSilveiraTools.Plugins.Application.Commands.Upload;

/// <summary>
/// Thread-safe DI service coordinating plugin submission lookup, binary streaming with SHA-256 hashing,
/// metadata/submission persistence, and best-effort rollback across storage and relational persistence.
/// </summary>
/// <param name="logger">Logger instance for persistence and rollback diagnostics.</param>
/// <param name="fileStorageService">Storage service handling binary streaming, hashing, and rollback cleanup.</param>
/// <param name="repository">Repository persisting <see cref="StorageEntryMetadataEntity"/> and <see cref="PluginSubmissionEntity"/> records.</param>
/// <param name="settings">Plugin settings providing the root storage directory.</param>
public sealed class PluginSubmissionPersistence(
    ILogger<PluginSubmissionPersistence> logger,
    IFileStorageService fileStorageService,
    IRepository repository,
    PluginSettings settings)
{
    private const string AssemblyContentType = "application/octet-stream";

    /// <summary>
    /// Retrieves all persisted plugin submissions matching <paramref name="pluginName"/> (case-insensitive).
    /// </summary>
    /// <param name="pluginName">Logical plugin name without version suffix.</param>
    /// <param name="cancellationToken">Token used to cancel the asynchronous query.</param>
    /// <returns>A list of matching <see cref="PluginSubmissionEntity"/> records.</returns>
    public async Task<List<PluginSubmissionEntity>> FindByPluginNameAsync(string pluginName, CancellationToken cancellationToken = default)
    {
        List<PluginSubmissionEntity> matches = [];

        IAsyncEnumerable<PluginSubmissionEntity> existingStream = repository.GetAsync<PluginSubmissionEntity, PluginSubmissionFilter>(new PluginSubmissionFilter { PluginName = pluginName }, cancellationToken: cancellationToken);
        await foreach (PluginSubmissionEntity existing in existingStream.ConfigureAwait(false))
        {
            if (existing.PluginName.Equals(pluginName, StringComparison.OrdinalIgnoreCase))
                matches.Add(existing);
        }

        return matches;
    }

    /// <summary>
    /// Streams <paramref name="file"/> to the target storage <paramref name="partition"/>, computes its SHA-256 hash,
    /// persists the linked <see cref="StorageEntryMetadataEntity"/> and <see cref="PluginSubmissionEntity"/> records,
    /// and rolls back storage/database artifacts on partial failure.
    /// </summary>
    /// <param name="file">Uploaded plugin assembly file.</param>
    /// <param name="safeFileName">Sanitized filename matching the required versioned DLL pattern.</param>
    /// <param name="pluginName">Parsed logical plugin name.</param>
    /// <param name="pluginVersion">Parsed semantic plugin version.</param>
    /// <param name="partition">Target storage partition (e.g., <c>pending</c> or <c>incoming</c>).</param>
    /// <param name="storageOwnerUserId">Owner user ID for the persisted storage metadata, or <see langword="null"/> for public binaries.</param>
    /// <param name="submissionFactory">Factory producing the <see cref="PluginSubmissionEntity"/> from the persisted file ID and SHA-256 hash.</param>
    /// <param name="creationTimestamp">UTC timestamp stamped on the returned submission entity.</param>
    /// <param name="cancellationToken">Token used to cancel storage or database I/O.</param>
    /// <returns>The persisted <see cref="PluginSubmissionEntity"/> with generated ID and the total written byte count.</returns>
    public async Task<(PluginSubmissionEntity Submission, long SizeBytes)> WriteAndPersistAsync(
        IFormFile file,
        string safeFileName,
        string pluginName,
        PluginVersion pluginVersion,
        string partition,
        long? storageOwnerUserId,
        Func<long, string, PluginSubmissionEntity> submissionFactory,
        DateTimeOffset creationTimestamp,
        CancellationToken cancellationToken = default)
    {
        await using Stream sourceStream = file.OpenReadStream();
        (string location, string directory, string sha256Hash, long sizeBytes) = await fileStorageService
            .WriteAndHashAsync(settings.Directory, partition, safeFileName, sourceStream, cancellationToken)
            .ConfigureAwait(false);

        StorageEntryMetadataEntity fileEntity = new()
        {
            UserId = storageOwnerUserId,
            ExternalIdentifier = $"{pluginName}@{pluginVersion.Name}",
            StorageProvider = fileStorageService.Provider,
            Location = location,
            Directory = directory,
            FileName = safeFileName,
            SizeBytes = sizeBytes,
            ContentType = AssemblyContentType
        };

        long? insertedFileId = null;
        try
        {
            long fileId = await repository.InsertAsync(fileEntity, cancellationToken).ConfigureAwait(false);
            insertedFileId = fileId;

            long submissionId = await repository.InsertAsync(submissionFactory(fileId, sha256Hash), cancellationToken).ConfigureAwait(false);
            PluginSubmissionEntity entity = (await repository.GetAsync<PluginSubmissionEntity>(submissionId, cancellationToken).ConfigureAwait(false))!;
            return (entity, sizeBytes);
        }
        catch
        {
            await RollbackBestEffortAsync(insertedFileId, location, cancellationToken).ConfigureAwait(false);
            throw;
        }
    }

    private async Task RollbackBestEffortAsync(long? insertedFileId, string location, CancellationToken cancellationToken)
    {
        if (insertedFileId.HasValue)
        {
            try
            {
                await repository.DeleteAsync<StorageEntryMetadataEntity>(insertedFileId.Value, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to delete storage metadata record {FileId} during rollback.", insertedFileId.Value);
            }
        }

        try
        {
            await fileStorageService.DeleteAsync(location, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to delete storage file {Location} during rollback.", location);
        }
    }
}
