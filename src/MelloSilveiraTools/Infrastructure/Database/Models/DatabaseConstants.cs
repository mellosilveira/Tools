namespace MelloSilveiraTools.Infrastructure.Database.Models;

/// <summary>
/// Constant values related to database interactions.
/// </summary>
public class DatabaseConstants
{
    /// <summary>
    /// SQLSTATE code returned by PostgreSQL when a unique constraint is violated.
    /// </summary>
    public const string UniqueViolationSqlState = "23505";
}
