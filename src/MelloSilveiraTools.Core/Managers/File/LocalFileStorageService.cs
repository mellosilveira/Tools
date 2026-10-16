namespace MelloSilveiraTools.Core.Managers.File;

/// <summary>
/// Storage-agnostic file management implementation targeting the local file system.
/// Implements canonical hierarchical directory structures partitioned by domain context and unique identifier ("storage/{context}/{identifier}/").
/// </summary>
/// <param name="settings">Optional file storage settings defining the root base directory. Defaults to standard settings if omitted.</param>
public class LocalFileStorageService(FileStorageSettings? settings = null) : IFileStorageService
{
    private readonly FileStorageSettings _settings = settings ?? new FileStorageSettings();

    /// <inheritdoc />
    public string GetDirectory(string context, string identifier) => string.IsNullOrEmpty(identifier)
        ? Path.Combine(_settings.BaseDirectory, context)
        : Path.Combine(_settings.BaseDirectory, context, identifier);

    /// <inheritdoc />
    public string ResolvePath(string context, string identifier, string fileName)
    {
        string directory = GetDirectory(context, identifier);
        return Path.Combine(directory, fileName);
    }

    /// <inheritdoc />
    public string ResolveTimebasedPath(string context, string identifier, string filePrefix, string fileExtension)
    {
        string timestampedName = $"{filePrefix}_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}{fileExtension}";
        return ResolvePath(context, identifier, timestampedName);
    }

    /// <inheritdoc />
    public Stream OpenWrite(string location)
    {
        string? directory = Path.GetDirectoryName(location);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

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

    /// <inheritdoc />
    public Stream OpenWrite(string context, string identifier, string fileName)
    {
        string location = ResolvePath(context, identifier, fileName);
        return OpenWrite(location);
    }

    /// <inheritdoc />
    public Stream OpenRead(string location)
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

    /// <inheritdoc />
    public Stream OpenRead(string context, string identifier, string fileName)
    {
        string location = ResolvePath(context, identifier, fileName);
        return OpenRead(location);
    }

    /// <inheritdoc />
    public bool Exists(string location) => System.IO.File.Exists(location);

    /// <inheritdoc />
    public bool Exists(string context, string identifier, string fileName)
    {
        string location = ResolvePath(context, identifier, fileName);
        return Exists(location);
    }

    /// <inheritdoc />
    public FileMetadata GetMetadata(string location)
    {
        FileInfo fileInfo = new(location);
        fileInfo.Refresh();

        long sizeBytes = fileInfo.Exists ? fileInfo.Length : 0L;
        DateTimeOffset createdAt = fileInfo.Exists ? fileInfo.CreationTimeUtc : DateTimeOffset.UtcNow;

        return new FileMetadata(fileInfo.FullName, sizeBytes, createdAt);
    }

    /// <inheritdoc />
    public FileMetadata GetMetadata(string context, string identifier, string fileName)
    {
        string location = ResolvePath(context, identifier, fileName);
        return GetMetadata(location);
    }

    /// <inheritdoc />
    public bool Delete(string location)
    {
        if (System.IO.File.Exists(location))
        {
            System.IO.File.Delete(location);
            return true;
        }

        return false;
    }

    /// <inheritdoc />
    public bool Delete(string context, string identifier, string fileName)
    {
        string location = ResolvePath(context, identifier, fileName);
        return Delete(location);
    }

    /// <inheritdoc />
    public IReadOnlyList<FileMetadata> ListFiles(string context, string identifier)
    {
        string directory = GetDirectory(context, identifier);
        if (!Directory.Exists(directory))
        {
            return [];
        }

        string[] filePaths = Directory.GetFiles(directory);
        List<FileMetadata> metadataList = new(filePaths.Length);

        foreach (string filePath in filePaths)
        {
            metadataList.Add(GetMetadata(filePath));
        }

        return metadataList;
    }
}
