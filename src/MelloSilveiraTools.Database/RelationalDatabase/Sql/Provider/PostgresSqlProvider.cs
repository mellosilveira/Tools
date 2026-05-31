using MelloSilveiraTools.Core.ExtensionMethods;
using MelloSilveiraTools.Database.ExtensionMethods;
using MelloSilveiraTools.Database.RelationalDatabase.Attributes;
using System.Collections.Concurrent;
using System.Reflection;
using System.Text;

namespace MelloSilveiraTools.Database.RelationalDatabase.Sql.Provider;

/// <inheritdoc cref="ISqlProvider"/>
/// <remarks>
/// <para>
/// Generated SQL strings are memoized in a process-wide static <see cref="ConcurrentDictionary{TKey, TValue}"/>
/// keyed by <c>(Type, Operation, BatchSize)</c>. Entries are never evicted: the cache lives for the entire
/// application lifetime. Memory growth is therefore bounded by the number of entity types multiplied by the
/// number of supported operations (and, for bulk inserts, by the number of distinct batch sizes used).
/// </para>
/// <para>
/// Reads and writes to the cache are thread-safe via <see cref="ConcurrentDictionary{TKey, TValue}"/>;
/// concurrent first-time generation of the same key may execute the factory more than once, but the result
/// is deterministic for a given type and only one instance is retained.
/// </para>
/// <para>
/// This provider is NOT multi-tenant safe when different tenants map the same CLR type to different schemas
/// or table names: the cached SQL is keyed only by <see cref="Type"/>, not by tenant context. Use a separate
/// provider instance (or distinct entity types per tenant) when schema isolation is required.
/// </para>
/// </remarks>
public class PostgresSqlProvider : ISqlProvider
{
    // Metadados por tipo cacheados estaticamente — calculados uma vez por tipo por toda a vida da aplicação.
    private static readonly ConcurrentDictionary<Type, EntityMetadata> _metadataCache = [];

    // Cache unificado de SQL: (Type, Operation, BatchSize) → SQL string.
    // BatchSize é 0 para operações que não são bulk insert.
    private static readonly ConcurrentDictionary<(Type Type, Operation Op, int BatchSize), string> _sqlCache = [];

    private enum Operation { BulkInsert, Insert, TryInsert, Count, Delete, DeletePk, ExistPk, Select, SelectDistinct, SelectUniqueColumn, SelectPk, Update, UpdatePk }

    private record EntityMetadata(
        string TableName,
        string Alias,
        PropertyInfo PrimaryKey,
        string PrimaryKeyCol,
        List<(PropertyInfo Prop, string ColName)> AllColumns,
        List<(PropertyInfo Prop, string ColName)> UniqueColumns);

    #region ISqlProvider Implementation

    /// <inheritdoc/>
    public string GetBulkInsertSql<T>(int batchSize)
        => _sqlCache.GetOrAdd(
            (typeof(T), Operation.BulkInsert, batchSize),
            static key => CreateBatchInsertSql(key.Type, key.BatchSize));

    /// <inheritdoc/>
    public string GetCountSql<T>() => GetSql<T>(Operation.Count, CreateCountSql);

    /// <inheritdoc/>
    public string GetDeleteSql<T>() => GetSql<T>(Operation.Delete, CreateDeleteSql);

    /// <inheritdoc/>
    public string GetDeleteByPrimaryKeySql<T>() => GetSql<T>(Operation.DeletePk, CreateDeleteByPrimaryKeySql);

    /// <inheritdoc/>
    public string GetExistByPrimaryKeySql<T>() => GetSql<T>(Operation.ExistPk, CreateExistByPrimaryKeySql);

    /// <inheritdoc/>
    public string GetInsertSql<T>() => GetSql<T>(Operation.Insert, CreateInsertSql);

    /// <inheritdoc/>
    public string GetTryInsertSql<T>() => GetSql<T>(Operation.TryInsert, CreateTryInsertSql);

    /// <inheritdoc/>
    public string GetSelectSql<T>() => GetSql<T>(Operation.Select, CreateSelectSql);

    /// <inheritdoc/>
    public string GetSelectDistinctSql<T>() => GetSql<T>(Operation.SelectDistinct, CreateDistinctSelectSql);

    /// <inheritdoc/>
    public string GetSelectByPrimaryKeySql<T>() => GetSql<T>(Operation.SelectPk, CreateSelectByPrimaryKeySql);

    /// <inheritdoc/>
    public string GetSelectByUniqueColumnSql<T>() => GetSql<T>(Operation.SelectUniqueColumn, CreateSelectByUniqueColumnSql);

    /// <inheritdoc/>
    public string GetUpdateSql<T>() => GetSql<T>(Operation.Update, CreateUpdateSql);

    /// <inheritdoc/>
    public string GetUpdateByPrimaryKeySql<T>() => GetSql<T>(Operation.UpdatePk, CreateUpdateByPrimaryKeySql);

