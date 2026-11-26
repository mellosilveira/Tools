using MelloSilveiraTools.Core.Commands;
using MelloSilveiraTools.Core.Files.Providers;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Core.Security;
using MelloSilveiraTools.Database.RelationalDatabase.Models.Entities;
using MelloSilveiraTools.Database.Repositories;
using MelloSilveiraTools.Plugins.Application.Commands.Submissions.Review;
using MelloSilveiraTools.Plugins.Infrastructure;
using MelloSilveiraTools.Plugins.Infrastructure.Models;
using MelloSilveiraTools.Plugins.Infrastructure.Persistences;
using Microsoft.Extensions.Logging;

namespace MelloSilveiraTools.Plugins.Application.Commands.Submissions;

/// <summary>
/// Superuser moderation command that rejects a <see cref="PluginSubmissionStatus.Pending"/> community plugin submission,
/// relocating its binary from <c>pending/</c> to <c>rejected/</c>, updating the linked <see cref="StorageEntryMetadataEntity"/>,
/// and recording audit metadata.
/// </summary>
/// <param name="logger">Logger instance for audit tracing.</param>
/// <param name="fileStorageService">Storage service verifying binary existence.</param>
/// <param name="currentUserProvider">Current user provider used to record the reviewing superuser ID (<c>SuperuserOnly</c> policy is enforced at the HTTP authorization boundary and PostgreSQL RLS).</param>
/// <param name="repository">Repository persisting <see cref="StorageEntryMetadataEntity"/> and <see cref="PluginSubmissionEntity"/> state.</param>
/// <param name="timeProvider">Time provider used to stamp review timestamps.</param>
/// <param name="fileProcessor">File processor handling movement into <c>rejected/</c>.</param>
public class RejectPluginSubmission(
    ILogger<RejectPluginSubmission> logger,
    IFileStorageService fileStorageService,
    ICurrentUserProvider currentUserProvider,
    IRepository repository,
    TimeProvider timeProvider,
    PluginFileProcessor fileProcessor)
    : CommandBaseWithData<ReviewPluginSubmissionRequest, PluginInfo>()
{
    /// <inheritdoc/>
    protected override async Task<Result<PluginInfo>> ExecuteCommandAsync(ReviewPluginSubmissionRequest request, CancellationToken cancellationToken = default)
    {
        PluginSubmissionEntity? submission = await repository.GetAsync<PluginSubmissionEntity>(request.Id, cancellationToken).ConfigureAwait(false);
        if (submission is null)
            return Result.CreateNotFound($"Plugin submission '{request.Id}' was not found.");

        if (submission.Status != PluginSubmissionStatus.Pending)
            return Result.CreateConflict($"Plugin submission '{request.Id}' is in status '{submission.Status}' and cannot be rejected.");

        StorageEntryMetadataEntity? fileMetadata = await repository.GetAsync<StorageEntryMetadataEntity>(submission.StorageEntryMetadataId, cancellationToken).ConfigureAwait(false);
        if (fileMetadata is not null)
        {
            string fileName = fileMetadata.FileName ?? $"{submission.PluginName}.{submission.PluginVersion}.dll";
            string rejectedLocation = fileMetadata.Location;
            if (await fileStorageService.ExistsAsync(fileMetadata.Location, cancellationToken).ConfigureAwait(false))
                rejectedLocation = await fileProcessor.MovePendingToRejectedAsync(fileMetadata.Location, fileName, cancellationToken).ConfigureAwait(false);

            StorageEntryMetadataEntity archivedFile = fileMetadata with
            {
                UserId = currentUserProvider.GetCurrentUserId(),
                Location = rejectedLocation,
                Directory = fileStorageService.ExtractDirectory(rejectedLocation, PluginStoragePartitions.Rejected)
            };

            await repository.TryUpdateAsync(archivedFile, cancellationToken).ConfigureAwait(false);
        }

        PluginSubmissionEntity rejected = submission with
        {
            Status = PluginSubmissionStatus.Rejected,
            ReviewedByUserId = currentUserProvider.GetCurrentUserId(),
            ReviewedAt = timeProvider.GetUtcNow(),
            ReviewNotes = request.ReviewNotes
        };

        bool updated = await repository.TryUpdateAsync(rejected, cancellationToken).ConfigureAwait(false);
        if (!updated)
            return Result.CreateConflict($"Failed to update plugin submission '{request.Id}' status to Rejected.");

        logger.LogInformation(
            "Rejected plugin submission {SubmissionId} ('{PluginName}' {PluginVersion}, FileId: {FileId}) by superuser {ReviewerId}.",
            rejected.Id,
            rejected.PluginName,
            rejected.PluginVersion,
            rejected.StorageEntryMetadataId,
            rejected.ReviewedByUserId);

        return PluginInfo.FromSubmissionEntity(rejected);
    }
}
