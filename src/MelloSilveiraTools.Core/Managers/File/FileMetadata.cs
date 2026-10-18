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
    // TODO: PERMITIR ATUALIZAÇÃO DA PROPRIEDADE APÓS ESCRITA TOTAL DO ARQUIVO.
    public long SizeBytes { get; init; }

    // TODO: ADICIONAR DOCUMENTAÇÃO
    public StorageProvider StorageProvider { get; init; }

    /// <summary>
    /// Gets the timestamp when the file artifact was generated.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="FileMetadata"/> record.
    /// Resolves <see cref="FileName"/> and <see cref="DirectoryPath"/> concurrently from <paramref name="fullPath"/> in a single index pass.
    /// </summary>
    // TODO: ADICIONAR DOCUMENTAÇÃO
    /// <param name="storageProvider"></param>
    /// <param name="fullPath">Complete physical path, storage URI, or blob key.</param>
    /// <param name="sizeBytes">Total file size in bytes.</param>
    /// <param name="createdAt">Optional creation timestamp (defaults to UTC now).</param>
    public FileMetadata(StorageProvider storageProvider, string fullPath, long sizeBytes, DateTimeOffset? createdAt = null)
    {
        FullPath = fullPath;
        SizeBytes = sizeBytes;
        CreatedAt = createdAt ?? DateTimeOffset.UtcNow;

        int lastSlash = fullPath.LastIndexOfAny(['/', '\\']);
        if (lastSlash >= 0)
        {
            DirectoryPath = fullPath[..lastSlash];
            FileName = fullPath[(lastSlash + 1)..];
        }
        else
        {
            DirectoryPath = string.Empty;
            FileName = fullPath;
        }
    }
}
