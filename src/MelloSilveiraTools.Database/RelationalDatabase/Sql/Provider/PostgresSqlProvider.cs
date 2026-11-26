using MelloSilveiraTools.Core.ExtensionMethods;
using MelloSilveiraTools.Database.RelationalDatabase.Attributes;
using MelloSilveiraTools.Database.RelationalDatabase.Models.Entities;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;

namespace MelloSilveiraTools.Database.RelationalDatabase.Sql.Provider;

/// <inheritdoc cref="ISqlProvider"/>
/// <summary>
/// Registered as Singleton in DI. Thread-safe and Native AOT-compatible. Backed by per-type static generic caches.
/// </summary>
/// <remarks>
/// <para>
/// Generated SQL strings are memoized per entity type in static generic caches (and a <see cref="ConcurrentDictionary{TKey, TValue}"/>
/// keyed by batch size for bulk inserts). Entries live for the lifetime of the application so reflection-based SQL
/// generation runs at most once per entity type and operation without delegate reflection or AOT hazards.
/// </para>
/// <para>
/// For entities inheriting from <see cref="UserScopedEntityBase"/> that declare one or more
/// <see cref="UniqueColumnAttribute"/> properties, <c>user_id</c> is automatically appended to the
/// <c>ON CONFLICT</c> unique key list so conflict resolution aligns with PostgreSQL multi-tenant
/// unique constraints (<c>UNIQUE NULLS NOT DISTINCT (..., user_id)</c>).
/// </para>
/// </remarks>
public class PostgresSqlProvider : ISqlProvider
{
    private static readonly ConcurrentDictionary<Type, EntityMetadata> _metadataCache = [];

    private static class SqlCache<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>
    {
        public static readonly Lazy<string> Count = new(() => CreateCountSql(typeof(T)), LazyThreadSafetyMode.PublicationOnly);
        public static readonly Lazy<string> Delete = new(() => CreateDeleteSql(typeof(T)), LazyThreadSafetyMode.PublicationOnly);
        public static readonly Lazy<string> DeletePk = new(() => CreateDeleteByPrimaryKeySql(typeof(T)), LazyThreadSafetyMode.PublicationOnly);
        public static readonly Lazy<string> ExistPk = new(() => CreateExistByPrimaryKeySql(typeof(T)), LazyThreadSafetyMode.PublicationOnly);
        public static readonly Lazy<string> Insert = new(() => CreateInsertSql(typeof(T)), LazyThreadSafetyMode.PublicationOnly);
        public static readonly Lazy<string> TryInsert = new(() => CreateTryInsertSql(typeof(T)), LazyThreadSafetyMode.PublicationOnly);
        public static readonly Lazy<string> Select = new(() => CreateSelectSql(typeof(T)), LazyThreadSafetyMode.PublicationOnly);
        public static readonly Lazy<string> SelectDistinct = new(() => CreateDistinctSelectSql(typeof(T)), LazyThreadSafetyMode.PublicationOnly);
        public static readonly Lazy<string> SelectPk = new(() => CreateSelectByPrimaryKeySql(typeof(T)), LazyThreadSafetyMode.PublicationOnly);
        public static readonly Lazy<string> SelectUniqueColumn = new(() => CreateSelectByUniqueColumnSql(typeof(T)), LazyThreadSafetyMode.PublicationOnly);
        public static readonly Lazy<string> Update = new(() => CreateUpdateSql(typeof(T)), LazyThreadSafetyMode.PublicationOnly);
        public static readonly Lazy<string> UpdatePk = new(() => CreateUpdateByPrimaryKeySql(typeof(T)), LazyThreadSafetyMode.PublicationOnly);
        public static readonly ConcurrentDictionary<int, string> BulkInserts = [];
    }

    private record EntityMetadata(
        string TableName,
        string Alias,
        PropertyInfo PrimaryKey,
        string PrimaryKeyCol,
        List<(PropertyInfo Prop, string ColName)> AllColumns,
        List<(PropertyInfo Prop, string ColName)> InsertColumns,
        List<(PropertyInfo Prop, string ColName)> UniqueColumns);

    #region ISqlProvider Implementation

    /// <inheritdoc/>
    public string GetBulkInsertSql<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(int batchSize)
        => SqlCache<T>.BulkInserts.GetOrAdd(batchSize, static size => CreateBatchInsertSql(typeof(T), size));

