using MelloSilveiraTools.Core.Managers.File.Models;
using System.Runtime.CompilerServices;

namespace MelloSilveiraTools.Core.Managers.File.Providers.Local;

/// <summary>
/// Storage-agnostic file management implementation targeting the local file system.
/// Implements canonical hierarchical directory structures partitioned by unique identifier and domain context ("storage/{identifier}/{context}/").
/// </summary>
/// <param name="settings">File storage settings defining the root base directory.</param>
/// <param name="timeProvider">The time provider instance used for temporal operations.</param>
public class LocalFileStorageService(LocalFileStorageSettings settings, TimeProvider timeProvider) : FileStorageServiceBase(timeProvider)
{
    private const FileOptions FileOptions = System.IO.FileOptions.SequentialScan | System.IO.FileOptions.Asynchronous;
    private const int BufferSize = 128 * 1024;

    /// <inheritdoc />
    public override StorageProvider Provider => StorageProvider.Local;

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
        string resolvedPath = Path.GetFullPath(Path.Combine(directory, fileName));
        string fullBasePath = Path.GetFullPath(settings.BaseDirectory);

        string relative = Path.GetRelativePath(fullBasePath, resolvedPath);
        return relative.StartsWith("..", StringComparison.Ordinal) || Path.IsPathRooted(relative)
            ? throw new InvalidOperationException($"Access denied. Resolved path '{resolvedPath}' escapes base directory '{fullBasePath}'.")
            : resolvedPath;
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
        return ValueTask.FromResult(System.IO.File.Exists(location) || Directory.Exists(location));
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

        if (System.IO.File.Exists(location))
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
            if (System.IO.File.Exists(location))
            {
                System.IO.File.Delete(location);
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
            Options = FileOptions,
            BufferSize = BufferSize
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
            Options = FileOptions,
            BufferSize = BufferSize
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
