using MelloSilveiraTools.Database.RelationalDatabase.Repositories;

namespace MelloSilveiraTools.Database.RelationalDatabase.Sql.Provider;

/// <summary>
/// Provides SQL statements tailored to a specific database dialect for an entity type provided per call.
/// </summary>
/// <remarks>
/// <para>
/// The SQL strings returned by this interface contain textual placeholders prefixed with <c>#</c> that the
/// caller (typically <see cref="PostgresRepository"/>)
/// substitutes via <see cref="string.Replace(string, string?)"/> before execution. Implementations must keep
/// these placeholder names stable. The placeholders currently emitted are:
/// </para>
/// <list type="bullet">
///   <item><description><c>#WHERE</c> — replaced with the full <c>WHERE ...</c> clause built from a <c>FilterBase</c>, or with <c>null</c>/empty when no filter applies.</description></item>
///   <item><description><c>#ORDERBY</c> — replaced with the <c>ORDER BY ...</c> clause derived from the requested <c>SortOrder</c>.</description></item>
///   <item><description><c>#LIMIT</c> — replaced with <c>LIMIT n</c> for paginated queries (removed otherwise).</description></item>
///   <item><description><c>#OFFSET</c> — replaced with <c>OFFSET n</c> for paginated queries (removed otherwise).</description></item>
///   <item><description><c>#JOIN</c> — replaced with <c>LEFT/INNER JOIN</c> clauses derived from <c>[ForeignKeyColumn]</c> attributes on the entity (only present in SELECT templates).</description></item>
/// </list>
/// <para>
/// Internal placeholders such as <c>#TABLE_NAME</c>, <c>#TABLE_ALIAS</c>, <c>#COLUMNS</c>, <c>#VALUES</c>,
/// <c>#PARAMETER_NAMES</c>, <c>#PRIMARY_KEY</c>, <c>#VALUES_TO_UPDATE</c>, <c>#UNIQUE_COLUMNS</c> and
/// <c>#UNIQUE_UPDATES</c> are resolved by the provider itself before the SQL is returned, and callers should
/// not see them in the produced strings.
/// </para>
/// </remarks>
public interface ISqlProvider
{
    /// <summary>
    /// Builds a bulk INSERT statement capable of inserting <paramref name="batchSize"/> rows in a single round-trip.
    /// </summary>
    string GetBulkInsertSql<T>(int batchSize);

    /// <summary>
    /// Builds a SELECT COUNT statement with a placeholder for the WHERE clause.
    /// </summary>
    string GetCountSql<T>();

    /// <summary>
    /// Builds a DELETE statement with a placeholder for the WHERE clause.
    /// </summary>
    string GetDeleteSql<T>();

    /// <summary>
    /// Builds a DELETE statement filtered by the entity's primary key.
    /// </summary>
    string GetDeleteByPrimaryKeySql<T>();

    /// <summary>
    /// Builds a statement that checks whether a row with the given primary key exists.
    /// </summary>
    string GetExistByPrimaryKeySql<T>();

    /// <summary>
    /// Builds an INSERT statement for a single entity.
    /// </summary>
    string GetInsertSql<T>();

    /// <summary>
    /// Builds an INSERT statement that, on unique-key conflict, leaves the existing row intact and
    /// returns its primary key plus a boolean flag indicating whether a new row was created.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <typeparamref name="T"/> has no <c>[UniqueColumn]</c>-annotated property.
    /// </exception>
    string GetTryInsertSql<T>();

    /// <summary>
    /// Builds a SELECT statement with placeholders for WHERE, ORDER BY, LIMIT and OFFSET.
    /// </summary>
    string GetSelectSql<T>();

    /// <summary>
    /// Builds a SELECT DISTINCT statement with placeholders for WHERE, ORDER BY, LIMIT and OFFSET.
    /// </summary>
    string GetSelectDistinctSql<T>();

    /// <summary>
    /// Builds a SELECT statement filtered by the entity's primary key.
    /// </summary>
    string GetSelectByPrimaryKeySql<T>();

    /// <summary>
    /// Builds an UPDATE statement with a placeholder for the WHERE clause.
    /// </summary>
    string GetUpdateSql<T>();

    /// <summary>
    /// Builds an UPDATE statement filtered by the entity's primary key.
    /// </summary>
    string GetUpdateByPrimaryKeySql<T>();
}
