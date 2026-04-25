using Dapper;
using MelloSilveiraTools.Core.ExtensionMethods;
using MelloSilveiraTools.Database.Infrastructure.Database.Attributes;
using MelloSilveiraTools.Database.Infrastructure.Database.Models;
using Npgsql;
using NpgsqlTypes;
using System.Collections.Concurrent;
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
    /// Gets the values from object which is following the hierarchy order from parent to child.
    /// </summary>
    /// <typeparam name="T">The type of <paramref name="obj"/>.</typeparam>
    /// <param name="obj"></param>
    /// <returns>A <see cref="List{T}"/> containing the values from object which is following the hierarchy order from parent to child.</returns>
    public static IEnumerable<object?> GetValuesInHierarchy<T>(this T obj)
    {
        PropertyInfo[] properties = typeof(T).GetPropertiesInHierarchy();
        return obj.GetValues(properties);
    }

    /// <summary>
    /// Gets the values from object using an <see cref="IEnumerable{T}"/> of properties as reference.
    /// </summary>
    /// <typeparam name="T">The type of <paramref name="obj"/>.</typeparam>
    /// <param name="obj"></param>
    /// <param name="properties">Properties to be used as reference to get the values from object.</param>
    /// <returns></returns>
    public static IEnumerable<object?> GetValues<T>(this T obj, IEnumerable<PropertyInfo> properties)
    {
        return properties.Select(property => property.GetValue(obj));
    }

    /// <summary>
    /// Sets the values in objects using reflection.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="obj"></param>
    /// <param name="type"></param>
    /// <param name="valuesGroupedByPropertyName"></param>
    public static void SetValues<T>(this object obj, Type type, IDictionary<string, T> valuesGroupedByPropertyName)
    {
        foreach (KeyValuePair<string, T> propertyNameAndValue in valuesGroupedByPropertyName)
        {
            PropertyInfo? property = type.GetProperty(propertyNameAndValue.Key);
            property?.SetValue(obj, propertyNameAndValue.Value);
        }
    }

    /// <summary>
    /// Gets the name and value of properties from object which is following the hierarchy order from parent to child.
    /// It is also possible to filter by a custom attribute.
    /// </summary>
    /// <typeparam name="T">The type of <paramref name="obj"/>.</typeparam>
    /// <typeparam name="TCustomAttribute">The type of custom attribute to be used in search.</typeparam>
    /// <param name="obj"></param>
    /// <returns>
    /// A <see cref="Dictionary{TKey, TValue}"/> which the key is the property name and the value is the property value.
    /// </returns>
    public static Dictionary<string, object?>? GetPropertyNamesAndValuesInHierarchy<T, TCustomAttribute>(this T obj) where TCustomAttribute : Attribute
    {
        if (obj is null)
            return null;

        Dictionary<string, object?> result = [];
        foreach (PropertyInfo property in typeof(T).GetPropertiesInHierarchy<TCustomAttribute>())
            result.Add(property.Name, property.GetValue(obj));

        return result;
    }

    /// <summary>
    /// Builds an enumerable with <see cref="NpgsqlParameter"/>.
    /// </summary>
    /// <typeparam name="T">The type of <paramref name="collection"/>.</typeparam>
    /// <param name="collection"></param>
    /// <returns>A <see cref="IEnumerable{T}"/> with <see cref="NpgsqlParameter"/>.</returns>
    public static IEnumerable<NpgsqlParameter> BuildParametersFromCollection<T>(this ICollection<T> collection)
    {
        if (collection.IsNullOrEmpty())
            yield break;

        var colMeta = GetColumnMeta<T>(declaredOnly: false);
        int index = 0;
        foreach (T obj in collection)
        {
            index++;
            foreach (var (prop, dbType) in colMeta)
                yield return new NpgsqlParameter($"{prop.Name}_{index}", dbType) { Value = prop.GetValue(obj) ?? DBNull.Value };
        }
    }

    /// <summary>
    /// Builds an enumerable with <see cref="NpgsqlParameter"/>.
    /// </summary>
    /// <typeparam name="T">The type of <paramref name="obj"/>.</typeparam>
    /// <param name="obj"></param>
    /// <param name="useDeclaredProperties"></param>
    /// <returns>A <see cref="IEnumerable{T}"/> with <see cref="NpgsqlParameter"/>.</returns>
    public static IEnumerable<NpgsqlParameter> BuildParameters<T>(this T obj, bool useDeclaredProperties = false)
    {
        if (obj is null)
            yield break;

        foreach (var (prop, dbType) in GetColumnMeta<T>(useDeclaredProperties))
            yield return new NpgsqlParameter(prop.Name, dbType) { Value = prop.GetValue(obj) ?? DBNull.Value };
    }

    /// <summary>
    /// Builds a SQL WHERE clause and a <see cref="DynamicParameters"/> based on filter.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="obj"></param>
    /// <returns></returns>
    public static (string? SqlWhereClause, DynamicParameters? Parameters) BuildWhereClauseAndParameters<T>(this T obj)
    {
        DynamicParameters parameters = new();
        string? whereClause = BuildWhereClauseCore(obj, (name, _, value) => parameters.Add(name, value));
        return whereClause is null ? (null, null) : (whereClause, parameters);
    }

    /// <summary>
    /// Builds a SQL WHERE clause and a list of <see cref="NpgsqlParameter"/> based on filter.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="obj"></param>
    /// <returns></returns>
    public static (string? SqlWhereClause, List<NpgsqlParameter> Parameters) BuildWhereClauseAndNpgsqlParameters<T>(this T obj)
    {
        List<NpgsqlParameter> parameters = [];
        string? whereClause = BuildWhereClauseCore(obj,
            (name, type, value) => parameters.Add(new NpgsqlParameter(name, type.GetDbTypeFromPropertyType()) { Value = value }));
        return (whereClause, parameters);
    }

    /// <summary>
    /// Wraps the supplied instance in a completed <see cref="Task{T}"/>.
    /// </summary>
    public static Task<T> AsTask<T>(this T obj) => Task.FromResult(obj);

    private static (PropertyInfo Prop, NpgsqlDbType DbType)[] GetColumnMeta<T>(bool declaredOnly)
        => _columnMetaCache.GetOrAdd(
            (typeof(T), declaredOnly),
            static key =>
            {
                PropertyInfo[] props = key.DeclaredOnly
                    ? key.Type.GetDeclaredProperties<ColumnAttribute>()
                    : key.Type.GetPropertiesInHierarchy<ColumnAttribute>();
                return props.Select(p => (p, p.PropertyType.GetDbTypeFromPropertyType())).ToArray();
            });

    private static (PropertyInfo Prop, FilterColumnAttribute Attr)[] GetFilterColumnMeta(Type type)
        => _filterColumnMetaCache.GetOrAdd(
            type,
            static t => t.GetPropertiesInHierarchy<FilterColumnAttribute>()
                         .Select(p => (p, p.GetCustomAttribute<FilterColumnAttribute>()!))
                         .ToArray());

    private static string? BuildWhereClauseCore<T>(T obj, Action<string, Type, object> addParam)
    {
        if (obj is null) return null;

        Type type = typeof(T);
        FilterAttribute? filterAttribute = _filterAttributeCache.GetOrAdd(type, static t => t.GetCustomAttribute<FilterAttribute>());
        if (filterAttribute is null) return null;

        List<string> whereClauses = [];

        foreach (var (property, filterColumnAttribute) in GetFilterColumnMeta(type))
        {
            object? propertyValue = property.GetValue(obj);
            if (propertyValue is null || propertyValue is string str && string.IsNullOrWhiteSpace(str))
                continue;

            string tableAlias = filterColumnAttribute.TableName is null
                ? filterAttribute.TableDefinition!.Alias
                : filterAttribute.JoinTablesDefinition[filterColumnAttribute.TableName].Alias;

            string columnName = (filterColumnAttribute.PropertyName ?? property.Name).ToSnakeCase();
            whereClauses.Add($"{tableAlias}.{columnName} {filterColumnAttribute.FilterClause} @{property.Name}");

            if (propertyValue is Enum)
                propertyValue = (int)propertyValue;

            if (filterColumnAttribute.FilterClause is FilterClause.Like or FilterClause.ILike)
                propertyValue = $"%{propertyValue}%";

            addParam(property.Name, property.PropertyType, propertyValue);
        }

        return whereClauses.IsNullOrEmpty() ? null : $"WHERE {string.Join("\r\n\tAND ", whereClauses)}";
    }
}
