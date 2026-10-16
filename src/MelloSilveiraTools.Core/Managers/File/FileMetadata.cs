namespace MelloSilveiraTools.Core.Managers.File;

/// <summary>
/// Encapsulates comprehensive metadata describing a generated or stored file artifact,
/// including physical location, byte size, and creation timestamps.
/// Built to be storage-agnostic across Local File Systems, Blob Storage, and DFS providers.
/// </summary>
public sealed record FileMetadata
{
    /// <summary>
    /// Gets the complete, absolute file system path or canonical storage URI (Single Source of Truth).
    /// </summary>
    public string FullPath { get; init; }

    /// <summary>
    /// Gets the base file name with extension.
    /// </summary>
    public string FileName { get; }

    /// <summary>
    /// Gets the directory, container, or virtual prefix path housing the file.
    /// </summary>
    public string DirectoryPath { get; }

    /// <summary>
    /// Gets the total file size in bytes.
    /// </summary>
    public long SizeBytes { get; init; }

    /// <summary>
    /// Gets the timestamp when the file artifact was generated.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="FileMetadata"/> record.
    /// Resolves <see cref="FileName"/> and <see cref="DirectoryPath"/> concurrently from <paramref name="fullPath"/> in a single index pass.
    /// </summary>
    /// <param name="fullPath">Complete physical path, storage URI, or blob key.</param>
    /// <param name="sizeBytes">Total file size in bytes.</param>
    /// <param name="createdAt">Optional creation timestamp (defaults to UTC now).</param>
    /// <param name="fileName">Optional override for the file name if different from the path segment.</param>
    public FileMetadata(
        string fullPath,
        long sizeBytes,
        DateTimeOffset? createdAt = null,
        string? fileName = null)
    {
        FullPath = fullPath;
        SizeBytes = sizeBytes;
        CreatedAt = createdAt ?? DateTimeOffset.UtcNow;

        int lastSlash = fullPath.LastIndexOfAny(['/', '\\']);
        if (lastSlash >= 0)
        {
            DirectoryPath = fullPath[..lastSlash];
            FileName = fileName ?? fullPath[(lastSlash + 1)..];
        }
        else
        {
            DirectoryPath = string.Empty;
            FileName = fileName ?? fullPath;
        }
    }
}
