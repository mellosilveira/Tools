using MelloSilveiraTools.Core.Commands;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Core.Security;
using MelloSilveiraTools.Database.Repositories;
using MelloSilveiraTools.Plugins.Application.Commands.Upload;
using MelloSilveiraTools.Plugins.Application.Validators;
using MelloSilveiraTools.Plugins.Infrastructure;
using MelloSilveiraTools.Plugins.Infrastructure.Models;
using MelloSilveiraTools.Plugins.Infrastructure.Persistences;
using Microsoft.Extensions.Logging;

namespace MelloSilveiraTools.Plugins.Application.Commands.Submissions;

/// <summary>
/// Community plugin submission command for authenticated users: enforces per-plugin author ownership
/// (preventing version hijacking) and pending quota limits, writes the validated assembly into the
/// isolated <c>pending/</c> partition, persists a private <c>storage_entry_metadata</c> record
/// (<c>UserId = author</c>), and creates a <see cref="PluginSubmissionStatus.Pending"/> submission awaiting
/// superuser moderation. Authentication is enforced at the HTTP authorization boundary (<c>[Authorize]</c>).
/// </summary>
/// <param name="logger">Logger instance for tracing and diagnostics.</param>
/// <param name="currentUserProvider">Current user provider identifying the submitting author.</param>
/// <param name="repository">Repository persisting metadata and submission records.</param>
/// <param name="timeProvider">Time provider used to stamp submission creation timestamps.</param>
/// <param name="fileProcessor">Plugin file processor parsing assembly filenames.</param>
/// <param name="submissionPersistence">Service handling plugin identity validation, binary streaming, and submission persistence.</param>
/// <param name="validator">Validator ensuring size limits, naming conventions, and managed IL-only PE compatibility.</param>
/// <param name="settings">Plugin settings defining per-user pending submission quotas.</param>
public class SubmitPlugin(
    ILogger<SubmitPlugin> logger,
    ICurrentUserProvider currentUserProvider,
    IRepository repository,
    TimeProvider timeProvider,
    PluginFileProcessor fileProcessor,
    PluginSubmissionPersistence submissionPersistence,
    UploadPluginValidator validator,
    PluginSettings settings)
    : CommandBaseWithData<UploadPluginRequest, PluginInfo>(validator)
{
    /// <inheritdoc/>
    protected override async Task<Result<PluginInfo>> ExecuteCommandAsync(UploadPluginRequest request, CancellationToken cancellationToken = default)
    {
        string safeFileName = Path.GetFileName(request.File.FileName);
        if (!fileProcessor.TryParsePluginFileName(safeFileName, out string pluginName, out PluginVersion pluginVersion))
            return Result.CreateBadRequest("Invalid plugin file name.");

        long? currentUserId = currentUserProvider.GetCurrentUserId();
        List<PluginSubmissionEntity> existing = await submissionPersistence.FindByPluginNameAsync(pluginName, cancellationToken).ConfigureAwait(false);
        if (existing.Any(s => s.Status == PluginSubmissionStatus.Approved && s.UserId != currentUserId))
            return Result.CreateForbidden($"Plugin '{pluginName}' is owned by another author. Choose a distinct plugin name.");

        int maxPending = settings.MaxPendingSubmissionsPerUser;
        long pendingCount = await repository.CountAsync<PluginSubmissionEntity, PluginSubmissionFilter>(new PluginSubmissionFilter { UserId = currentUserId, Status = PluginSubmissionStatus.Pending }, cancellationToken).ConfigureAwait(false);
        if (pendingCount >= maxPending)
            return Result.CreateBadRequest($"Maximum concurrent pending plugin submissions ({maxPending}) reached. Wait for review or cancel an existing pending submission.");

        DateTimeOffset now = timeProvider.GetUtcNow();
        (PluginSubmissionEntity persisted, long sizeBytes) = await submissionPersistence.WriteAndPersistAsync(
            request.File,
            safeFileName,
            pluginName,
            pluginVersion,
            PluginStoragePartitions.Pending,
            storageOwnerUserId: currentUserId,
            (fileId, sha256Hash) => new PluginSubmissionEntity
            {
                UserId = currentUserId,
                StorageEntryMetadataId = fileId,
                PluginName = pluginName,
                PluginVersion = pluginVersion.Name,
                Sha256Hash = sha256Hash,
                Status = PluginSubmissionStatus.Pending
            },
            now,
            cancellationToken).ConfigureAwait(false);

        logger.LogInformation(
            "Submitted community plugin '{FileName}' (FileId: {FileId}, SHA-256: {Sha256}) to 'pending' by user {UserId}. Size: {SizeBytes} bytes.",
            safeFileName,
            persisted.StorageEntryMetadataId,
            persisted.Sha256Hash,
            currentUserId,
            sizeBytes);

        return Result.CreateSuccessOk(PluginInfo.FromSubmissionEntity(persisted));
    }
}
