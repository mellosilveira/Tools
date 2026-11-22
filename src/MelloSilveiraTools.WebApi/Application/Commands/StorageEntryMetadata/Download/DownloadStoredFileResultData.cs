namespace MelloSilveiraTools.Core.Commands.StorageEntryMetadata.Download;

/// <summary>
/// Stream and metadata payload for an HTTP file download response.
/// </summary>
public sealed record DownloadStoredFileResultData
{
    /// <summary>
    /// Readable stream of the physical file.
    /// </summary>
    public required Stream FileStream { get; init; }

    /// <summary>
    /// MIME content type of the file.
    /// </summary>
    public required string ContentType { get; init; }

    /// <summary>
    /// Download file name including extension.
    /// </summary>
    public required string FileName { get; init; }

    public required long SizeBytes { get; init; }
}
