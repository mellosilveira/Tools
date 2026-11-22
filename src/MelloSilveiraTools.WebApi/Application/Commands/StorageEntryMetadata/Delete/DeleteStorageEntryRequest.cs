using MelloSilveiraTools.Core.Files.Models;
using MelloSilveiraTools.Core.Files.Providers;

namespace MelloSilveiraTools.Core.Commands.StorageEntryMetadata.Delete;

/// <summary>
/// Request payload for idempotently deleting a stored file and its database metadata.
/// </summary>
public sealed record DeleteStorageEntryRequest
{
    /// <summary>
    /// Optional database metadata entity ID.
    /// </summary>
    public long? FileId { get; init; }

    /// <summary>
    /// Optional physical path or storage URI used when no database record exists.
    /// </summary>
    public string? Location { get; init; }

    /// <summary>
    /// Optional storage provider key used when no database record exists; defaults to the non-keyed <see cref="IFileStorageService"/>.
    /// </summary>
    public StorageProvider? StorageProvider { get; init; }
}
