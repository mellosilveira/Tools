using Microsoft.AspNetCore.Http;

namespace MelloSilveiraTools.WebApi.Application.Commands.StorageEntryMetadata.Upload;

/// <summary>
/// Request payload for overwriting the physical file of an existing storage entry.
/// </summary>
public sealed record UpdateStorageEntryFileRequest
{
    /// <summary>
    /// Identifier of the existing metadata record.
    /// </summary>
    public required long FileId { get; init; }

    /// <summary>
    /// Replacement file content.
    /// </summary>
    public required IFormFile File { get; init; }
}
