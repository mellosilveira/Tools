using MelloSilveiraTools.Core.Managers.File.Models;

namespace MelloSilveiraTools.Core.Managers.File.Providers.Local;

/// <summary>
/// Storage-agnostic file management implementation targeting the local file system.
/// Implements canonical hierarchical directory structures partitioned by domain context and unique identifier ("storage/{context}/{identifier}/").
/// </summary>
/// <param name="settings">File storage settings defining the root base directory.</param>
public class LocalFileStorageService(LocalFileStorageSettings settings) : FileStorageServiceBase
{
    /// <inheritdoc />
    public override StorageProvider Provider => StorageProvider.Local;

    /// <inheritdoc />
    public override string ResolveLocation(string context, string identifier, string fileName)
    {
        string directory = GetDirectory(context, identifier);
        return Path.GetFullPath(Path.Combine(directory, fileName));
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
        return ValueTask.FromResult(System.IO.File.Exists(location));
    }

    /// <inheritdoc />
    public override ValueTask<FileMetadata> GetMetadataAsync(string location, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(GetMetadataFromLocation(location));
    }

    /// <inheritdoc />
    public override ValueTask<bool> DeleteAsync(string location, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(DeleteFromLocation(location));
    }

    /// <inheritdoc />
    public override ValueTask<IReadOnlyList<FileMetadata>> ListFilesAsync(string context, string identifier, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        string directory = GetDirectory(context, identifier);
        if (!Directory.Exists(directory))
            return ValueTask.FromResult<IReadOnlyList<FileMetadata>>([]);

        string[] filePaths = Directory.GetFiles(directory);
        List<FileMetadata> metadataList = new(filePaths.Length);

        foreach (string filePath in filePaths)
        {
            metadataList.Add(GetMetadataFromLocation(filePath));
        }

        return ValueTask.FromResult<IReadOnlyList<FileMetadata>>(metadataList);
    }

    private string GetDirectory(string context, string identifier) => string.IsNullOrEmpty(identifier)
        ? Path.Combine(settings.BaseDirectory, context)
        : Path.Combine(settings.BaseDirectory, context, identifier);

    private static Stream CreateWriteStream(string location)
    {
        string? directory = Path.GetDirectoryName(location);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        FileStreamOptions options = new()
        {
            Mode = FileMode.Create,
            Access = FileAccess.Write,
            Share = FileShare.None,
            Options = FileOptions.SequentialScan | FileOptions.Asynchronous,
            BufferSize = 128 * 1024
        };

        return new FileStream(location, options);
    }

    private static Stream OpenReadStream(string location)
    {
        FileStreamOptions options = new()
        {
            Mode = FileMode.Open,
            Access = FileAccess.Read,
            Share = FileShare.Read,
            Options = FileOptions.SequentialScan | FileOptions.Asynchronous,
            BufferSize = 128 * 1024
        };

        return new FileStream(location, options);
    }

    private static FileMetadata GetMetadataFromLocation(string location)
    {
        FileInfo fileInfo = new(location);
        fileInfo.Refresh();

        long sizeBytes = fileInfo.Exists ? fileInfo.Length : 0L;
        DateTimeOffset createdAt = fileInfo.Exists ? fileInfo.CreationTimeUtc : DateTimeOffset.UtcNow;

        return new FileMetadata(StorageProvider.Local, fileInfo.FullName, sizeBytes, createdAt);
    }

    private static bool DeleteFromLocation(string location)
    {
        if (System.IO.File.Exists(location))
        {
            System.IO.File.Delete(location);
            return true;
        }

        return false;
    }
}
