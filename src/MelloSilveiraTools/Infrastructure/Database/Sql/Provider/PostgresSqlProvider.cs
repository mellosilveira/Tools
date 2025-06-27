using MelloSilveiraTools.ExtensionMethods;
using MelloSilveiraTools.Infrastructure.Database.Attributes;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.Reflection;

namespace MelloSilveiraTools.Infrastructure.Database.Sql.Provider;

/// <inheritdoc cref="ISqlProvider"/>
public class PostgresSqlProvider : ISqlProvider
{
    private readonly Dictionary<string, string> _deleteQueries = [];
    private readonly Dictionary<string, string> _deleteByPrimaryKeyQueries = [];
    private readonly Dictionary<string, string> _insertQueries = [];
    private readonly Dictionary<string, string> _selectQueries = [];
    private readonly Dictionary<string, string> _selectByPrimaryKeyQueries = [];
    private readonly Dictionary<string, string> _updateQueries = [];
    private readonly Dictionary<string, string> _updateByPrimaryKeyQueries = [];
    private readonly Dictionary<string, string> _batchInsertQueries = [];

    /// <inheritdoc/>
    public string GetDeleteQuery<T>() => GetQuery<T>(_deleteQueries, CreateDeleteQuery);

    /// <inheritdoc/>
    public string GetDeleteByPrimaryKeyQuery<T>() => GetQuery<T>(_deleteByPrimaryKeyQueries, CreateDeleteByPrimaryKeyQuery);

    /// <inheritdoc/>
    public string GetInsertQuery<T>() => GetQuery<T>(_insertQueries, CreateInsertQuery);

    /// <inheritdoc/>
    public string GetBatchInsertQuery<T>(int batchSize) => GetBatchQuery<T>(_batchInsertQueries, CreateBatchInsertQuery, batchSize);

    /// <inheritdoc/>
    public string GetSelectQuery<T>() => GetQuery<T>(_selectQueries, CreateSelectQuery);

    /// <inheritdoc/>
    public string GetSelectByPrimaryKeyQuery<T>() => GetQuery<T>(_selectByPrimaryKeyQueries, CreateSelectByPrimaryKeyQuery);

    /// <inheritdoc/>
    public string GetUpdateQuery<T>() => GetQuery<T>(_updateQueries, CreateUpdateQuery);

    /// <inheritdoc/>
    public string GetUpdateByPrimaryKeyQuery<T>() => GetQuery<T>(_updateByPrimaryKeyQueries, CreateUpdateByPrimaryKeyQuery);

    private static string GetBatchQuery<T>(Dictionary<string, string> queriesDictionary, Func<Type, int, string> createQueryMethod, int batchSize)
    {
        Type type = typeof(T);
        string fullTypeName = type.FullName!;

        if (queriesDictionary.TryGetValue(fullTypeName, out var storedQuery))
            return storedQuery;

        string query = createQueryMethod(type, batchSize);
        queriesDictionary[fullTypeName] = query;
        return query;
    }

    private static string GetQuery<T>(Dictionary<string, string> queriesDictionary, Func<Type, string> createQueryMethod)
    {
        Type type = typeof(T);
        string fullTypeName = type.FullName!;

        if (queriesDictionary.TryGetValue(fullTypeName, out var storedQuery))
            return storedQuery;

        string query = createQueryMethod(type);
        queriesDictionary[fullTypeName] = query;
        return query;
    }

    private static string CreateSelectQuery(Type type)
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
            .Replace("#COLUMNS", string.Join("\r\n\t,", columnsToSelect))
            .Replace("#TABLE_NAME", tableName)
            .Replace("#TABLE_ALIAS", tableAlias)
            .Replace("#JOIN", joins.IsEmpty() ? null : string.Join("\r\n", joins));
    }

    private static string CreateSelectByPrimaryKeyQuery(Type type)
    {
        TableAttribute tableAttribute = type.GetCustomAttribute<TableAttribute>()!;
        PropertyInfo[] columnProperties = type.GetPropertiesInHierarchy<ColumnAttribute>();
        PropertyInfo primaryKeyProperty = columnProperties.Single(p => p.GetCustomAttribute<PrimaryKeyColumnAttribute>() != null);

        string primaryKeyPropertyName = primaryKeyProperty.Name;

        return CreateSelectQuery(type)
            .Replace("#WHERE", $"WHERE {tableAttribute.Alias}.{primaryKeyPropertyName.ToSnakeCase()} = @{primaryKeyPropertyName}")
            .Replace("#ORDERBY", null)
            .Replace("#OFFSET", null)
            .Replace("#LIMIT", null);
    }

    private static string CreateInsertQuery(Type type)
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

    private static string CreateBatchInsertQuery(Type type, int batchSize)
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

    private static string CreateUpdateQuery(TableAttribute tableAttribute, PropertyInfo[] columnProperties)
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

    private static string CreateUpdateQuery(Type type)
    {
        TableAttribute tableAttribute = type.GetCustomAttribute<TableAttribute>()!;
        PropertyInfo[] columnProperties = type.GetPropertiesInHierarchy<ColumnAttribute>();
        return CreateUpdateQuery(tableAttribute, columnProperties);
    }

    private static string CreateUpdateByPrimaryKeyQuery(Type type)
    {
        TableAttribute tableAttribute = type.GetCustomAttribute<TableAttribute>()!;
        PropertyInfo[] columnProperties = type.GetPropertiesInHierarchy<ColumnAttribute>();
        PropertyInfo primaryKeyProperty = columnProperties.Single(p => p.GetCustomAttribute<PrimaryKeyColumnAttribute>() != null);
        string primaryKeyPropertyName = primaryKeyProperty.Name;

        return CreateUpdateQuery(tableAttribute, columnProperties)
            .Replace("#WHERE", $"WHERE {primaryKeyPropertyName.ToSnakeCase()} = @{primaryKeyPropertyName};");
    }

    private static string CreateDeleteQuery(TableAttribute tableAttribute)
    {
        return SqlResource.DeleteTemplate
            .Replace("#TABLE_NAME", tableAttribute.Name)
            .Replace("#TABLE_ALIAS", tableAttribute.Alias);
    }

    private static string CreateDeleteQuery(Type type)
    {
        TableAttribute tableAttribute = type.GetCustomAttribute<TableAttribute>()!;
        return CreateDeleteQuery(tableAttribute);
    }

    private static string CreateDeleteByPrimaryKeyQuery(Type type)
    {
        TableAttribute tableAttribute = type.GetCustomAttribute<TableAttribute>()!;
        string primaryKeyPropertyName = type.GetProperties().Single(p => p.GetCustomAttribute<PrimaryKeyColumnAttribute>() != null).Name;
        return CreateDeleteQuery(tableAttribute)
            .Replace("#WHERE", $"WHERE {tableAttribute.Alias}.{primaryKeyPropertyName.ToSnakeCase()} = @{primaryKeyPropertyName};");
    }
}
