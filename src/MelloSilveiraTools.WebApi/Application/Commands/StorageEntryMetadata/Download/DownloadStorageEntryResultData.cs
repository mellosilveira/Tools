namespace MelloSilveiraTools.WebApi.Application.Commands.StorageEntryMetadata.Download;

/// <summary>
/// Stream and metadata payload for an HTTP storage entry download response.
/// </summary>
public sealed record DownloadStorageEntryResultData
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

    /// <summary>
    /// File size in bytes.
    /// </summary>
    public required long SizeBytes { get; init; }
}
