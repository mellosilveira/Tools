namespace MelloSilveiraTools.Infrastructure.Database.Models.Filters;

public record FilterBase { }

public record Pagination
{
    public SortOrder? SortOrder { get; init; }

    public int? Limit { get; init; }

    public int? Offset { get; init; }
}
