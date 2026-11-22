using MelloSilveiraTools.Core.Files.Models;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Commands.StorageEntryMetadata;

/// <summary>
/// Query parameters for reading and grouping stored file entries from both storage and database.
/// </summary>
public sealed record ReadStorageEntriesRequest
{
    /// <summary>
    /// Optional external correlation identifier filter.
    /// </summary>
    public string? ExternalIdentifier { get; init; }

    public string? Identifier { get; init; }

    /// <summary>
    /// Optional context partition filter.
    /// </summary>
    public string? Context { get; init; }

    /// <summary>
    /// Optional storage provider filter.
    /// </summary>
    public StorageProvider? StorageProvider { get; init; }
}
