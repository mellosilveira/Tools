using MelloSilveiraTools.Infrastructure.Database.Models.Filters;

namespace MelloSilveiraTools.ExtensionMethods;

public static class EnumExtensions
{
    public static string ToNpgsqlString(this SortOrder sortOrder)
        => $"ORDER BY 1 {sortOrder.ToString().ToUpperInvariant()}";
}