    #endregion

    #region Core Logic

    private static string GetSql<T>(Operation operation, Func<Type, string> factory, int batchSize = 0)
        => _sqlCache.GetOrAdd((typeof(T), operation, batchSize), static (key, f) => f(key.Type), factory);

    private static EntityMetadata GetMetadata(Type type)
        => _metadataCache.GetOrAdd(type, t =>
        {
            var tableAttr = t.GetCustomAttribute<TableAttribute>()
                ?? throw new InvalidOperationException($"O tipo {t.Name} não possui TableAttribute.");

            var props = t.GetPropertiesInHierarchy<ColumnAttribute>();
            var pk = props.FirstOrDefault(p => p.GetCustomAttribute<PrimaryKeyColumnAttribute>() != null)
                ?? throw new InvalidOperationException($"O tipo {t.Name} não possui uma PrimaryKey definida.");

            return new EntityMetadata(
                tableAttr.Name,
                tableAttr.Alias,
                pk,
                pk.Name.ToSnakeCase(),
                [.. props.Select(p => (p, p.Name.ToSnakeCase()))],
                [.. props.Where(p => p.GetCustomAttribute<UniqueColumnAttribute>() != null).Select(p => (p, p.Name.ToSnakeCase()))]
            );
        });

    #endregion

    #region SQL Factories

    private static string CreateBatchInsertSql(Type type, int batchSize)
    {
        var meta = GetMetadata(type);
        var cols = string.Join(", ", meta.AllColumns.Select(c => c.ColName));

        // Os sufixos devem ser 1-based (_1, _2, ...) para coincidir com BuildParametersFromCollection.
        var valueLines = new StringBuilder();
        for (int i = 0; i < batchSize; i++)
        {
            var suffix = $"_{i + 1}";
            valueLines.Append('(')
                      .Append(string.Join(", ", meta.AllColumns.Select(c => $"@{c.Prop.Name}{suffix}")))
                      .Append(')');
            if (i < batchSize - 1) valueLines.Append(",\r\n\t");
        }

        var template = meta.UniqueColumns.Count != 0
            ? SqlResource.BulkInsertWithUniqueKeyTemplate
            : SqlResource.BulkInsertTemplate;

        var sql = template
            .Replace("#TABLE_NAME", meta.TableName)
            .Replace("#COLUMNS", cols)
            .Replace("#VALUES", valueLines.ToString())
            .Replace("#PRIMARY_KEY", meta.PrimaryKeyCol);

        if (meta.UniqueColumns.Count != 0)
        {
            var uniqueCols = string.Join(", ", meta.UniqueColumns.Select(c => c.ColName));
            var uniqueUpdates = BuildUniqueUpdates(meta);
            sql = sql
                .Replace("#UNIQUE_COLUMNS", uniqueCols)
                .Replace("#UNIQUE_UPDATES", uniqueUpdates);
        }

        return sql;
    }

    private static string CreateCountSql(Type type)
    {
        var meta = GetMetadata(type);
        return $"SELECT COUNT(1) FROM {meta.TableName} AS {meta.Alias}\r\n#WHERE";
    }

    private static string CreateExistByPrimaryKeySql(Type type)
    {
        var meta = GetMetadata(type);
        return $"SELECT 1 FROM {meta.TableName} AS {meta.Alias}\r\nWHERE {meta.Alias}.{meta.PrimaryKeyCol} = @{meta.PrimaryKey.Name}\r\nLIMIT 1";
    }

    private static string CreateBaseSelectSql(Type type, bool isDistinct = false)
    {
        var meta = GetMetadata(type);
        var columns = meta.AllColumns.Select(c => $"{meta.Alias}.{c.ColName} AS \"{c.Prop.Name}\"").ToList();

        var joins = new StringBuilder();
        foreach (var (prop, colName) in meta.AllColumns)
        {
            var fk = prop.GetCustomAttribute<ForeignKeyColumnAttribute>();
            if (fk == null)
                continue;

            EntityMetadata refMeta = GetMetadata(fk.ReferencedTableType);
            joins.AppendLine($"{fk.JoinType} JOIN {refMeta.TableName} AS {refMeta.Alias} ON {refMeta.Alias}.{refMeta.PrimaryKeyCol} = {meta.Alias}.{colName}");
        }

        var sql = SqlResource.SelectTemplate;
        if (!isDistinct)
            sql = sql.Replace("SELECT DISTINCT", "SELECT");

        return sql
            .Replace("#COLUMNS", string.Join("\r\n\t,", columns))
            .Replace("#TABLE_NAME", meta.TableName)
            .Replace("#TABLE_ALIAS", meta.Alias)
            .Replace("#JOIN", joins.Length > 0 ? joins.ToString() : string.Empty);
    }

    private static string CreateSelectSql(Type type) => CreateBaseSelectSql(type, false);

    private static string CreateDistinctSelectSql(Type type) => CreateBaseSelectSql(type, true);

