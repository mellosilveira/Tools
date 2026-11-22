namespace MelloSilveiraTools.Core.Commands.StorageEntryMetadata.Download;

/// <summary>
/// Request payload for downloading a stored file by its metadata entity ID.
/// </summary>
public sealed record DownloadStoredFileRequest
{
    /// <summary>
    /// Database ID of the stored file metadata record.
    /// </summary>
    public required long FileId { get; init; }
}
