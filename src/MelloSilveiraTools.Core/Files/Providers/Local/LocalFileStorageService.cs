using MelloSilveiraTools.Core.Files.Models;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;

namespace MelloSilveiraTools.Core.Files.Providers.Local;

/// <summary>
/// Storage-agnostic file management implementation targeting the local file system.
/// Implements canonical hierarchical directory structures partitioned by
/// unique identifier and domain context (<c>{identifier}/{context}/</c>).
/// </summary>
/// <param name="loggerFactory">Logger factory used to create the category logger for this service.</param>
/// <param name="timeProvider">The time provider instance used for temporal operations.</param>
/// <param name="settings">File storage settings defining the root base directory.</param>
public class LocalFileStorageService(
    ILoggerFactory loggerFactory,
    TimeProvider timeProvider,
    LocalFileStorageSettings settings)
    : FileStorageServiceBase(timeProvider)
{
    private readonly ILogger<LocalFileStorageService> _logger = loggerFactory.CreateLogger<LocalFileStorageService>();

    /// <summary>
    /// Default asynchronous sequential file options used for local and DFS file streams.
    /// </summary>
    protected const FileOptions DefaultFileOptions = FileOptions.SequentialScan | FileOptions.Asynchronous;

    /// <summary>
    /// Default stream buffer size in bytes (128 KiB) used for local and DFS file streams.
    /// </summary>
    protected const int DefaultBufferSize = 128 * 1024;

    private static readonly char[] PathSeparators = [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar];

    /// <inheritdoc />
    public override StorageProvider Provider => StorageProvider.Local;

    /// <inheritdoc />
    public override async ValueTask<bool> IsHealthAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        string baseDirectory = Path.GetFullPath(settings.BaseDirectory);
        if (!Directory.Exists(baseDirectory))
            return false;

        byte[] expectedPayload = Guid.NewGuid().ToByteArray();
        string probeFilePath = Path.Combine(baseDirectory, $".health-probe-{Guid.NewGuid():N}.tmp");
        bool fileCreated = false;
        bool readMatched;
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
                    _logger.LogWarning(ex, "Failed to delete temporary health probe file '{ProbeFilePath}' during local file storage health check cleanup.", probeFilePath);
                }
            }

            throw;
        }

        File.Delete(probeFilePath);
        bool stillExists = File.Exists(probeFilePath);
        return readMatched && !stillExists;
    }

    /// <summary>
    /// Gets the file sharing mode used when creating write streams.
    /// </summary>
    protected virtual FileShare WriteFileShare => FileShare.None;

    /// <summary>
    /// Gets the file sharing mode used when opening read streams.
    /// </summary>
    protected virtual FileShare ReadFileShare => FileShare.Read;

    /// <inheritdoc />
    public override string ResolveLocation(string identifier, string context, string fileName)
    {
        string directory = GetDirectory(identifier, context);
        return Path.GetFullPath(Path.Combine(directory, fileName));
    }

    /// <inheritdoc />
    public override (string Identifier, string Context, string FileName) ParseLocation(string location)
    {
        string relative = Path.GetRelativePath(settings.BaseDirectory, location);
        if (relative.StartsWith("..", StringComparison.Ordinal) || Path.IsPathRooted(relative))
            throw new FormatException($"Location '{location}' is outside the configured base directory.");

        string[] segments = relative.Split(PathSeparators, StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length != 3)
            throw new FormatException($"Location '{location}' does not match the canonical '{{identifier}}/{{context}}/{{fileName}}' layout.");

        return (segments[0], segments[1], segments[2]);
    }

    /// <inheritdoc />
    public override ValueTask<Stream> OpenWriteAsync(string location, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Stream stream = CreateWriteStream(location);
        return ValueTask.FromResult(stream);
    }

    /// <inheritdoc />
    public override ValueTask<Stream> OpenReadAsync(string location, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Stream stream = OpenReadStream(location);
        return ValueTask.FromResult(stream);
    }

    /// <inheritdoc />
    public override ValueTask<bool> ExistsAsync(string location, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(File.Exists(location) || Directory.Exists(location));
    }

    /// <inheritdoc />
    public override ValueTask<StorageEntryMetadata> GetMetadataAsync(string location, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (Directory.Exists(location))
        {
            DirectoryInfo directoryInfo = new(location);
            directoryInfo.Refresh();

            long directorySizeBytes = GetDirectorySizeSecure(directoryInfo);
            string directoryPath = directoryInfo.Parent?.FullName ?? directoryInfo.FullName;
            return ValueTask.FromResult(new StorageEntryMetadata(Provider, directoryInfo.FullName, directoryPath, null, directorySizeBytes, directoryInfo.CreationTimeUtc));
        }

        if (File.Exists(location))
        {
            FileInfo fileInfo = new(location);
            fileInfo.Refresh();

            return ValueTask.FromResult(new StorageEntryMetadata(Provider, fileInfo.FullName, fileInfo.DirectoryName!, fileInfo.Name, fileInfo.Length, fileInfo.CreationTimeUtc));
        }

        throw new FileNotFoundException("The specified file or directory does not exist.", location);
    }

    /// <inheritdoc />
    public override ValueTask<bool> DeleteAsync(string location, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            if (File.Exists(location))
            {
                File.Delete(location);
                return ValueTask.FromResult(true);
            }

            if (Directory.Exists(location))
            {
                Directory.Delete(location, recursive: true);
                return ValueTask.FromResult(true);
            }
        }
        catch
        {
            return ValueTask.FromResult(false);
        }

        return ValueTask.FromResult(false);
    }

    /// <inheritdoc />
    public override async IAsyncEnumerable<StorageEntryMetadata> ListFilesAsync(string identifier, string context, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        string directory = GetDirectory(identifier, context);
        if (!Directory.Exists(directory))
            yield break;

        DirectoryInfo directoryInfo = new(directory);
        foreach (FileInfo fileInfo in directoryInfo.EnumerateFiles())
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return new StorageEntryMetadata(Provider, fileInfo.FullName, fileInfo.DirectoryName ?? directoryInfo.FullName, fileInfo.Name, fileInfo.Length, fileInfo.CreationTimeUtc);
        }

        await Task.CompletedTask.ConfigureAwait(false);
    }

    /// <inheritdoc />
    public override async IAsyncEnumerable<StorageEntryMetadata> ListEntriesAsync(string identifier, string context, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        string directory = GetDirectory(identifier, context);
        if (!Directory.Exists(directory))
            yield break;

        DirectoryInfo directoryInfo = new(directory);
        foreach (FileSystemInfo entry in directoryInfo.EnumerateFileSystemInfos())
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (entry is DirectoryInfo subDir)
            {
                long directorySizeBytes = GetDirectorySizeSecure(subDir);
                yield return new StorageEntryMetadata(Provider, subDir.FullName, directoryInfo.FullName, null, directorySizeBytes, subDir.CreationTimeUtc);
            }
            else if (entry is FileInfo fileInfo)
            {
                yield return new StorageEntryMetadata(Provider, fileInfo.FullName, fileInfo.DirectoryName ?? directoryInfo.FullName, fileInfo.Name, fileInfo.Length, fileInfo.CreationTimeUtc);
            }
        }

        await Task.CompletedTask.ConfigureAwait(false);
    }

    /// <summary>
    /// Computes the directory path for the given identifier and context partition.
    /// </summary>
    /// <param name="identifier">Unique run, experiment, or analysis identifier.</param>
    /// <param name="context">Business domain context.</param>
    /// <returns>The combined directory path.</returns>
    protected string GetDirectory(string identifier, string context) => string.IsNullOrEmpty(context)
        ? Path.Combine(settings.BaseDirectory, identifier)
        : Path.Combine(settings.BaseDirectory, identifier, context);

    /// <summary>
    /// Creates a write stream for the specified physical location.
    /// </summary>
    /// <param name="location">Physical file path.</param>
    /// <returns>A writable <see cref="FileStream"/>.</returns>
    protected Stream CreateWriteStream(string location)
    {
        string? directory = Path.GetDirectoryName(location);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        FileStreamOptions options = new()
        {
            Mode = FileMode.Create,
            Access = FileAccess.Write,
            Share = WriteFileShare,
            Options = DefaultFileOptions,
            BufferSize = DefaultBufferSize
        };
        return new FileStream(location, options);
    }

    /// <summary>
    /// Opens a read stream for the specified physical location.
    /// </summary>
    /// <param name="location">Physical file path.</param>
    /// <returns>A readable <see cref="FileStream"/>.</returns>
    protected Stream OpenReadStream(string location)
    {
        FileStreamOptions options = new()
        {
            Mode = FileMode.Open,
            Access = FileAccess.Read,
            Share = ReadFileShare,
            Options = DefaultFileOptions,
            BufferSize = DefaultBufferSize
        };

        return new FileStream(location, options);
    }

    /// <summary>
    /// Computes cumulative directory size recursively, skipping symlinks and junctions.
    /// </summary>
    /// <param name="directoryInfo">Directory information.</param>
    /// <returns>Total size in bytes.</returns>
    protected static long GetDirectorySizeSecure(DirectoryInfo directoryInfo)
    {
        long totalSize = 0;

        try
        {
            foreach (FileInfo file in directoryInfo.EnumerateFiles())
            {
                totalSize += file.Length;
            }

            foreach (DirectoryInfo subDir in directoryInfo.EnumerateDirectories())
            {
                if (!subDir.Attributes.HasFlag(FileAttributes.ReparsePoint))
                    totalSize += GetDirectorySizeSecure(subDir);
            }
        }
        catch (UnauthorizedAccessException) { }
        catch (DirectoryNotFoundException) { }

        return totalSize;
    }
}
