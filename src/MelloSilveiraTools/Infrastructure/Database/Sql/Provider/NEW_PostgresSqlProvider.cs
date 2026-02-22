using MelloSilveiraTools.ExtensionMethods;
using MelloSilveiraTools.Infrastructure.Database.Attributes;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace MelloSilveiraTools.Infrastructure.Database.Sql.Provider;

public class NEW_PostgresSqlProvider : ISqlProvider
{
    // Cache de metadados para evitar reflexão repetitiva (Otimização de Performance)
    private static readonly ConcurrentDictionary<Type, EntityMetadata> _metadataCache = [];

    // Unificação dos caches de SQL para reduzir alocações de dicionários
    private readonly ConcurrentDictionary<(Type Type, Operation Operation, int BatchSize), Lazy<string>> _sqlCache = [];

    private enum Operation
    {
        BulkInsert,
        Insert,
        Count,
        Delete,
        DeletePk,
        ExistPk,
        Select,
        SelectDistinct,
        SelectPk,
        Update,
        UpdatePk
    };

    private record EntityMetadata(
        string TableName,
        string Alias,
        PropertyInfo PrimaryKey,
        string PrimaryKeyCol,
        List<(PropertyInfo Prop, string ColName)> AllColumns,
        List<(PropertyInfo Prop, string ColName)> UniqueColumns);

    #region ISqlProvider Implementation

    public string GetBulkInsertSql<T>(int batchSize) => GetSql<T>(Operation.BulkInsert, t => CreateBatchInsertSql(t, batchSize), batchSize);
    public string GetCountSql<T>() => GetSql<T>(Operation.Count, CreateCountSql);
    public string GetDeleteSql<T>() => GetSql<T>(Operation.Delete, CreateDeleteSql);
    public string GetDeleteByPrimaryKeySql<T>() => GetSql<T>(Operation.DeletePk, CreateDeleteByPrimaryKeySql);
    public string GetExistByPrimaryKeySql<T>() => GetSql<T>(Operation.ExistPk, CreateExistByPrimaryKeySql);
    public string GetInsertSql<T>() => GetSql<T>(Operation.Insert, CreateInsertSql);
    public string GetSelectSql<T>() => GetSql<T>(Operation.Select, CreateSelectSql);
    public string GetSelectDistinctSql<T>() => GetSql<T>(Operation.SelectDistinct, CreateDistinctSelectSql);
    public string GetSelectByPrimaryKeySql<T>() => GetSql<T>(Operation.SelectPk, CreateSelectByPrimaryKeySql);
    public string GetUpdateSql<T>() => GetSql<T>(Operation.Update, CreateUpdateSql);
    public string GetUpdateByPrimaryKeySql<T>() => GetSql<T>(Operation.UpdatePk, CreateUpdateByPrimaryKeySql);

    #endregion

    #region Core Logic

    private string GetSql<T>(Operation operation, Func<Type, string> factory, int batchSize = 0)
    {
        return _sqlCache.GetOrAdd(
            (typeof(T), operation, batchSize),
            key => new Lazy<string>(() => factory(key.Type), isThreadSafe: true)).Value;
    }

