using MelloSilveiraTools.ExtensionMethods;
using MelloSilveiraTools.Infrastructure.Database.Attributes;
using System.Collections.Concurrent;
using System.Reflection;

namespace MelloSilveiraTools.Infrastructure.Database.Sql.Provider;

/// <inheritdoc cref="ISqlProvider"/>
public class PostgresSqlProvider : ISqlProvider
{
    private readonly ConcurrentDictionary<(Type Type, int BatchSize), Lazy<string>> _bulkInsertSqls = [];
    private readonly ConcurrentDictionary<Type, Lazy<string>> _countSqls = [];
    private readonly ConcurrentDictionary<Type, Lazy<string>> _deleteSqls = [];
    private readonly ConcurrentDictionary<Type, Lazy<string>> _deleteByPrimaryKeySqls = [];
    private readonly ConcurrentDictionary<Type, Lazy<string>> _existSqls = [];
    private readonly ConcurrentDictionary<Type, Lazy<string>> _insertSqls = [];
    private readonly ConcurrentDictionary<Type, Lazy<string>> _selectSqls = [];
    private readonly ConcurrentDictionary<Type, Lazy<string>> _selectByPrimaryKeySqls = [];
    private readonly ConcurrentDictionary<Type, Lazy<string>> _updateSqls = [];
    private readonly ConcurrentDictionary<Type, Lazy<string>> _updateByPrimaryKeySqls = [];

    /// <inheritdoc/>
    public string GetBulkInsertSql<T>(int batchSize) => GetBulkSql<T>(_bulkInsertSqls, CreateBatchInsertSql, batchSize);

    /// <inheritdoc/>
    public string GetCountSql<T>() => GetSql<T>(_countSqls, CreateCountSql);

    /// <inheritdoc/>
    public string GetDeleteSql<T>() => GetSql<T>(_deleteSqls, CreateDeleteSql);

    /// <inheritdoc/>
    public string GetDeleteByPrimaryKeySql<T>() => GetSql<T>(_deleteByPrimaryKeySqls, CreateDeleteByPrimaryKeySql);

    /// <inheritdoc/>
    public string GetExistByPrimaryKeySql<T>() => GetSql<T>(_existSqls, CreateExistByPrimaryKeySql);

    /// <inheritdoc/>
    public string GetInsertSql<T>() => GetSql<T>(_insertSqls, CreateInsertSql);

    /// <inheritdoc/>
    public string GetSelectSql<T>() => GetSql<T>(_selectSqls, CreateSelectSql);

    /// <inheritdoc/>
    public string GetSelectByPrimaryKeySql<T>() => GetSql<T>(_selectByPrimaryKeySqls, CreateSelectByPrimaryKeySql);

    /// <inheritdoc/>
    public string GetUpdateSql<T>() => GetSql<T>(_updateSqls, CreateUpdateSql);

    /// <inheritdoc/>
    public string GetUpdateByPrimaryKeySql<T>() => GetSql<T>(_updateByPrimaryKeySqls, CreateUpdateByPrimaryKeySql);

    private static string GetBulkSql<T>(ConcurrentDictionary<(Type Type, int BatchSize), Lazy<string>> cache, Func<Type, int, string> createSqlMethod, int batchSize)
    {
        Lazy<string> lazy = cache.GetOrAdd(
            (typeof(T), batchSize),
            k => new Lazy<string>(() => createSqlMethod(k.Type, k.BatchSize)));

        return lazy.Value;
    }

    private static string GetSql<T>(ConcurrentDictionary<Type, Lazy<string>> cache, Func<Type, string> createSqlMethod)
    {
        Lazy<string> lazy = cache.GetOrAdd(
            typeof(T),
            t => new Lazy<string>(() => createSqlMethod(t), isThreadSafe: true));

        return lazy.Value;
    }

    private static string CreateCountSql(Type type) => CreateBaseSelectSql(type)
        .Replace("#COLUMNS", "COUNT(1)")
        .Remove("#ORDERBY")
        .Remove("#OFFSET")
        .Remove("#LIMIT");

    private static string CreateDeleteSql(TableAttribute tableAttribute)
    {
        return SqlResource.DeleteTemplate
            .Replace("#TABLE_NAME", tableAttribute.Name)
            .Replace("#TABLE_ALIAS", tableAttribute.Alias);
    }

    private static string CreateDeleteSql(Type type)
    {
        TableAttribute tableAttribute = type.GetCustomAttribute<TableAttribute>()!;
        return CreateDeleteSql(tableAttribute);
    }

    private static string CreateDeleteByPrimaryKeySql(Type type)
    {
        TableAttribute tableAttribute = type.GetCustomAttribute<TableAttribute>()!;
        string primaryKeyPropertyName = type.GetProperties().Single(p => p.GetCustomAttribute<PrimaryKeyColumnAttribute>() != null).Name;
        return CreateDeleteSql(tableAttribute)
            .Replace("#WHERE", $"WHERE {tableAttribute.Alias}.{primaryKeyPropertyName.ToSnakeCase()} = @{primaryKeyPropertyName};");
    }

