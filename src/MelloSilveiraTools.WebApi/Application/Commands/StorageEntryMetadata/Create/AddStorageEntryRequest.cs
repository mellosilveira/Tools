using MelloSilveiraTools.Core.Files.Models;
using MelloSilveiraTools.Core.Files.Providers;
using Microsoft.AspNetCore.Http;

namespace MelloSilveiraTools.WebApi.Application.Commands.StorageEntryMetadata.Create;

/// <summary>
/// Request payload for creating and persisting a stored file alongside its database metadata.
/// </summary>
public sealed record AddStorageEntryRequest
{
    /// <summary>
    /// Uploaded file to store.
    /// </summary>
    public required IFormFile File { get; init; }

    public required string Identifier { get; init; }

    /// <summary>
    /// Storage context partition.
    /// </summary>
    public required string Context { get; init; }

    /// <summary>
    /// Optional external correlation identifier (defaults to a generated GUID when omitted).
    /// </summary>
    public string? ExternalIdentifier { get; init; }

    /// <summary>
    /// Optional storage provider key; when omitted, resolves the default non-keyed <see cref="IFileStorageService"/>.
    /// </summary>
    public StorageProvider? StorageProvider { get; init; }
}
