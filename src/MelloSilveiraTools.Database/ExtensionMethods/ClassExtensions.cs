using Dapper;
using MelloSilveiraTools.Core.ExtensionMethods;
using MelloSilveiraTools.Database.RelationalDatabase.Attributes;
using MelloSilveiraTools.Database.RelationalDatabase.Models;
using Npgsql;
using NpgsqlTypes;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace MelloSilveiraTools.Database.ExtensionMethods;

/// <summary>
/// Contains extension methods for class.
/// </summary>
public static class ClassExtensions
{
    // (EntityType, declaredOnly) → (PropertyInfo, NpgsqlDbType)[] for [Column]-annotated props
    private static readonly ConcurrentDictionary<(Type Type, bool DeclaredOnly), (PropertyInfo Prop, NpgsqlDbType DbType)[]> _columnMetaCache = new();

    // FilterType → (PropertyInfo, FilterColumnAttribute)[] for [FilterColumn]-annotated props
    private static readonly ConcurrentDictionary<Type, (PropertyInfo Prop, FilterColumnAttribute Attr)[]> _filterColumnMetaCache = new();

    // FilterType → FilterAttribute? (class-level attribute, cached to avoid repeated GetCustomAttribute)
    private static readonly ConcurrentDictionary<Type, FilterAttribute?> _filterAttributeCache = new();

    /// <summary>
    /// Contains extension methods for class instances.
    /// </summary>
    /// <typeparam name="T">The type of <paramref name="obj"/>.</typeparam>
    /// <param name="obj"></param>
    extension<T>(T obj)
    {
        /// <summary>
        /// Builds an enumerable with <see cref="NpgsqlParameter"/>.
        /// </summary>
        /// <param name="useDeclaredProperties"></param>
        /// <returns>A <see cref="IEnumerable{T}"/> with <see cref="NpgsqlParameter"/>.</returns>
        public IEnumerable<NpgsqlParameter> BuildParameters(bool useDeclaredProperties = false)
        {
            if (obj is null)
                yield break;

            foreach ((PropertyInfo? prop, NpgsqlDbType dbType) in GetColumnMeta<T>(useDeclaredProperties))
                yield return new NpgsqlParameter(prop.Name, dbType) { Value = prop.GetCustomValue(obj) ?? DBNull.Value };
        }

        /// <summary>
        /// Builds a SQL WHERE clause and a <see cref="DynamicParameters"/> based on filter.
        /// </summary>
        /// <returns></returns>
        public (string? SqlWhereClause, DynamicParameters? Parameters) BuildWhereClauseAndParameters()
        {
            DynamicParameters parameters = new();
            string? whereClause = BuildWhereClauseCore(obj, (name, _, value) => parameters.Add(name, value));
            return whereClause is null ? (null, null) : (whereClause, parameters);
        }

        /// <summary>
        /// Builds a SQL WHERE clause and a list of <see cref="NpgsqlParameter"/> based on filter.
        /// </summary>
        /// <returns></returns>
        public (string? SqlWhereClause, List<NpgsqlParameter> Parameters) BuildWhereClauseAndNpgsqlParameters()
        {
            List<NpgsqlParameter> parameters = [];
            string? whereClause = BuildWhereClauseCore(obj, (name, type, value) => parameters.Add(new NpgsqlParameter(name, type.GetDbTypeFromPropertyType()) { Value = value }));
            return (whereClause, parameters);
        }
    }

    extension(object obj)
    {
        /// <summary>
        /// Sets the values in objects using reflection.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="valuesGroupedByPropertyName"></param>
        public void SetValues<T>([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type type,
            IDictionary<string, T> valuesGroupedByPropertyName)
        {
            foreach (KeyValuePair<string, T> propertyNameAndValue in valuesGroupedByPropertyName)
            {
                PropertyInfo? property = type.GetProperty(propertyNameAndValue.Key);
                property?.SetValue(obj, propertyNameAndValue.Value);
            }
        }
    }

