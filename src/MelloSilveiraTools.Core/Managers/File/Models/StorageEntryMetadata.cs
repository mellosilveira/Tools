namespace MelloSilveiraTools.Core.Managers.File.Models;

/// <summary>
/// Immutable snapshot representing persistent storage entry (file or directory) metadata returned by <see cref="Providers.IFileStorageService"/>.
/// Encapsulates the resolved storage location, directory, file name, size, creation date, and underlying storage provider.
/// </summary>
/// <param name="StorageProvider">The underlying storage provider technology where the entry resides.</param>
/// <param name="FullPath">The physical path or canonical storage URI.</param>
/// <param name="Directory">The containing directory path or prefix.</param>
/// <param name="FileName">The file name with extension if this entry represents a file; otherwise, <c>null</c> if it represents a directory.</param>
/// <param name="SizeBytes">The size in bytes (file size or cumulative directory size).</param>
/// <param name="CreatedAt">UTC timestamp when the entry was created or persisted.</param>
public sealed record StorageEntryMetadata(
    StorageProvider StorageProvider,
    string FullPath,
    string Directory,
    string? FileName,
    long SizeBytes,
    DateTimeOffset CreatedAt)
{
    private readonly string? _fileName = NormalizeFileName(FileName);

    /// <summary>
    /// Gets the file name with extension if this entry represents a file; otherwise, <c>null</c> if it represents a directory.
    /// </summary>
    public string? FileName
    {
        get => _fileName;
        init => _fileName = NormalizeFileName(value);
    }

    /// <summary>
    /// Gets a value indicating whether this storage entry represents a directory.
    /// </summary>
    public bool IsDirectory => string.IsNullOrEmpty(FileName);

    /// <summary>
    /// Gets a value indicating whether this storage entry represents a file.
    /// </summary>
    public bool IsFile => !IsDirectory;

    private static string? NormalizeFileName(string? fileName) => string.IsNullOrEmpty(fileName) ? null : fileName;
}