    private static EntityMetadata GetMetadata(Type type)
    {
        return _metadataCache.GetOrAdd(type, t =>
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
    }

    #endregion

    #region SQL Factories

    private static string CreateBatchInsertSql(Type type, int batchSize)
    {
        var meta = GetMetadata(type);
        var cols = string.Join(", ", meta.AllColumns.Select(c => c.ColName));

        // placeholders: (@Prop1, @Prop2), (@Prop1_1, @Prop2_1)...
        var valueLines = new StringBuilder();
        for (int i = 0; i < batchSize; i++)
        {
            var suffix = i == 0 ? "" : $"_{i}";
            valueLines.Append('(')
                      .Append(string.Join(", ", meta.AllColumns.Select(c => $"@{c.Prop.Name}{suffix}")))
                      .Append(')');
            if (i < batchSize - 1) valueLines.Append(",\r\n\t");
        }

        var sql = (meta.UniqueColumns.Count != 0 ? SqlResource.InsertWithUniqueKeyTemplate : SqlResource.BulkInsertTemplate)
            .Replace("#TABLE_NAME", meta.TableName)
            .Replace("#COLUMNS", cols)
            .Replace("#VALUES", valueLines.ToString())
            .Replace("#PRIMARY_KEY", meta.PrimaryKeyCol);

        if (meta.UniqueColumns.Count != 0)
        {
            sql = sql.Replace("#UNIQUE_KEYS", string.Join(", ", meta.UniqueColumns.Select(c => c.ColName)))
                     .Replace("#UNIQUE_KEY_FILTERS", string.Join(" AND ", meta.UniqueColumns.Select(c => $"{meta.Alias}.{c.ColName} = ANY(@{c.Prop.Name})")));
        }

        return sql;
    }

    private static string CreateBaseSelectSql(Type type, bool isDistinct = false)
    {
        var meta = GetMetadata(type);
        var columns = meta.AllColumns.Select(c => $"{meta.Alias}.{c.ColName} AS \"{c.Prop.Name}\"").ToList();

        // Lógica simplificada de Join usando reflexão apenas se houver ForeignKey
        var joins = new StringBuilder();
        foreach (var (prop, colName) in meta.AllColumns)
        {
            var fk = prop.GetCustomAttribute<ForeignKeyColumnAttribute>();
            if (fk == null) 
                continue;

            EntityMetadata refMeta = GetMetadata(fk.ReferencedTableType);
            joins.AppendLine($"{fk.JoinType} JOIN {refMeta.TableName} AS {refMeta.Alias} ON {refMeta.Alias}.{refMeta.PrimaryKeyCol} = {meta.Alias}.{colName}");
            columns.AddRange(refMeta.AllColumns.Select(c => $"{refMeta.Alias}.{c.ColName} AS \"{c.Prop.Name}\""));
        }

        return SqlResource.SelectTemplate
            .Replace(isDistinct ? "SELECT" : "SELECT DISTINCT", "SELECT" + (isDistinct ? " DISTINCT" : ""))
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

    private static string CreateInsertSql(Type type)
    {
        var meta = GetMetadata(type);
        var cols = string.Join(", ", meta.AllColumns.Select(c => c.ColName));
        var pars = string.Join(", ", meta.AllColumns.Select(c => $"@{c.Prop.Name}"));

        string sql = (meta.UniqueColumns.Count != 0 ? SqlResource.InsertWithUniqueKeyTemplate : SqlResource.InsertTemplate)
            .Replace("#TABLE_NAME", meta.TableName)
            .Replace("#COLUMNS", cols)
            .Replace("#PARAMETER_NAMES", pars)
            .Replace("#PRIMARY_KEY", meta.PrimaryKeyCol);

        if (meta.UniqueColumns.Count != 0)
        {
            sql = sql.Replace("#TABLE_ALIAS", meta.Alias)
                     .Replace("#UNIQUE_KEYS", string.Join(", ", meta.UniqueColumns.Select(c => c.ColName)))
                     .Replace("#UNIQUE_KEY_FILTERS", string.Join(" AND ", meta.UniqueColumns.Select(c => $"{meta.Alias}.{c.ColName} = @{c.Prop.Name}")));
        }
        return sql;
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

    private static string CreateCountSql(Type type) => CreateBaseSelectSql(type)
        .Replace("#COLUMNS", "COUNT(1)")
        .Remove("#ORDERBY").Remove("#OFFSET").Remove("#LIMIT");

    private static string CreateExistByPrimaryKeySql(Type type)
    {
        var meta = GetMetadata(type);
        return CreateBaseSelectSql(type)
            .Replace("#COLUMNS", "1")
            .Replace("#WHERE", $"WHERE {meta.Alias}.{meta.PrimaryKeyCol} = @{meta.PrimaryKey.Name}")
            .Replace("#LIMIT", "LIMIT 1")
            .Remove("#ORDERBY").Remove("#OFFSET");
    }

    #endregion
}