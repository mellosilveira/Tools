using MelloSilveiraTools.Core.Commands;
using MelloSilveiraTools.Core.Files.Providers;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Core.Security;
using MelloSilveiraTools.Database.RelationalDatabase.Models.Entities;
using MelloSilveiraTools.Database.Repositories;
using MelloSilveiraTools.Plugins.Application.Commands.Submissions.Review;
using MelloSilveiraTools.Plugins.Application.Commands.Upload;
using MelloSilveiraTools.Plugins.Infrastructure;
using MelloSilveiraTools.Plugins.Infrastructure.Models;
using MelloSilveiraTools.Plugins.Infrastructure.Persistences;
using MelloSilveiraTools.Plugins.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;

namespace MelloSilveiraTools.Plugins.Application.Commands.Submissions;

/// <summary>
/// Superuser moderation command that approves a <see cref="PluginSubmissionStatus.Pending"/> community plugin submission:
/// resolves its linked <see cref="StorageEntryMetadataEntity"/> (<c>storage_entry_metadata</c>), verifies author ownership,
/// re-validates the stored binary's SHA-256 digest (anti-TOCTOU), promotes the assembly from <c>pending/</c> to
/// <c>incoming/</c>, promotes <c>storage_entry_metadata.user_id</c> to <see langword="null"/> (public read/download, superuser-only
/// edit/delete), marks the submission <see cref="PluginSubmissionStatus.Approved"/>, and loads the plugin immediately.
/// </summary>
/// <param name="logger">Logger instance for audit tracing.</param>
/// <param name="fileStorageService">Storage service verifying binary existence.</param>
/// <param name="currentUserProvider">Current user provider used to record the reviewing superuser ID (<c>SuperuserOnly</c> policy is enforced at the HTTP authorization boundary and PostgreSQL RLS).</param>
/// <param name="repository">Repository persisting <see cref="StorageEntryMetadataEntity"/> and <see cref="PluginSubmissionEntity"/> state.</param>
/// <param name="pluginService">Plugin runtime service used to load the approved plugin immediately.</param>
/// <param name="timeProvider">Time provider used to stamp approval timestamps.</param>
/// <param name="fileProcessor">File processor handling SHA-256 verification and partition promotion.</param>
/// <param name="submissionPersistence">Service coordinating plugin submission queries and persistence.</param>
public class ApprovePluginSubmission(
    ILogger<ApprovePluginSubmission> logger,
    IFileStorageService fileStorageService,
    ICurrentUserProvider currentUserProvider,
    IRepository repository,
    IPluginService pluginService,
    TimeProvider timeProvider,
    PluginFileProcessor fileProcessor,
    PluginSubmissionPersistence submissionPersistence)
    : CommandBaseWithData<ReviewPluginSubmissionRequest, PluginInfo>()
{
    /// <inheritdoc/>
    protected override async Task<Result<PluginInfo>> ExecuteCommandAsync(ReviewPluginSubmissionRequest request, CancellationToken cancellationToken = default)
    {
        PluginSubmissionEntity? submission = await repository.GetAsync<PluginSubmissionEntity>(request.Id, cancellationToken).ConfigureAwait(false);
        if (submission is null)
            return Result.CreateNotFound($"Plugin submission '{request.Id}' was not found.");

        if (submission.Status != PluginSubmissionStatus.Pending)
            return Result.CreateConflict(PluginInfo.FromSubmissionEntity(submission), $"Plugin submission '{request.Id}' is in status '{submission.Status}' and cannot be approved.");

        StorageEntryMetadataEntity? fileMetadata = await repository.GetAsync<StorageEntryMetadataEntity>(submission.StorageEntryMetadataId, cancellationToken).ConfigureAwait(false);
        if (fileMetadata is null)
            return Result.CreateNotFound($"Linked stored file '{submission.StorageEntryMetadataId}' for plugin submission '{request.Id}' was not found.");

        // Enforce author ownership even when two users submitted the same plugin name concurrently while pending.
        List<PluginSubmissionEntity> existing = await submissionPersistence.FindByPluginNameAsync(submission.PluginName, cancellationToken).ConfigureAwait(false);
        if (existing.Any(e => e.Status == PluginSubmissionStatus.Approved && e.UserId != submission.UserId))
            return Result.CreateConflict(PluginInfo.FromSubmissionEntity(submission), $"Cannot approve: plugin '{submission.PluginName}' is already owned by another author.");

        string fileName = fileMetadata.FileName ?? $"{submission.PluginName}.{submission.PluginVersion}.dll";
        if (!await fileStorageService.ExistsAsync(fileMetadata.Location, cancellationToken).ConfigureAwait(false))
            return Result.CreateNotFound($"Pending plugin binary '{fileName}' is missing from storage.");

        string actualSha256 = await fileStorageService.ComputeSha256Async(fileMetadata.Location, cancellationToken).ConfigureAwait(false);
        if (!HasMatchingDigest(actualSha256, submission.Sha256Hash))
        {
            logger.LogWarning(
                "Refusing to approve plugin submission {SubmissionId} ('{FileName}'): SHA-256 mismatch (expected {Expected}, actual {Actual}).",
                submission.Id,
                fileName,
                submission.Sha256Hash,
                actualSha256);

            return Result.CreateConflict(PluginInfo.FromSubmissionEntity(submission), "Integrity verification failed: stored binary SHA-256 digest does not match the submitted hash.");
        }

        string incomingLocation = await fileProcessor.MovePendingToIncomingAsync(fileMetadata.Location, fileName, cancellationToken).ConfigureAwait(false);

        // Promote storage_entry_metadata row to public (UserId = null): readable/downloadable by all users,
        // editable/deletable exclusively by superusers under PostgreSQL RLS (blocks post-approval author tampering).
        StorageEntryMetadataEntity promotedFile = fileMetadata with
        {
            UserId = null,
            Location = incomingLocation,
            Directory = fileStorageService.ExtractDirectory(incomingLocation, PluginStoragePartitions.Incoming)
        };

        bool fileUpdated = await repository.TryUpdateAsync(promotedFile, cancellationToken).ConfigureAwait(false);
        if (!fileUpdated)
            return Result.CreateConflict(PluginInfo.FromSubmissionEntity(submission), $"Failed to update linked stored file '{fileMetadata.Id}' during approval.");

        PluginSubmissionEntity approved = submission with
        {
            Status = PluginSubmissionStatus.Approved,
            ReviewedByUserId = currentUserProvider.GetCurrentUserId(),
            ReviewedAt = timeProvider.GetUtcNow(),
            ReviewNotes = request.ReviewNotes
        };

        bool updated = await repository.TryUpdateAsync(approved, cancellationToken).ConfigureAwait(false);
        if (!updated)
            return Result.CreateConflict(PluginInfo.FromSubmissionEntity(submission), $"Failed to update plugin submission '{request.Id}' status to Approved.");

        await pluginService.LoadPluginsOnRuntimeAsync(approved.PluginName, PluginVersion.Parse(approved.PluginVersion), cancellationToken).ConfigureAwait(false);

        logger.LogInformation(
            "Approved and loaded plugin submission {SubmissionId} ('{PluginName}' {PluginVersion}, FileId: {FileId}, SHA-256: {Sha256}) by superuser {ReviewerId}.",
            approved.Id,
            approved.PluginName,
            approved.PluginVersion,
            approved.StorageEntryMetadataId,
            approved.Sha256Hash,
            approved.ReviewedByUserId);

        return PluginInfo.FromSubmissionEntity(approved);
    }

    private static bool HasMatchingDigest(string actualSha256, string expectedSha256) => CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(actualSha256), Encoding.UTF8.GetBytes(expectedSha256));
}
