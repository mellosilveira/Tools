namespace MelloSilveiraTools.Database.RelationalDatabase.Models.Filters;

/// <summary>
/// Base record for filter types consumed by repository query methods.
/// </summary>
public record FilterBase { }

/// <summary>
/// Describes pagination and ordering options applied to a query.
/// </summary>
public record Pagination
{
    /// <summary>
    /// Order in which the results should be sorted.
    /// </summary>
    public SortOrder? SortOrder { get; init; }

    /// <summary>
    /// Maximum number of records to return.
    /// </summary>
    public int? Limit { get; init; }

    /// <summary>
    /// Number of records to skip before returning results.
    /// </summary>
    public int? Offset { get; init; }
}