    extension<T>(ICollection<T> collection)
    {
        /// <summary>
        /// Builds an enumerable with <see cref="NpgsqlParameter"/>.
        /// </summary>
        /// <returns>A <see cref="IEnumerable{T}"/> with <see cref="NpgsqlParameter"/>.</returns>
        public IEnumerable<NpgsqlParameter> BuildParametersFromCollection()
        {
            if (collection.IsNullOrEmpty())
                yield break;

            int index = 0;
            (PropertyInfo Prop, NpgsqlDbType DbType)[] colMeta = GetColumnMeta<T>(declaredOnly: false);
            foreach (T obj in collection)
            {
                index++;
                foreach ((PropertyInfo? prop, NpgsqlDbType dbType) in colMeta)
                    yield return new NpgsqlParameter($"{prop.Name}_{index}", dbType) { Value = prop.GetCustomValue(obj) ?? DBNull.Value };
            }
        }
    }

    extension(PropertyInfo property)
    {
        /// <summary>
        /// Gets the value of the property for the specified object, converting it to an <see cref="int"/> if it is an <see cref="Enum"/>.
        /// </summary>
        /// <param name="obj">The object whose property value will be returned.</param>
        /// <returns>The property value, or its integer representation if the value is an <see cref="Enum"/>.</returns>
        public object? GetCustomValue(object? obj)
        {
            object? val = property.GetValue(obj);
            return val is Enum ? (int)val : val;
        }
    }

    internal static (PropertyInfo Prop, NpgsqlDbType DbType)[] GetColumnMeta<T>(bool declaredOnly)
        => _columnMetaCache.GetOrAdd(
            (typeof(T), declaredOnly),
            static key =>
            {
                PropertyInfo[] props = key.DeclaredOnly
                    ? key.Type.GetDeclaredProperties<ColumnAttribute>()
                    : key.Type.GetPropertiesInHierarchy<ColumnAttribute>();
                return [.. props.Select(p => (p, p.PropertyType.GetDbTypeFromPropertyType()))];
            });

    private static (PropertyInfo Prop, FilterColumnAttribute Attr)[] GetFilterColumnMeta(Type type)
        => _filterColumnMetaCache.GetOrAdd(
            type,
            static t => [.. t.GetPropertiesInHierarchy<FilterColumnAttribute>().Select(p => (p, p.GetCustomAttribute<FilterColumnAttribute>()!))]);

    private static string? BuildWhereClauseCore<T>(T obj, Action<string, Type, object> addParam)
    {
        if (obj is null) return null;

        Type type = typeof(T);
        FilterAttribute? filterAttribute = _filterAttributeCache.GetOrAdd(type, static t => t.GetCustomAttribute<FilterAttribute>());
        if (filterAttribute is null) return null;

        List<string> whereClauses = [];

        foreach ((PropertyInfo? property, FilterColumnAttribute? filterColumnAttribute) in GetFilterColumnMeta(type))
        {
            object? propertyValue = property.GetCustomValue(obj);
            if (propertyValue is null || propertyValue is string str && string.IsNullOrWhiteSpace(str))
                continue;

            string tableAlias = filterColumnAttribute.TableName is null
                ? filterAttribute.TableDefinition!.Alias
                : filterAttribute.JoinTablesDefinition[filterColumnAttribute.TableName].Alias;

            string columnName = (filterColumnAttribute.PropertyName ?? property.Name).ToSnakeCase();
            whereClauses.Add($"{tableAlias}.{columnName} {filterColumnAttribute.FilterClause} @{property.Name}");

            if (filterColumnAttribute.FilterClause is FilterClause.Like or FilterClause.ILike)
                propertyValue = $"%{propertyValue}%";
            
            addParam(property.Name, property.PropertyType, propertyValue);
        }

        return whereClauses.IsNullOrEmpty() ? null : $"WHERE {string.Join("\r\n\tAND ", whereClauses)}";
    }
}
