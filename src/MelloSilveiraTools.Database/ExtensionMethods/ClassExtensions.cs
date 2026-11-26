using MelloSilveiraTools.Core.ExtensionMethods;
using MelloSilveiraTools.Database.RelationalDatabase.Attributes;
using MelloSilveiraTools.Database.RelationalDatabase.Models;
using MelloSilveiraTools.Database.RelationalDatabase.Models.Entities;
using Npgsql;
using NpgsqlTypes;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace MelloSilveiraTools.Database.ExtensionMethods;

/// <summary>
/// Contains Native AOT-compatible extension methods for mapping entities and filters to typed <see cref="NpgsqlParameter"/> collections.
/// </summary>
public static class ClassExtensions
{
    private static class ColumnMetaCache<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>
    {
        public static readonly (PropertyInfo Prop, NpgsqlDbType DbType)[] DeclaredOnly = BuildColumnMeta(typeof(T), declaredOnly: true);
        public static readonly (PropertyInfo Prop, NpgsqlDbType DbType)[] All = BuildColumnMeta(typeof(T), declaredOnly: false);
    }

    private static class FilterMetaCache<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>
    {
        public static readonly FilterAttribute? FilterAttribute = typeof(T).GetCustomAttribute<FilterAttribute>();
        public static readonly (PropertyInfo Prop, FilterColumnAttribute Attr)[] Columns = BuildFilterColumnMeta(typeof(T));
    }

    /// <summary>
    /// Contains extension methods for class instances.
    /// </summary>
    /// <typeparam name="T">The type of <paramref name="obj"/>.</typeparam>
    /// <param name="obj">Target entity or filter instance.</param>
    extension<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(T obj)
    {
        /// <summary>
        /// Builds a sequence of strongly-typed <see cref="NpgsqlParameter"/> instances from <c>[Column]</c>-annotated properties.
        /// </summary>
        /// <param name="useDeclaredProperties">When <see langword="true"/>, inspects only declared properties (plus <c>UserId</c> for user-scoped entities).</param>
        /// <returns>An <see cref="IEnumerable{T}"/> of <see cref="NpgsqlParameter"/> instances.</returns>
        public IEnumerable<NpgsqlParameter> BuildParameters(bool useDeclaredProperties = false)
        {
            if (obj is null)
                yield break;

            foreach ((PropertyInfo prop, NpgsqlDbType dbType) in GetColumnMeta<T>(useDeclaredProperties))
                yield return new NpgsqlParameter(prop.Name, dbType) { Value = prop.GetCustomValue(obj) ?? DBNull.Value };
        }

        /// <summary>
        /// Builds a parameterized SQL <c>WHERE</c> clause and its corresponding strongly-typed <see cref="NpgsqlParameter"/> list from a filter object.
        /// </summary>
        /// <returns>A tuple containing the SQL <c>WHERE</c> clause (or <see langword="null"/> when empty) and the bound <see cref="NpgsqlParameter"/> list.</returns>
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
        /// Sets property values on <paramref name="obj"/> by property name.
        /// </summary>
        /// <typeparam name="T">Value type stored in the dictionary.</typeparam>
        /// <param name="type">Target runtime type whose public properties are preserved for trimming and Native AOT.</param>
        /// <param name="valuesGroupedByPropertyName">Map of property names to values to assign.</param>
        public void SetValues<T>([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type type, IDictionary<string, T> valuesGroupedByPropertyName)
        {
            foreach (KeyValuePair<string, T> propertyNameAndValue in valuesGroupedByPropertyName)
            {
                PropertyInfo? property = type.GetProperty(propertyNameAndValue.Key);
                property?.SetValue(obj, propertyNameAndValue.Value);
            }
        }
    }

    extension<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(ICollection<T> collection)
    {
        /// <summary>
        /// Builds a 1-based suffixed (<c>_1</c>, <c>_2</c>, ...) sequence of strongly-typed <see cref="NpgsqlParameter"/> instances for bulk operations.
        /// </summary>
        /// <returns>An <see cref="IEnumerable{T}"/> of <see cref="NpgsqlParameter"/> instances.</returns>
        public IEnumerable<NpgsqlParameter> BuildParametersFromCollection()
        {
            if (collection.IsNullOrEmpty())
                yield break;

            int index = 0;
            (PropertyInfo Prop, NpgsqlDbType DbType)[] colMeta = GetColumnMeta<T>(declaredOnly: false);
            foreach (T obj in collection)
            {
                index++;
                foreach ((PropertyInfo prop, NpgsqlDbType dbType) in colMeta)
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

    internal static (PropertyInfo Prop, NpgsqlDbType DbType)[] GetColumnMeta<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(bool declaredOnly)
        => declaredOnly ? ColumnMetaCache<T>.DeclaredOnly : ColumnMetaCache<T>.All;

    private static (PropertyInfo Prop, NpgsqlDbType DbType)[] BuildColumnMeta([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type type, bool declaredOnly)
    {
        PropertyInfo[] props = declaredOnly
            ? type.GetDeclaredProperties<ColumnAttribute>()
            : type.GetPropertiesInHierarchy<ColumnAttribute>();

        if (declaredOnly
            && typeof(UserScopedEntityBase).IsAssignableFrom(type)
            && !props.Any(p => p.Name == nameof(UserScopedEntityBase.UserId)))
        {
            PropertyInfo? userIdProp = type.GetPropertiesInHierarchy<ColumnAttribute>().FirstOrDefault(p => p.Name == nameof(UserScopedEntityBase.UserId));
            if (userIdProp is not null)
                props = [userIdProp, .. props];
        }

        return [.. props.Select(p => (p, p.PropertyType.GetDbTypeFromPropertyType()))];
    }

    private static (PropertyInfo Prop, FilterColumnAttribute Attr)[] BuildFilterColumnMeta([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type type)
        => [.. type.GetPropertiesInHierarchy<FilterColumnAttribute>().Select(p => (p, p.GetCustomAttribute<FilterColumnAttribute>()!))];

    private static string? BuildWhereClauseCore<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(
        T obj,
        Action<string, Type, object> addParam)
    {
        if (obj is null) return null;

        FilterAttribute? filterAttribute = FilterMetaCache<T>.FilterAttribute;
        if (filterAttribute is null) return null;

        List<string> whereClauses = [];

        foreach ((PropertyInfo property, FilterColumnAttribute filterColumnAttribute) in FilterMetaCache<T>.Columns)
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