    private static string CreateExistByPrimaryKeySql(Type type)
    {
        TableAttribute tableAttribute = type.GetCustomAttribute<TableAttribute>()!;
        PropertyInfo[] columnProperties = type.GetPropertiesInHierarchy<ColumnAttribute>();
        PropertyInfo primaryKeyProperty = columnProperties.Single(p => p.GetCustomAttribute<PrimaryKeyColumnAttribute>() != null);

        string primaryKeyPropertyName = primaryKeyProperty.Name;

        return CreateBaseSelectSql(type)
            .Replace("#COLUMNS", "1")
            .Replace("#WHERE", $"WHERE {tableAttribute.Alias}.{primaryKeyPropertyName.ToSnakeCase()} = @{primaryKeyPropertyName}")
            .Replace("#LIMIT", "LIMIT 1")
            .Remove("#ORDERBY")
            .Remove("#OFFSET");
    }

    private static string CreateSelectSql(Type type)
    {
        TableAttribute tableAttribute = type.GetCustomAttribute<TableAttribute>()!;
        PropertyInfo[] columnProperties = type.GetPropertiesInHierarchy<ColumnAttribute>();

        string tableName = tableAttribute.Name;
        string tableAlias = tableAttribute.Alias;

        List<string> columnsToSelect = [];
        List<string> joins = [];

        foreach (PropertyInfo columnProperty in columnProperties)
        {
            string propertyName = columnProperty.Name;
            string columnName = propertyName.ToSnakeCase();
            columnsToSelect.Add($"{tableAlias}.{columnName} AS \"{propertyName}\"");

            var foreignKeyAttribute = columnProperty.GetCustomAttribute<ForeignKeyColumnAttribute>();
            if (foreignKeyAttribute != null)
            {
                Type referencedTableType = foreignKeyAttribute.ReferencedTableType;
                TableAttribute referencedTableDefinitionAttribute = referencedTableType.GetCustomAttribute<TableAttribute>()!;
                string referencedTableName = referencedTableDefinitionAttribute.Name;
                string referencedTableAlias = referencedTableDefinitionAttribute.Alias;
                string referencedTablePrimaryKeyColumnName = referencedTableType
                    .GetPropertiesInHierarchy()
                    .Single(p => p.GetCustomAttribute<PrimaryKeyColumnAttribute>() != null)
                    .Name
                    .ToSnakeCase();

                joins.Add($"{foreignKeyAttribute.JoinType} JOIN {referencedTableDefinitionAttribute.Name} AS {referencedTableDefinitionAttribute.Alias} ON {referencedTableAlias}.{referencedTablePrimaryKeyColumnName} = {tableAlias}.{columnName}");
            }
        }

        return CreateBaseSelectSql(type).Replace("#COLUMNS", string.Join("\r\n\t,", columnsToSelect));
    }

    private static string CreateSelectByPrimaryKeySql(Type type)
    {
        TableAttribute tableAttribute = type.GetCustomAttribute<TableAttribute>()!;
        PropertyInfo[] columnProperties = type.GetPropertiesInHierarchy<ColumnAttribute>();
        PropertyInfo primaryKeyProperty = columnProperties.Single(p => p.GetCustomAttribute<PrimaryKeyColumnAttribute>() != null);

        string primaryKeyPropertyName = primaryKeyProperty.Name;

        return CreateSelectSql(type)
            .Replace("#WHERE", $"WHERE {tableAttribute.Alias}.{primaryKeyPropertyName.ToSnakeCase()} = @{primaryKeyPropertyName}")
            .Remove("#ORDERBY")
            .Remove("#OFFSET")
            .Remove("#LIMIT");
    }

    private static string CreateBaseSelectSql(Type type)
    {
        TableAttribute tableAttribute = type.GetCustomAttribute<TableAttribute>()!;
        PropertyInfo[] columnProperties = type.GetPropertiesInHierarchy<ColumnAttribute>();

        string tableName = tableAttribute.Name;
        string tableAlias = tableAttribute.Alias;

        List<string> columnsToSelect = [];
        List<string> joins = [];

        foreach (PropertyInfo columnProperty in columnProperties)
        {
            string propertyName = columnProperty.Name;
            string columnName = propertyName.ToSnakeCase();
            columnsToSelect.Add($"{tableAlias}.{columnName} AS \"{propertyName}\"");

            var foreignKeyAttribute = columnProperty.GetCustomAttribute<ForeignKeyColumnAttribute>();
            if (foreignKeyAttribute != null)
            {
                Type referencedTableType = foreignKeyAttribute.ReferencedTableType;
                TableAttribute referencedTableDefinitionAttribute = referencedTableType.GetCustomAttribute<TableAttribute>()!;
                string referencedTableName = referencedTableDefinitionAttribute.Name;
                string referencedTableAlias = referencedTableDefinitionAttribute.Alias;
                string referencedTablePrimaryKeyColumnName = referencedTableType
                    .GetPropertiesInHierarchy()
                    .Single(p => p.GetCustomAttribute<PrimaryKeyColumnAttribute>() != null)
                    .Name
                    .ToSnakeCase();

                joins.Add($"{foreignKeyAttribute.JoinType} JOIN {referencedTableDefinitionAttribute.Name} AS {referencedTableDefinitionAttribute.Alias} ON {referencedTableAlias}.{referencedTablePrimaryKeyColumnName} = {tableAlias}.{columnName}");
            }
        }

        return SqlResource.SelectTemplate
            .Replace("#TABLE_NAME", tableName)
            .Replace("#TABLE_ALIAS", tableAlias)
            .Replace("#JOIN", joins.IsEmpty() ? null : string.Join("\r\n", joins));
    }

