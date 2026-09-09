namespace MelloSilveiraTools.Database.RelationalDatabase.Models.Filters;

/// <summary>
/// Direction used to sort query results.
/// </summary>
public enum SortOrder : int
{
    /// <summary>
    /// Ascending order (smallest value first).
    /// </summary>
    Asc = 1,

    /// <summary>
    /// Descending order (largest value first).
    /// </summary>
    Desc = 2,
}
