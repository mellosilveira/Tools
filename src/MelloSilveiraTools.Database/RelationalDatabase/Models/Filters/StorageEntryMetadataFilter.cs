using MelloSilveiraTools.Core.Files.Models;
using MelloSilveiraTools.Database.RelationalDatabase.Attributes;
using MelloSilveiraTools.Database.RelationalDatabase.Models.Entities;

namespace MelloSilveiraTools.Database.RelationalDatabase.Models.Filters;

/// <summary>
/// Filter model used to query <see cref="StorageEntryMetadataEntity"/> records in the <c>stored_files</c> table.
/// </summary>
[Filter(typeof(StorageEntryMetadataEntity))]
public record StorageEntryMetadataFilter : FilterBase
{
    /// <summary>
    /// Optional external identifier correlating the file to an analysis, experiment, or simulation run.
    /// </summary>
    [FilterColumn(FilterClause.Equal)]
    public string? ExternalIdentifier { get; init; }

    /// <summary>
    /// Optional storage provider technology where the artifact resides.
    /// </summary>
    [FilterColumn(FilterClause.Equal)]
    public StorageProvider? StorageProvider { get; init; }

    /// <summary>
    /// Optional absolute file system path, URI, or cloud blob key where the file can be retrieved.
    /// </summary>
    [FilterColumn(FilterClause.Like)]
    public string? Location => string.IsNullOrWhiteSpace(Identifier) && string.IsNullOrWhiteSpace(Context) ? null : string.Join('%', Identifier, Context);

    /// <summary>
    /// Optional directory path or prefix where the file is stored.
    /// </summary>
    [FilterColumn(FilterClause.Like)]
    public string? Directory { get; init; }

    /// <summary>
    /// Optional physical file name including extension.
    /// </summary>
    [FilterColumn(FilterClause.Like)]
    public string? FileName { get; init; }

    /// <summary>
    /// Optional MIME content type representing the file format.
    /// </summary>
    [FilterColumn(FilterClause.Equal)]
    public string? ContentType { get; init; }

    public string? Identifier { get; init; }

    public string? Context { get; init; }
}