    private static string CreateSelectByPrimaryKeySql(Type type)
    {
        var meta = GetMetadata(type);
        return CreateSelectSql(type)
            .Replace("#WHERE", $"WHERE {meta.Alias}.{meta.PrimaryKeyCol} = @{meta.PrimaryKey.Name}")
            .Remove("#ORDERBY").Remove("#OFFSET").Remove("#LIMIT");
    }

    private static string CreateSelectByUniqueColumnSql(Type type)
    {
        var meta = GetMetadata(type);
        var (_, colName) = meta.UniqueColumns[0];
        return CreateSelectSql(type)
            .Replace("#WHERE", $"WHERE {meta.Alias}.{colName} = @UniqueColumnValue")
            .Remove("#ORDERBY").Remove("#OFFSET").Remove("#LIMIT");
    }

    private static string CreateInsertSql(Type type)
    {
        var meta = GetMetadata(type);
        var cols = string.Join(", ", meta.AllColumns.Select(c => c.ColName));
        var pars = string.Join(", ", meta.AllColumns.Select(c => $"@{c.Prop.Name}"));

        string sql = meta.UniqueColumns.Count == 0
            ? SqlResource.InsertTemplate
            : SqlResource.InsertWithUniqueKeyTemplate
                .Replace("#UNIQUE_COLUMNS", string.Join(", ", meta.UniqueColumns.Select(c => c.ColName)))
                .Replace("#UNIQUE_UPDATES", BuildUniqueUpdates(meta));

        return sql
            .Replace("#TABLE_NAME", meta.TableName)
            .Replace("#COLUMNS", cols)
            .Replace("#PARAMETER_NAMES", pars)
            .Replace("#PRIMARY_KEY", meta.PrimaryKeyCol);
    }

    private static string CreateTryInsertSql(Type type)
    {
        var meta = GetMetadata(type);
        var cols = string.Join(", ", meta.AllColumns.Select(c => c.ColName));
        var pars = string.Join(", ", meta.AllColumns.Select(c => $"@{c.Prop.Name}"));
        var uniqueCols = string.Join(", ", meta.UniqueColumns.Select(c => c.ColName));
        var uniqueWhere = string.Join(" AND ", meta.UniqueColumns.Select(c => $"{c.ColName} = @{c.Prop.Name}"));

        return SqlResource.TryInsertWithUniqueKeyTemplate
            .Replace("#TABLE_NAME", meta.TableName)
            .Replace("#COLUMNS", cols)
            .Replace("#PARAMETER_NAMES", pars)
            .Replace("#UNIQUE_COLUMNS", uniqueCols)
            .Replace("#UNIQUE_WHERE", uniqueWhere)
            .Replace("#PRIMARY_KEY", meta.PrimaryKeyCol);
    }

    private static string CreateUpdateSql(Type type)
    {
        var meta = GetMetadata(type);
        var updates = string.Join(",\r\n\t", meta.AllColumns.Select(c => $"{c.ColName} = @{c.Prop.Name}"));

        return SqlResource.UpdateTemplate
            .Replace("#TABLE_NAME", meta.TableName)
            .Replace("#VALUES_TO_UPDATE", updates);
    }

    private static string CreateUpdateByPrimaryKeySql(Type type)
    {
        var meta = GetMetadata(type);
        return CreateUpdateSql(type)
            .Replace("#WHERE", $"WHERE {meta.PrimaryKeyCol} = @{meta.PrimaryKey.Name};");
    }

    private static string CreateDeleteSql(Type type)
    {
        var meta = GetMetadata(type);
        return SqlResource.DeleteTemplate
            .Replace("#TABLE_NAME", meta.TableName)
            .Replace("#TABLE_ALIAS", meta.Alias);
    }

    private static string CreateDeleteByPrimaryKeySql(Type type)
    {
        var meta = GetMetadata(type);
        return CreateDeleteSql(type)
            .Replace("#WHERE", $"WHERE {meta.Alias}.{meta.PrimaryKeyCol} = @{meta.PrimaryKey.Name};");
    }

    /// <summary>
    /// Builds the DO UPDATE SET clause for ON CONFLICT.
    /// Updates payload columns (non-PK, non-unique); falls back to unique columns if no payload exists.
    /// </summary>
    private static string BuildUniqueUpdates(EntityMetadata meta)
    {
        var payloadCols = meta.AllColumns
            .Where(c => c.Prop.GetCustomAttribute<PrimaryKeyColumnAttribute>() == null &&
                        c.Prop.GetCustomAttribute<UniqueColumnAttribute>() == null)
            .ToList();

        return payloadCols.Count > 0
            ? string.Join(", ", payloadCols.Select(c => $"{c.ColName} = EXCLUDED.{c.ColName}"))
            : string.Join(", ", meta.UniqueColumns.Select(c => $"{c.ColName} = EXCLUDED.{c.ColName}"));
    }

    #endregion
}
