using MelloSilveiraTools.Database.RelationalDatabase.Attributes;
using MelloSilveiraTools.Database.RelationalDatabase.Models.Entities;

namespace MelloSilveiraTools.Database.RelationalDatabase.Models.Filters;

/// <summary>
/// Filter model used to query <see cref="UserEntity"/> records in the <c>users</c> table.
/// </summary>
[Filter(typeof(UserEntity))]
public record UserFilter : FilterBase
{
    /// <summary>
    /// Optional exact username filter.
    /// </summary>
    [FilterColumn(FilterClause.Equal)]
    public string? Username { get; init; }

    /// <summary>
    /// Optional active status filter.
    /// </summary>
    [FilterColumn(FilterClause.Equal)]
    public bool? IsActive { get; init; }
}
