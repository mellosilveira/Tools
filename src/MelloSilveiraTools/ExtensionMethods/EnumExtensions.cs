using MelloSilveiraTools.Infrastructure.Database.Models.Filters;

namespace MelloSilveiraTools.ExtensionMethods;

public static class EnumExtensions
{
    public static string ToNpgsqlString(this SortOrder sortOrder) => sortOrder switch
    {
        SortOrder.Asc => "ORDER BY 1 ASC",
        SortOrder.Desc => "ORDER BY 1 DESC",
        _ => throw new ArgumentOutOfRangeException(nameof(sortOrder), sortOrder, null)
    };
}
