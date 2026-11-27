using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Core.Validators;
using MelloSilveiraTools.Plugins.Application.Commands.Upload;
using MelloSilveiraTools.Plugins.Infrastructure;
using MelloSilveiraTools.Plugins.Infrastructure.Models;
using MelloSilveiraTools.Plugins.Infrastructure.Persistences;
using Microsoft.AspNetCore.Http;
using System.Reflection.PortableExecutable;

namespace MelloSilveiraTools.Plugins.Application.Validators;

/// <summary>
/// Shared validator for <see cref="UploadPluginRequest"/> (used by both <see cref="UploadPlugin"/> and
/// <see cref="Commands.Submissions.SubmitPlugin"/>), enforcing file size limits, naming conventions,
/// IL-only managed .NET assembly and host architecture compatibility, and active storage/database version conflict checks.
/// </summary>
/// <param name="fileProcessor">File processor checking whether the target assembly already exists in active storage partitions.</param>
/// <param name="submissionPersistence">Persistence service checking whether the plugin version has already been submitted.</param>
/// <param name="settings">Plugin settings supplying <see cref="PluginSettings.MaxPluginSizeBytes"/> and <see cref="PluginSettings.AllowUnmanagedDlls"/>.</param>
public class UploadPluginValidator(
    PluginFileProcessor fileProcessor,
    PluginSubmissionPersistence submissionPersistence,
    PluginSettings settings) : IValidator<UploadPluginRequest>
{
    /// <inheritdoc/>
    public async ValueTask<Result> ValidateAsync(UploadPluginRequest value, CancellationToken cancellationToken = default)
    {
        if (value is null || value.File is null || value.File.Length == 0)
            return Result.CreateBadRequest("Plugin file is required and cannot be empty.");

        long maxSizeBytes = settings.MaxPluginSizeBytes;
        if (value.File.Length > maxSizeBytes)
            return Result.CreateBadRequest($"Plugin file size ({value.File.Length} bytes) exceeds the maximum allowed size of {maxSizeBytes} bytes.");

        string safeFileName = Path.GetFileName(value.File.FileName);
        if (!value.File.FileName.Equals(safeFileName, StringComparison.Ordinal) || value.File.FileName.Contains("..", StringComparison.Ordinal))
            return Result.CreateBadRequest("Plugin filename must not contain directory separators or path traversal segments.");

        if (!safeFileName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            return Result.CreateBadRequest("Only .dll assembly files are permitted.");

        if (!fileProcessor.TryParsePluginFileName(safeFileName, out string pluginName, out PluginVersion pluginVersion))
            return Result.CreateBadRequest($"Plugin filename '{safeFileName}' does not conform to required pattern '{{name}}.v{{major}}.{{minor}}.{{patch}}.dll'.");

        Result binaryValidation = await ValidateBinaryAsync(value.File, maxSizeBytes, cancellationToken).ConfigureAwait(false);
        if (!binaryValidation.Success)
            return binaryValidation;

        if (await fileProcessor.ExistsInActivePartitionsAsync(safeFileName, cancellationToken).ConfigureAwait(false))
            return Result.CreateConflict($"Plugin assembly '{safeFileName}' already exists. Bump the semantic version instead of overwriting.");

        List<PluginSubmissionEntity> existing = await submissionPersistence.FindByPluginNameAsync(pluginName, cancellationToken).ConfigureAwait(false);
        if (existing.Any(s => s.PluginVersion.Equals(pluginVersion.Name, StringComparison.OrdinalIgnoreCase)))
            return Result.CreateConflict($"Plugin '{pluginName}' version '{pluginVersion.Name}' has already been submitted.");

        return Result.CreateSuccessOk();
    }

    private async Task<Result> ValidateBinaryAsync(IFormFile file, long maxSizeBytes, CancellationToken cancellationToken)
    {
        try
        {
            await using Stream stream = file.OpenReadStream();
            using MemoryStream? buffered = stream.CanSeek ? null : await CopyBoundedToMemoryAsync(stream, maxSizeBytes, cancellationToken).ConfigureAwait(false);
            if (!stream.CanSeek && buffered is null)
                return Result.CreateBadRequest($"Plugin file stream exceeds the maximum allowed size of {maxSizeBytes} bytes.");

            using PEReader peReader = new(buffered ?? stream, PEStreamOptions.LeaveOpen);
            if (!fileProcessor.IsArchitectureCompatible(peReader, out string failureReason))
                return Result.CreateBadRequest($"Uploaded file is not a valid compatible managed .NET assembly: {failureReason}");

            return Result.CreateSuccessOk();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return Result.CreateBadRequest($"Invalid PE binary format: {ex.Message}");
        }
    }

    private static async Task<MemoryStream?> CopyBoundedToMemoryAsync(Stream source, long maxBytes, CancellationToken cancellationToken)
    {
        MemoryStream destination = new();
        byte[] buffer = new byte[81920];
        long total = 0;
        int read;
        while ((read = await source.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
        {
            total += read;
            if (total > maxBytes)
            {
                destination.Dispose();
                return null;
            }

            await destination.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
        }

        destination.Position = 0;
        return destination;
    }
}