    /// <inheritdoc/>
    public string GetCountSql<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>() => SqlCache<T>.Count.Value;

    /// <inheritdoc/>
    public string GetDeleteSql<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>() => SqlCache<T>.Delete.Value;

    /// <inheritdoc/>
    public string GetDeleteByPrimaryKeySql<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>() => SqlCache<T>.DeletePk.Value;

    /// <inheritdoc/>
    public string GetExistByPrimaryKeySql<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>() => SqlCache<T>.ExistPk.Value;

    /// <inheritdoc/>
    public string GetInsertSql<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>() => SqlCache<T>.Insert.Value;

    /// <inheritdoc/>
    public string GetTryInsertSql<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>() => SqlCache<T>.TryInsert.Value;

    /// <inheritdoc/>
    public string GetSelectSql<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>() => SqlCache<T>.Select.Value;

    /// <inheritdoc/>
    public string GetSelectDistinctSql<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>() => SqlCache<T>.SelectDistinct.Value;

    /// <inheritdoc/>
    public string GetSelectByPrimaryKeySql<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>() => SqlCache<T>.SelectPk.Value;

    /// <inheritdoc/>
    public string GetSelectByUniqueColumnSql<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>() => SqlCache<T>.SelectUniqueColumn.Value;

    /// <inheritdoc/>
    public string GetUpdateSql<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>() => SqlCache<T>.Update.Value;

    /// <inheritdoc/>
    public string GetUpdateByPrimaryKeySql<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>() => SqlCache<T>.UpdatePk.Value;

    #endregion

    #region Core Logic

