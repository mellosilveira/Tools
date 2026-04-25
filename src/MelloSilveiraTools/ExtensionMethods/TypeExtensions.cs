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
    /// Gets every public instance property declared anywhere in the type hierarchy of <paramref name="type"/>,
    /// ordered from the topmost base class down to the most derived type. Properties declared in a base
    /// class appear before those declared in its descendants. Results are cached per <see cref="Type"/>.
    /// </summary>
    /// <param name="type">The type whose hierarchy should be walked.</param>
    /// <returns>The properties of <paramref name="type"/> ordered from base to derived.</returns>
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
    /// Gets every public instance property declared in the type hierarchy of <paramref name="type"/>
    /// that is decorated with <typeparamref name="TAttribute"/>, ordered from the topmost base class
    /// down to the most derived type. Results are cached per (type, attribute) pair.
    /// </summary>
    /// <typeparam name="TAttribute">The attribute used to filter properties.</typeparam>
    /// <param name="type">The type whose hierarchy should be walked.</param>
    /// <returns>The matching properties ordered from base to derived.</returns>
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
    /// Gets the names of every public instance property declared in the hierarchy of <paramref name="type"/>,
    /// ordered from the topmost base class down to the most derived type (same order as
    /// <see cref="GetPropertiesInHierarchy(Type)"/>). Results are cached per <see cref="Type"/>.
    /// </summary>
    /// <param name="type">The type whose hierarchy should be walked.</param>
    /// <returns>The property names of <paramref name="type"/> ordered from base to derived.</returns>
    public static string[] GetPropertyNamesInHierarchy(this Type type)
    {
        return _propertyNamesCache.GetOrAdd(type, static t => t.GetPropertiesInHierarchy().Select(p => p.Name).ToArray());
    }

    /// <summary>
    /// Gets only the public instance properties declared directly on <paramref name="type"/>,
    /// excluding those inherited from base classes.
    /// </summary>
    /// <param name="type">The type whose declared properties should be returned.</param>
    /// <returns>The public instance properties declared on <paramref name="type"/>.</returns>
    public static PropertyInfo[] GetDeclaredProperties(this Type type) => type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);

    /// <summary>
    /// Gets the public instance properties declared directly on <paramref name="type"/> (no inherited
    /// members) that are decorated with <typeparamref name="TAttribute"/>.
    /// </summary>
    /// <typeparam name="TAttribute">The attribute used to filter properties.</typeparam>
    /// <param name="type">The type whose declared properties should be inspected.</param>
    /// <returns>The matching declared properties.</returns>
    public static PropertyInfo[] GetDeclaredProperties<TAttribute>(this Type type) where TAttribute : Attribute
    {
        return type
            .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Where(p => p.GetCustomAttribute<TAttribute>() != null)
            .ToArray();
    }

    /// <summary>
    /// Gets the names of the public instance properties declared directly on <paramref name="type"/>,
    /// excluding inherited members.
    /// </summary>
    /// <param name="type">The type whose declared property names should be returned.</param>
    /// <returns>The names of the public instance properties declared on <paramref name="type"/>.</returns>
    public static string[] GetDeclaredPropertyNames(this Type type) => type.GetDeclaredProperties().Select(property => property.Name).ToArray();

    /// <summary>
    /// Indicates whether <paramref name="type"/> is an enumerable collection — that is, whether it
    /// implements <see cref="IEnumerable"/>. <see cref="string"/> is treated as a non-enumerable
    /// special case and always returns <see langword="false"/>.
    /// </summary>
    /// <param name="type">The type to test.</param>
    /// <returns><see langword="true"/> if <paramref name="type"/> implements <see cref="IEnumerable"/> and is not <see cref="string"/>; otherwise <see langword="false"/>.</returns>
    public static bool IsEnumerable(this Type type) => typeof(IEnumerable).IsAssignableFrom(type) && type != typeof(string);

    /// <summary>
    /// Returns the <see cref="NpgsqlDbType"/> from property type.
    /// </summary>
    /// <param name="type">The CLR property type to be mapped to its corresponding <see cref="NpgsqlDbType"/>.</param>
    /// <returns>The <see cref="NpgsqlDbType"/> matching <paramref name="type"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="type"/> is not handled by this mapping (i.e. it is not one of the supported
    /// primitive, nullable primitive, array or generic enumerable types).
    /// </exception>
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
