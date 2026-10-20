namespace MelloSilveiraTools.Core.Managers.File.Models;

/// <summary>
/// Immutable snapshot representing persistent file metadata returned by <see cref="Providers.IFileStorageService"/>.
/// Encapsulates the resolved storage location, size, creation date, and underlying storage provider.
/// </summary>
public sealed record FileMetadata
{
    /// <summary>
    /// Initializes a new immutable instance of the <see cref="FileMetadata"/> record with its corresponding storage provider.
    /// </summary>
    /// <param name="storageProvider">The underlying storage provider technology where the file resides.</param>
    /// <param name="fullPath">The physical path or canonical storage URI.</param>
    /// <param name="sizeBytes">File size in bytes.</param>
    /// <param name="createdAt">UTC timestamp when the file was created or persisted.</param>
    public FileMetadata(StorageProvider storageProvider, string fullPath, long sizeBytes, DateTimeOffset createdAt)
    {
        StorageProvider = storageProvider;
        FullPath = fullPath;
        SizeBytes = sizeBytes;
        CreatedAt = createdAt;
    }

    /// <summary>
    /// Gets the underlying storage provider technology where the file resides.
    /// </summary>
    public StorageProvider StorageProvider { get; init; }

    /// <summary>
    /// Gets the full physical path or canonical storage URI of the file.
    /// </summary>
    public string FullPath { get; init; }

    /// <summary>
    /// Gets the file name including extension extracted from <see cref="FullPath"/>.
    /// </summary>
    public string FileName => Path.GetFileName(FullPath);

    /// <summary>
    /// Gets the file size in bytes.
    /// </summary>
    public long SizeBytes { get; init; }

    /// <summary>
    /// Gets the creation timestamp in UTC.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; }
}
