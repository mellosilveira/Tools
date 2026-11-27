using MelloSilveiraTools.Core.Files.Models;
using MelloSilveiraTools.Core.Files.Providers.Local;
using Microsoft.Extensions.Logging;

namespace MelloSilveiraTools.Core.Files.Providers.Dfs;

/// <summary>
/// Storage-agnostic file management implementation targeting distributed file systems (DFS) and network shares.
/// Inherits from <see cref="LocalFileStorageService"/> as DFS represents a network-mounted hierarchical file system,
/// configured with <see cref="FileShare.ReadWrite"/> to facilitate concurrent distributed access.
/// </summary>
/// <param name="loggerFactory">Logger factory used to create the category logger for this service.</param>
/// <param name="timeProvider">The time provider instance used for temporal operations.</param>
/// <param name="settings">DFS storage settings defining the root network share path or mount point.</param>
public class DfsFileStorageService(
    ILoggerFactory loggerFactory, 
    TimeProvider timeProvider, 
    DfsStorageSettings settings) 
    : LocalFileStorageService(loggerFactory, timeProvider, new LocalFileStorageSettings { BaseDirectory = settings.BaseDirectory })
{
    private readonly ILogger<DfsFileStorageService> _loger = loggerFactory.CreateLogger<DfsFileStorageService>();

    /// <inheritdoc />
    public override StorageProvider Provider => StorageProvider.Dfs;

    /// <inheritdoc />
    public override async ValueTask<bool> IsHealthAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        string baseDirectory = Path.GetFullPath(settings.BaseDirectory);
        if (!Directory.Exists(baseDirectory))
            return false;

        if (!string.IsNullOrWhiteSpace(settings.HealthMarkerFileName))
        {
            string markerPath = Path.GetFullPath(Path.Combine(baseDirectory, settings.HealthMarkerFileName));
            string relativeMarker = Path.GetRelativePath(baseDirectory, markerPath);
            if (relativeMarker.StartsWith("..", StringComparison.Ordinal) || Path.IsPathRooted(relativeMarker))
                return false;

            if (!File.Exists(markerPath))
                return false;
        }

        byte[] expectedPayload = Guid.NewGuid().ToByteArray();
        string probeFilePath = Path.Combine(baseDirectory, $".health-probe-{Guid.NewGuid():N}.tmp");
        bool fileCreated = false;
        bool readMatched = false;

        try
        {
            await using (FileStream writeStream = new(probeFilePath, FileMode.CreateNew, FileAccess.Write, WriteFileShare, DefaultBufferSize, DefaultFileOptions))
            {
                fileCreated = true;
                await writeStream.WriteAsync(expectedPayload, cancellationToken).ConfigureAwait(false);
                await writeStream.FlushAsync(cancellationToken).ConfigureAwait(false);
            }

            cancellationToken.ThrowIfCancellationRequested();

            byte[] actualPayload;
            await using (FileStream readStream = new(probeFilePath, FileMode.Open, FileAccess.Read, ReadFileShare, DefaultBufferSize, DefaultFileOptions))
            {
                using MemoryStream buffer = new(expectedPayload.Length);
                await readStream.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);
                actualPayload = buffer.ToArray();
            }

            readMatched = actualPayload.AsSpan().SequenceEqual(expectedPayload);
        }
        catch
        {
            if (fileCreated && File.Exists(probeFilePath))
            {
                try
                {
                    File.Delete(probeFilePath);
                }
                catch (Exception ex)
                {
                    // Best-effort cleanup so the original write or read failure is preserved.
                    _loger.LogWarning(ex, "Failed to delete temporary health probe file '{ProbeFilePath}' during DFS health check cleanup.", probeFilePath);
                }
            }

            throw;
        }

        File.Delete(probeFilePath);
        bool stillExists = File.Exists(probeFilePath);
        return readMatched && !stillExists;
    }

    /// <inheritdoc />
    protected override FileShare WriteFileShare => FileShare.ReadWrite;

    /// <inheritdoc />
    protected override FileShare ReadFileShare => FileShare.ReadWrite;
}
