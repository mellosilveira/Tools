using Dapper;
using MelloSilveiraTools.Infrastructure.Database.Attributes;
using MelloSilveiraTools.Infrastructure.Database.Models;
using System.Reflection;

namespace MelloSilveiraTools.ExtensionMethods;

/// <summary>
/// Contains extension methods for class.
/// </summary>
public static class ClassExtensions
{
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

        Dictionary<string, object?> propertyNameAndValues = [];

        PropertyInfo[] properties = obj.GetType().GetPropertiesInHierarchy();
        foreach (var property in properties)
        {
            var attribute = property.GetCustomAttribute<TCustomAttribute>();
            if (attribute != null)
            {
                object? value = property.GetValue(obj);
                propertyNameAndValues.Add(property.Name, value);
            }
        }

        return propertyNameAndValues;
    }

    /// <summary>
    /// Builds a SQL WHERE clause and a <see cref="DynamicParameters"/> based on filter.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="obj"></param>
    /// <returns></returns>
    public static (string? SqlWhereClause, DynamicParameters? Parameters) BuildWhereClauseAndParameters<T>(this T obj)
    {
        if (obj is null)
            return (null, null);

        // Step 1. Gets the attribute for filter.
        var type = obj.GetType();
        FilterAttribute? filterAttribute = type.GetCustomAttribute<FilterAttribute>();
        if (filterAttribute is null)
            return (null, null);

        // Step 2. Initializes the variables to be used.
        var parameters = new DynamicParameters();
        List<string> whereClauses = [];

        // Step 3. Gets the properties of type according to its hierarchy of classes and for each property:
        // 3.1. Gets the filter column attribute and create the line for WHERE statement.
        var properties = type.GetPropertiesInHierarchy();
        foreach (PropertyInfo property in properties)
        {
            // 3.1.
            var filterColumnAttribute = property.GetCustomAttribute<FilterColumnAttribute>();
            if (filterColumnAttribute != null)
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

                propertyValue = filterColumnAttribute.FilterClause.Equals(FilterClause.Like) ? $"%{propertyValue}%" : propertyValue;
                parameters.Add(property.Name, propertyValue);
            }
        }

        // Step 4. Creates the SQL WHERE clause and returns it and parameters.
        string? whereClause = whereClauses.IsNullOrEmpty() ? null : $"WHERE {string.Join("\r\n\tAND ", whereClauses)}";
        return (whereClause, parameters);
    }
}