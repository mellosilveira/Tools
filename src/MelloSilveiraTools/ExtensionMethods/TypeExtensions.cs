using NpgsqlTypes;
using System.Collections;
using System.Collections.Concurrent;
using System.Reflection;

namespace MelloSilveiraTools.ExtensionMethods;

/// <summary>
/// Contains extension methods for <see cref="Type"/>.
/// </summary>
public static class TypeExtensions
{
    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> _hierarchyCache = new();
    private static readonly ConcurrentDictionary<(Type Type, Type Attr), PropertyInfo[]> _hierarchyAttrCache = new();
    private static readonly ConcurrentDictionary<Type, string[]> _propertyNamesCache = new();

    /// <summary>
    /// Gets the properties from a <see cref="Type"/> in following the hierarchy order from parent to child.
    /// </summary>
    /// <param name="type"></param>
    /// <returns>A <see cref="List{T}"/> with the properties of the <paramref name="type"/>.</returns>
    public static PropertyInfo[] GetPropertiesInHierarchy(this Type type)
    {
        return _hierarchyCache.GetOrAdd(type, static t =>
        {
            Type? localType = t;
            List<PropertyInfo> properties = [];

            while (localType != null)
            {
                properties.InsertRange(0, localType.GetDeclaredProperties());
                localType = localType.BaseType;
            }

            return [.. properties];
        });
    }


    /// <summary>
    /// Gets the properties from a <see cref="Type"/> in following the hierarchy order from parent to child.
    /// </summary>
    /// <param name="type"></param>
    /// <returns>A <see cref="List{T}"/> with the properties of the <paramref name="type"/>.</returns>
    public static PropertyInfo[] GetPropertiesInHierarchy<TAttribute>(this Type type) where TAttribute : Attribute
    {
        return _hierarchyAttrCache.GetOrAdd((type, typeof(TAttribute)), static key =>
        {
            Type? localType = key.Type;
            List<PropertyInfo> properties = [];

            while (localType != null)
            {
                properties.InsertRange(0, localType.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly).Where(p => p.GetCustomAttribute(key.Attr) != null));
                localType = localType.BaseType;
            }

            return [.. properties];
        });
    }

    /// <summary>
    /// Gets the properties name defined in the <see cref="Type"/>.
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static string[] GetPropertyNamesInHierarchy(this Type type)
    {
        return _propertyNamesCache.GetOrAdd(type, static t => t.GetPropertiesInHierarchy().Select(p => p.Name).ToArray());
    }

    /// <summary>
    /// Gets the properties defined in the <see cref="Type"/>.
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static PropertyInfo[] GetDeclaredProperties(this Type type) => type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);

    /// <summary>
    /// Gets the properties defined in the <see cref="Type"/>.
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static PropertyInfo[] GetDeclaredProperties<TAttribute>(this Type type) where TAttribute : Attribute
    {
        return type
            .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Where(p => p.GetCustomAttribute<TAttribute>() != null)
            .ToArray();
    }

    /// <summary>
    /// Gets the properties name defined in the <see cref="Type"/>.
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static string[] GetDeclaredPropertyNames(this Type type) => type.GetDeclaredProperties().Select(property => property.Name).ToArray();

    /// <summary>
    /// Checks if the type is an <see cref="IEnumerable"/> excluding the <see cref="string"/>.
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static bool IsEnumerable(this Type type) => typeof(IEnumerable).IsAssignableFrom(type) && type != typeof(string);

    /// <summary>
    /// Returns the <see cref="NpgsqlDbType"/> from property type.
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public static NpgsqlDbType GetDbTypeFromPropertyType(this Type type)
    {
        if (type == typeof(string)) return NpgsqlDbType.Text;
        if (type == typeof(bool) || type == typeof(bool?)) return NpgsqlDbType.Boolean;
        if (type == typeof(short) || type == typeof(short?)) return NpgsqlDbType.Smallint;
        if (type == typeof(int) || type == typeof(int?)) return NpgsqlDbType.Integer;
        if (type == typeof(long) || type == typeof(long?)) return NpgsqlDbType.Bigint;
        if (type == typeof(float) || type == typeof(float?)) return NpgsqlDbType.Real;
        if (type == typeof(double) || type == typeof(double?)) return NpgsqlDbType.Double;
        if (type == typeof(decimal) || type == typeof(decimal?)) return NpgsqlDbType.Numeric;
        if (type == typeof(byte[])) return NpgsqlDbType.Bytea;
        if (type == typeof(string[])) return NpgsqlDbType.Text | NpgsqlDbType.Array;
        if (type == typeof(DateTime) || type == typeof(DateTime?)) return NpgsqlDbType.Timestamp;
        if (type == typeof(DateTimeOffset) || type == typeof(DateTimeOffset?)) return NpgsqlDbType.TimestampTz;
        if (type == typeof(IList) || type == typeof(IEnumerable) || type == typeof(IEnumerator)) return NpgsqlDbType.Array;
        throw new ArgumentOutOfRangeException(nameof(type), $"Invalid type: '{type.FullName}'.");
    }
}
