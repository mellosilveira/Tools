using MelloSilveiraTools.Core.Commands;
using MelloSilveiraTools.Core.Files.Providers;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Core.Security;
using MelloSilveiraTools.Database.RelationalDatabase.Models.Entities;
using MelloSilveiraTools.Database.Repositories;
using MelloSilveiraTools.Plugins.Infrastructure.Models;
using MelloSilveiraTools.Plugins.Infrastructure.Persistences;
using Microsoft.Extensions.Logging;

namespace MelloSilveiraTools.Plugins.Application.Commands.Submissions.Cancel;

/// <summary>
/// Cancels and deletes a <see cref="PluginSubmissionStatus.Pending"/> plugin submission along with its linked
/// <see cref="StorageEntryMetadataEntity"/> (<c>storage_entry_metadata</c>) record and binary in <c>pending/</c>.
/// Allowed exclusively for the submission's original author (while still pending) or a superuser.
/// Approved plugins can never be deleted by regular authors.
/// </summary>
/// <param name="logger">Logger instance for audit tracing.</param>
/// <param name="fileStorageService">Storage service deleting the pending binary.</param>
/// <param name="currentUserProvider">Mandatory current user provider enforcing author or superuser authorization.</param>
/// <param name="repository">Repository deleting the <see cref="PluginSubmissionEntity"/> and <see cref="StorageEntryMetadataEntity"/> records.</param>
public class CancelPluginSubmission(
    ILogger<CancelPluginSubmission> logger,
    IFileStorageService fileStorageService,
    ICurrentUserProvider currentUserProvider,
    IRepository repository)
    : CommandBaseWithDefaultResponse<CancelPluginSubmissionRequest>()
{
    /// <inheritdoc/>
    protected override async Task<Result> ExecuteCommandAsync(CancelPluginSubmissionRequest request, CancellationToken cancellationToken = default)
    {
        PluginSubmissionEntity? submission = await repository.GetAsync<PluginSubmissionEntity>(request.Id, cancellationToken).ConfigureAwait(false);
        if (submission is null)
            return Result.CreateNotFound($"Plugin submission '{request.Id}' was not found.");

        if (submission.Status != PluginSubmissionStatus.Pending)
            return Result.CreateConflict($"Plugin submission '{request.Id}' is in status '{submission.Status}' and can only be canceled while Pending.");

        bool isSuperuser = currentUserProvider.IsSuperuser();
        long? currentUserId = currentUserProvider.GetCurrentUserId();

        if (!isSuperuser && submission.UserId != currentUserId)
            return Result.CreateForbidden("You can only cancel your own pending plugin submissions.");

        StorageEntryMetadataEntity? fileMetadata = await repository.GetAsync<StorageEntryMetadataEntity>(submission.StorageEntryMetadataId, cancellationToken).ConfigureAwait(false);

        // Delete child submission first to satisfy ON DELETE RESTRICT on storage_entry_metadata(id).
        await repository.DeleteAsync<PluginSubmissionEntity>(submission.Id, cancellationToken).ConfigureAwait(false);

        if (fileMetadata is not null)
        {
            if (await fileStorageService.ExistsAsync(fileMetadata.Location, cancellationToken).ConfigureAwait(false))
                await fileStorageService.DeleteAsync(fileMetadata.Location, cancellationToken).ConfigureAwait(false);

            await repository.DeleteAsync<StorageEntryMetadataEntity>(fileMetadata.Id, cancellationToken).ConfigureAwait(false);
        }

        logger.LogInformation(
            "Canceled pending plugin submission {SubmissionId} ('{PluginName}' {PluginVersion}, FileId: {FileId}) by user {UserId}.",
            submission.Id,
            submission.PluginName,
            submission.PluginVersion,
            submission.StorageEntryMetadataId,
            currentUserId);

        return Result.CreateSuccessOk();
    }
}