    private static EntityMetadata GetMetadata([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type type)
    {
        if (_metadataCache.TryGetValue(type, out EntityMetadata? cached))
            return cached;

        EntityMetadata created = BuildMetadata(type);
        return _metadataCache.GetOrAdd(type, created);
    }

    private static EntityMetadata BuildMetadata([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type type)
    {
        TableAttribute tableAttr = type.GetCustomAttribute<TableAttribute>() ?? throw new InvalidOperationException($"O tipo {type.Name} não possui TableAttribute.");
        PropertyInfo[] props = type.GetPropertiesInHierarchy<ColumnAttribute>();
        PropertyInfo pk = props.FirstOrDefault(p => p.GetCustomAttribute<PrimaryKeyColumnAttribute>() != null) ?? throw new InvalidOperationException($"O tipo {type.Name} não possui uma PrimaryKey definida.");

        List<(PropertyInfo Prop, string ColName)> allColumns = [.. props.Select(p => (p, p.Name.ToSnakeCase()))];
        List<(PropertyInfo Prop, string ColName)> insertColumns =
        [
            .. allColumns.Where(c =>
                c.Prop.GetCustomAttribute<PrimaryKeyColumnAttribute>() is null
                && !string.Equals(c.Prop.Name, nameof(EntityBase.CreationTimestamp), StringComparison.Ordinal))
        ];
        List<(PropertyInfo Prop, string ColName)> uniqueColumns = [.. props.Where(p => p.GetCustomAttribute<UniqueColumnAttribute>() != null).Select(p => (p, p.Name.ToSnakeCase()))];

        if (uniqueColumns.Count > 0 && typeof(UserScopedEntityBase).IsAssignableFrom(type))
        {
            (PropertyInfo Prop, string ColName) userIdCol = allColumns.FirstOrDefault(c => c.Prop.Name == nameof(UserScopedEntityBase.UserId));
            if (userIdCol.Prop is not null && !uniqueColumns.Any(c => c.Prop.Name == nameof(UserScopedEntityBase.UserId)))
                uniqueColumns.Add(userIdCol);
        }

        return new EntityMetadata(
            tableAttr.Name,
            tableAttr.Alias,
            pk,
            pk.Name.ToSnakeCase(),
            allColumns,
            insertColumns,
            uniqueColumns
        );
    }

    #endregion

    #region SQL Factories

    private static string CreateBatchInsertSql([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type type, int batchSize)
    {
        EntityMetadata meta = GetMetadata(type);
        string cols = string.Join(", ", meta.InsertColumns.Select(c => c.ColName));

        // Os sufixos devem ser 1-based (_1, _2, ...) para coincidir com BuildParametersFromCollection.
        StringBuilder valueLines = new();
        for (int i = 0; i < batchSize; i++)
        {
            string suffix = $"_{i + 1}";
            valueLines.Append('(')
                      .Append(string.Join(", ", meta.InsertColumns.Select(c => $"@{c.Prop.Name}{suffix}")))
                      .Append(')');
            if (i < batchSize - 1) valueLines.Append(",\r\n\t");
        }

        string template = meta.UniqueColumns.Count != 0
            ? SqlResource.BulkInsertWithUniqueKeyTemplate
            : SqlResource.BulkInsertTemplate;

        string sql = template
            .Replace("#TABLE_NAME", meta.TableName)
            .Replace("#COLUMNS", cols)
            .Replace("#VALUES", valueLines.ToString())
            .Replace("#PRIMARY_KEY", meta.PrimaryKeyCol);

        if (meta.UniqueColumns.Count != 0)
        {
            string uniqueCols = string.Join(", ", meta.UniqueColumns.Select(c => c.ColName));
            string uniqueUpdates = BuildUniqueUpdates(meta);
            sql = sql
                .Replace("#UNIQUE_COLUMNS", uniqueCols)
                .Replace("#UNIQUE_UPDATES", uniqueUpdates);
        }

        return sql;
    }

    private static string CreateCountSql([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type type)
    {
        EntityMetadata meta = GetMetadata(type);
        return $"SELECT COUNT(1) FROM {meta.TableName} AS {meta.Alias}\r\n#WHERE";
    }

    private static string CreateExistByPrimaryKeySql([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type type)
    {
        EntityMetadata meta = GetMetadata(type);
        return $"SELECT 1 FROM {meta.TableName} AS {meta.Alias}\r\nWHERE {meta.Alias}.{meta.PrimaryKeyCol} = @{meta.PrimaryKey.Name}\r\nLIMIT 1";
    }

    private static string CreateBaseSelectSql([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type type, bool isDistinct = false)
    {
        EntityMetadata meta = GetMetadata(type);
        List<string> columns = [.. meta.AllColumns.Select(c => $"{meta.Alias}.{c.ColName} AS \"{c.Prop.Name}\"")];

        StringBuilder joins = new();
        HashSet<string> usedAliases = new(StringComparer.OrdinalIgnoreCase) { meta.Alias };
        foreach ((PropertyInfo prop, string colName) in meta.AllColumns)
        {
            ForeignKeyColumnAttribute? fk = prop.GetCustomAttribute<ForeignKeyColumnAttribute>();
            if (fk == null)
                continue;

            EntityMetadata refMeta = GetMetadata(fk.ReferencedTableType);
            string joinAlias = usedAliases.Add(refMeta.Alias) ? refMeta.Alias : $"{refMeta.Alias}_{colName}";
            joins.AppendLine($"{fk.JoinType} JOIN {refMeta.TableName} AS {joinAlias} ON {joinAlias}.{refMeta.PrimaryKeyCol} = {meta.Alias}.{colName}");
        }

        string sql = SqlResource.SelectTemplate;
        if (!isDistinct)
            sql = sql.Replace("SELECT DISTINCT", "SELECT");

        return sql
            .Replace("#COLUMNS", string.Join("\r\n\t,", columns))
            .Replace("#TABLE_NAME", meta.TableName)
            .Replace("#TABLE_ALIAS", meta.Alias)
            .Replace("#JOIN", joins.Length > 0 ? joins.ToString() : string.Empty);
    }

    private static string CreateSelectSql([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type type) => CreateBaseSelectSql(type, false);

    private static string CreateDistinctSelectSql([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type type) => CreateBaseSelectSql(type, true);

    private static string CreateSelectByPrimaryKeySql([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type type)
    {
        EntityMetadata meta = GetMetadata(type);
        return CreateSelectSql(type)
            .Replace("#WHERE", $"WHERE {meta.Alias}.{meta.PrimaryKeyCol} = @{meta.PrimaryKey.Name}")
            .Remove("#ORDERBY").Remove("#OFFSET").Remove("#LIMIT");
    }

    private static string CreateSelectByUniqueColumnSql([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type type)
    {
        EntityMetadata meta = GetMetadata(type);
        (_, string colName) = meta.UniqueColumns[0];
        return CreateSelectSql(type)
            .Replace("#WHERE", $"WHERE {meta.Alias}.{colName} = @UniqueColumnValue")
            .Remove("#ORDERBY").Remove("#OFFSET").Remove("#LIMIT");
    }

    private static string CreateInsertSql([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type type)
    {
        EntityMetadata meta = GetMetadata(type);
        string cols = string.Join(", ", meta.InsertColumns.Select(c => c.ColName));
        string pars = string.Join(", ", meta.InsertColumns.Select(c => $"@{c.Prop.Name}"));

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

    private static string CreateTryInsertSql([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type type)
    {
        EntityMetadata meta = GetMetadata(type);
        string cols = string.Join(", ", meta.InsertColumns.Select(c => c.ColName));
        string pars = string.Join(", ", meta.InsertColumns.Select(c => $"@{c.Prop.Name}"));

        if (meta.UniqueColumns.Count == 0)
            return $"INSERT INTO {meta.TableName}\r\n(\r\n\t{cols}\r\n)\r\nVALUES\r\n(\r\n\t{pars}\r\n)\r\nRETURNING {meta.PrimaryKeyCol}, TRUE AS inserted;";

        string uniqueCols = string.Join(", ", meta.UniqueColumns.Select(c => c.ColName));
        string uniqueWhere = string.Join(" AND ", meta.UniqueColumns.Select(c => $"{c.ColName} IS NOT DISTINCT FROM @{c.Prop.Name}"));

        return SqlResource.TryInsertWithUniqueKeyTemplate
            .Replace("#TABLE_NAME", meta.TableName)
            .Replace("#COLUMNS", cols)
            .Replace("#PARAMETER_NAMES", pars)
            .Replace("#UNIQUE_COLUMNS", uniqueCols)
            .Replace("#UNIQUE_WHERE", uniqueWhere)
            .Replace("#PRIMARY_KEY", meta.PrimaryKeyCol);
    }

    private static string CreateUpdateSql([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type type)
    {
        EntityMetadata meta = GetMetadata(type);
        string updates = string.Join(",\r\n\t", meta.AllColumns
            .Where(c => c.Prop.GetCustomAttribute<PrimaryKeyColumnAttribute>() is null)
            .Select(c => $"{c.ColName} = @{c.Prop.Name}"));

        return SqlResource.UpdateTemplate
            .Replace("#TABLE_NAME", meta.TableName)
            .Replace("#VALUES_TO_UPDATE", updates);
    }

    private static string CreateUpdateByPrimaryKeySql([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type type)
    {
        EntityMetadata meta = GetMetadata(type);
        return CreateUpdateSql(type)
            .Replace("#WHERE", $"WHERE {meta.PrimaryKeyCol} = @{meta.PrimaryKey.Name};");
    }

    private static string CreateDeleteSql([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type type)
    {
        EntityMetadata meta = GetMetadata(type);
        return SqlResource.DeleteTemplate
            .Replace("#TABLE_NAME", meta.TableName)
            .Replace("#TABLE_ALIAS", meta.Alias);
    }

    private static string CreateDeleteByPrimaryKeySql([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type type)
    {
        EntityMetadata meta = GetMetadata(type);
        return CreateDeleteSql(type)
            .Replace("#WHERE", $"WHERE {meta.Alias}.{meta.PrimaryKeyCol} = @{meta.PrimaryKey.Name};");
    }

    /// <summary>
    /// Builds the DO UPDATE SET clause for ON CONFLICT.
    /// Updates payload columns (non-PK, non-unique); falls back to unique columns if no payload exists.
    /// </summary>
    private static string BuildUniqueUpdates(EntityMetadata meta)
    {
        HashSet<string> uniquePropNames = [.. meta.UniqueColumns.Select(c => c.Prop.Name)];
        List<(PropertyInfo Prop, string ColName)> payloadCols =
        [
            .. meta.InsertColumns.Where(c => !uniquePropNames.Contains(c.Prop.Name))
        ];

        return payloadCols.Count > 0
            ? string.Join(", ", payloadCols.Select(c => $"{c.ColName} = EXCLUDED.{c.ColName}"))
            : string.Join(", ", meta.UniqueColumns.Select(c => $"{c.ColName} = EXCLUDED.{c.ColName}"));
    }

    #endregion
}
