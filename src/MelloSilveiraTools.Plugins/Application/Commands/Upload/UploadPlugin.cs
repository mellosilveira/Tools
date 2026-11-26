using MelloSilveiraTools.Core.Commands;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Core.Security;
using MelloSilveiraTools.Plugins.Application.Validators;
using MelloSilveiraTools.Plugins.Infrastructure;
using MelloSilveiraTools.Plugins.Infrastructure.Models;
using MelloSilveiraTools.Plugins.Infrastructure.Persistences;
using Microsoft.Extensions.Logging;

namespace MelloSilveiraTools.Plugins.Application.Commands.Upload;

/// <summary>
/// Superuser direct plugin publishing command: writes a validated assembly straight into <c>incoming/</c>,
/// persists a public read-only <c>storage_entry_metadata</c> record (<c>UserId = null</c> so everyone
/// can read/download while only superusers can edit/delete under RLS), and creates an auto-approved
/// <see cref="PluginSubmissionEntity"/> linked by FK.
/// Access is restricted to superusers at the HTTP authorization boundary (<c>SuperuserOnly</c>).
/// </summary>
/// <param name="logger">Logger instance for tracing and diagnostics.</param>
/// <param name="currentUserProvider">Current user provider used to stamp author and reviewer identity.</param>
/// <param name="timeProvider">Time provider used to stamp approval timestamps.</param>
/// <param name="fileProcessor">Plugin file processor parsing assembly filenames.</param>
/// <param name="submissionPersistence">Service handling plugin identity validation, binary streaming, and submission persistence.</param>
/// <param name="validator">Validator ensuring size limits, naming conventions, and managed IL-only PE compatibility.</param>
public class UploadPlugin(
    ILogger<UploadPlugin> logger,
    ICurrentUserProvider currentUserProvider,
    TimeProvider timeProvider,
    PluginFileProcessor fileProcessor,
    PluginSubmissionPersistence submissionPersistence,
    UploadPluginValidator validator)
    : CommandBaseWithData<UploadPluginRequest, PluginInfo>(validator)
{
    /// <inheritdoc/>
    protected override async Task<Result<PluginInfo>> ExecuteCommandAsync(UploadPluginRequest request, CancellationToken cancellationToken = default)
    {
        string safeFileName = Path.GetFileName(request.File.FileName);
        if (!fileProcessor.TryParsePluginFileName(safeFileName, out string pluginName, out PluginVersion pluginVersion))
            return Result.CreateBadRequest("Invalid plugin file name.");

        long? currentUserId = currentUserProvider.GetCurrentUserId();
        DateTimeOffset now = timeProvider.GetUtcNow();

        (PluginSubmissionEntity persisted, long sizeBytes) = await submissionPersistence.WriteAndPersistAsync(
            request.File,
            safeFileName,
            pluginName,
            pluginVersion,
            PluginStoragePartitions.Incoming,
            storageOwnerUserId: null,
            (fileId, sha256Hash) => new PluginSubmissionEntity
            {
                UserId = currentUserId,
                StorageEntryMetadataId = fileId,
                PluginName = pluginName,
                PluginVersion = pluginVersion.Name,
                Sha256Hash = sha256Hash,
                Status = PluginSubmissionStatus.Approved,
                ReviewedByUserId = currentUserId,
                ReviewedAt = now,
                ReviewNotes = "Auto-approved on superuser upload."
            },
            now,
            cancellationToken).ConfigureAwait(false);

        logger.LogInformation(
            "Published plugin '{FileName}' (FileId: {FileId}, SHA-256: {Sha256}) directly to 'incoming' by superuser {UserId}. Size: {SizeBytes} bytes.",
            safeFileName,
            persisted.StorageEntryMetadataId,
            persisted.Sha256Hash,
            currentUserId,
            sizeBytes);

        return Result.CreateSuccessOk(PluginInfo.FromSubmissionEntity(persisted));
    }
}