    private static string CreateInsertSql(Type type)
    {
        TableAttribute tableAttribute = type.GetCustomAttribute<TableAttribute>()!;
        PropertyInfo[] columnProperties = type.GetPropertiesInHierarchy<ColumnAttribute>();
        PropertyInfo primaryKeyProperty = columnProperties.Single(p => p.GetCustomAttribute<PrimaryKeyColumnAttribute>() != null);

        List<string> columnsToInsert = [];
        List<string> parameterNames = [];

        foreach (PropertyInfo columnProperty in columnProperties)
        {
            string propertyName = columnProperty.Name;
            columnsToInsert.Add(propertyName.ToSnakeCase());
            parameterNames.Add($"@{propertyName}");
        }

        return SqlResource.InsertTemplate
            .Replace("#TABLE_NAME", tableAttribute.Name)
            .Replace("#COLUMNS", string.Join("\r\n\t,", columnsToInsert))
            .Replace("#PARAMETER_NAMES", string.Join("\r\n\t,", parameterNames))
            .Replace("#PRIMARY_KEY", primaryKeyProperty.Name.ToSnakeCase());
    }

    private static string CreateBatchInsertSql(Type type, int batchSize)
    {
        TableAttribute tableAttribute = type.GetCustomAttribute<TableAttribute>()!;
        PropertyInfo[] columnProperties = type.GetPropertiesInHierarchy<ColumnAttribute>();
        PropertyInfo primaryKeyProperty = columnProperties.Single(p => p.GetCustomAttribute<PrimaryKeyColumnAttribute>() != null);

        List<string> columnsToInsert = [];
        List<string> parameterNames = [];

        foreach (PropertyInfo columnProperty in columnProperties)
        {
            string propertyName = columnProperty.Name;
            columnsToInsert.Add(propertyName.ToSnakeCase());
            parameterNames.Add($"@{propertyName}");
        }

        IEnumerable<string> sqlValues = Enumerable.Repeat("(" + string.Join("\r\n\t,", parameterNames) + ")", batchSize);
        return SqlResource.InsertTemplate
            .Replace("#TABLE_NAME", tableAttribute.Name)
            .Replace("#COLUMNS", string.Join("\r\n\t,", columnsToInsert))
            .Replace("#PRIMARY_KEY", primaryKeyProperty.Name.ToSnakeCase())
            .Replace("#VALUES", string.Join(",\r\n\t", sqlValues));
    }

    private static string CreateUpdateSql(TableAttribute tableAttribute, PropertyInfo[] columnProperties)
    {
        List<string> columnsToUpdate = [];
        foreach (var columnProperty in columnProperties)
        {
            string propertyName = columnProperty.Name;
            columnsToUpdate.Add($"{propertyName.ToSnakeCase()} = @{propertyName}");
        }

        return SqlResource.UpdateTemplate
            .Replace("#TABLE_NAME", tableAttribute.Name)
            .Replace("#VALUES_TO_UPDATE", string.Join("\r\n\t,", columnsToUpdate));
    }

    private static string CreateUpdateSql(Type type)
    {
        TableAttribute tableAttribute = type.GetCustomAttribute<TableAttribute>()!;
        PropertyInfo[] columnProperties = type.GetPropertiesInHierarchy<ColumnAttribute>();
        return CreateUpdateSql(tableAttribute, columnProperties);
    }

    private static string CreateUpdateByPrimaryKeySql(Type type)
    {
        TableAttribute tableAttribute = type.GetCustomAttribute<TableAttribute>()!;
        PropertyInfo[] columnProperties = type.GetPropertiesInHierarchy<ColumnAttribute>();
        PropertyInfo primaryKeyProperty = columnProperties.Single(p => p.GetCustomAttribute<PrimaryKeyColumnAttribute>() != null);
        string primaryKeyPropertyName = primaryKeyProperty.Name;

        return CreateUpdateSql(tableAttribute, columnProperties)
            .Replace("#WHERE", $"WHERE {primaryKeyPropertyName.ToSnakeCase()} = @{primaryKeyPropertyName};");
    }
}
