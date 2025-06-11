namespace MelloSilveiraTools.Infrastructure.Database.Models;

/// <summary>
/// JOIN is an SQL clause used to query and access data from multiple tables, based on logical relationships between those tables.
/// </summary>
public enum JoinType
{
    /// <summary>
    /// Returns records that have matching values in both tables.
    /// </summary>
    Inner = 0,

    /// <summary>
    /// Returns all records from the left table, and the matched records from the right table.
    /// </summary>
    Left = 1,

    /// <summary>
    /// Returns all records from the right table, and the matched records from the left table.
    /// </summary>
    Right = 2,
}
