namespace MelloSilveiraTools.WebApi.Application.Commands.StorageEntryMetadata.Download;

/// <summary>
/// Request payload for downloading a storage entry by its metadata entity ID.
/// </summary>
public sealed record DownloadStorageEntryRequest
{
    /// <summary>
    /// Database ID of the storage entry metadata record.
    /// </summary>
    public required long FileId { get; init; }
}
