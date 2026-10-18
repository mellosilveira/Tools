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
    public (Stream Stream, FileMetadata Metadata) Create(string context, string identifier, string fileName)
    {
        string location = ResolvePath(context, identifier, fileName);
        Stream stream = CreateWriteStream(location);
        FileMetadata metadata = new(StorageProvider.Local, Path.GetFullPath(location), 0L, DateTimeOffset.UtcNow);
        return (stream, metadata);
    }

    /// <inheritdoc />
    public (Stream Stream, FileMetadata Metadata) CreateTimebased(string context, string identifier, string fileExtension, string? filePrefix = null)
    {
        string location = ResolveTimebasedPath(context, identifier, fileExtension, filePrefix);
        Stream stream = CreateWriteStream(location);
        FileMetadata metadata = new(StorageProvider.Local, Path.GetFullPath(location), 0L, DateTimeOffset.UtcNow);
        return (stream, metadata);
    }

    /// <inheritdoc />
    public Stream OpenRead(string location) => OpenReadStream(location);

    /// <inheritdoc />
    public Stream OpenRead(string context, string identifier, string fileName)
    {
        string location = ResolvePath(context, identifier, fileName);
        return OpenReadStream(location);
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
    public FileMetadata GetMetadata(string context, string identifier, string fileName)
    {
        string location = ResolvePath(context, identifier, fileName);
        return GetMetadataFromLocation(location);
    }

    /// <inheritdoc />
    public bool Delete(string context, string identifier, string fileName)
    {
        string location = ResolvePath(context, identifier, fileName);
        return DeleteFromLocation(location);
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
            metadataList.Add(GetMetadataFromLocation(filePath));
        }

        return metadataList;
    }

    private string GetDirectory(string context, string identifier) => string.IsNullOrEmpty(identifier)
        ? Path.Combine(_settings.BaseDirectory, context)
        : Path.Combine(_settings.BaseDirectory, context, identifier);

    private string ResolvePath(string context, string identifier, string fileName)
    {
        string directory = GetDirectory(context, identifier);
        return Path.Combine(directory, fileName);
    }

    private string ResolveTimebasedPath(string context, string identifier, string fileExtension, string? filePrefix = null)
    {
        string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        string timestampedName = string.IsNullOrEmpty(filePrefix)
            ? $"{timestamp}{fileExtension}"
            : $"{filePrefix}_{timestamp}{fileExtension}";
        return ResolvePath(context, identifier, timestampedName);
    }

    private static Stream CreateWriteStream(string location)
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

