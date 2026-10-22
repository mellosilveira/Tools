using MelloSilveiraTools.Core.Application.Commands;
using MelloSilveiraTools.Core.Managers.File.Models;
using MelloSilveiraTools.Core.Managers.File.Providers;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Plugins.Application.Validators;
using MelloSilveiraTools.Plugins.Infrastructure;
using Microsoft.Extensions.Logging;

namespace MelloSilveiraTools.Plugins.Application.Commands.Upload;

/// <summary>
/// Operation that receives and writes an uploaded plugin assembly into the incoming storage partition.
/// </summary>
/// <param name="logger">Logger instance for tracing and diagnostics.</param>
/// <param name="fileStorageService">Storage service for writing the uploaded file.</param>
/// <param name="validator">Validator ensuring the file is a conforming managed .NET assembly.</param>
/// <param name="settings">Plugin settings defining the storage context.</param>
public class UploadPlugin(
    ILogger<UploadPlugin> logger,
    IFileStorageService fileStorageService,
    UploadPluginValidator validator,
    PluginSettings settings)
    : CommandBaseWithData<UploadPluginRequest, StorageEntryMetadata>(validator)
{
    /// <inheritdoc/>
    protected override async Task<Result<StorageEntryMetadata>> ExecuteCommandAsync(UploadPluginRequest request, CancellationToken cancellationToken = default)
    {
        string location = fileStorageService.ResolveLocation(settings.Directory, PluginStoragePartitions.Incoming, request.File.FileName);

        await using (Stream sourceStream = request.File.OpenReadStream())
        {
            await using (Stream targetStream = await fileStorageService.OpenWriteAsync(location, cancellationToken).ConfigureAwait(false))
            {
                await sourceStream.CopyToAsync(targetStream, cancellationToken).ConfigureAwait(false);
                await targetStream.FlushAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        StorageEntryMetadata metadata = await fileStorageService.GetMetadataAsync(location, cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Successfully uploaded plugin '{FileName}' to storage partition '{Partition}'. Size: {SizeBytes} bytes.",
            request.File.FileName,
            PluginStoragePartitions.Incoming,
            metadata.SizeBytes);

        return Result.CreateSuccessOk(metadata);
    }
}
