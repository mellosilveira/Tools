using MelloSilveiraTools.Database.RelationalDatabase.Models.Filters;

namespace MelloSilveiraTools.Database.ExtensionMethods;

/// <summary>
/// Contains extension methods for enumerations used across the Tools project.
/// </summary>
public static class EnumExtensions
{
    extension(SortOrder sortOrder)
    {
        /// <summary>
        /// Converts a <see cref="SortOrder"/> into the corresponding PostgreSQL <c>ORDER BY</c> clause.
        /// </summary>
        public string ToNpgsqlString() => sortOrder switch
        {
            SortOrder.Asc => "ORDER BY 1 ASC",
            SortOrder.Desc => "ORDER BY 1 DESC",
            _ => throw new ArgumentOutOfRangeException(nameof(sortOrder), sortOrder, null)
        };
    }
}
