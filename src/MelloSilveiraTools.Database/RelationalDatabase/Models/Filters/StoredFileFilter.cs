using MelloSilveiraTools.Database.RelationalDatabase.Attributes;
using MelloSilveiraTools.Database.RelationalDatabase.Models.Entities;

namespace MelloSilveiraTools.Database.RelationalDatabase.Models.Filters;

/// <summary>
/// Filter model used to query <see cref="StoredFileEntity"/> records by external identifier, context, or file name.
/// </summary>
[Filter(typeof(StoredFileEntity))]
public record StoredFileFilter : FilterBase
{
    /// <summary>
    /// Optional external identifier correlating the file to an analysis, experiment, or simulation run.
    /// </summary>
    [FilterColumn(FilterClause.Equal)]
    public string? ExternalIdentifier { get; init; }

    /// <summary>
    /// Optional business domain context under which the file was created.
    /// </summary>
    [FilterColumn(FilterClause.Equal)]
    public string? Context { get; init; }

    /// <summary>
    /// Optional physical file name including extension.
    /// </summary>
    [FilterColumn(FilterClause.Like)]
    public string? FileName { get; init; }
}
