using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace MelloSilveiraTools.Core.ExtensionMethods;

/// <summary>
/// Contains extension methods for <see cref="Type"/>.
/// </summary>
public static class TypeExtensions
{
    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> _hierarchyCache = new();
    private static readonly ConcurrentDictionary<(Type Type, Type Attr), PropertyInfo[]> _hierarchyAttrCache = new();
    private static readonly ConcurrentDictionary<Type, string[]> _propertyNamesCache = new();

    extension(Type type)
    {
        /// <summary>
        /// Indicates whether <paramref name="type"/> is an enumerable collection — that is, whether it
        /// implements <see cref="IEnumerable"/>. <see cref="string"/> is treated as a non-enumerable
        /// special case and always returns <see langword="false"/>.
        /// </summary>
        /// <remarks>
        /// <paramref name="type"/>: The type to test.
        /// </remarks>
        /// <returns><see langword="true"/> if <paramref name="type"/> implements <see cref="IEnumerable"/> and is not <see cref="string"/>; otherwise <see langword="false"/>.</returns>
        public bool IsEnumerable() => typeof(IEnumerable).IsAssignableFrom(type) && type != typeof(string);
    }

    extension([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type type)
    {
        /// <summary>
        /// Gets every public instance property declared anywhere in the type hierarchy of <paramref name="type"/>,
        /// ordered from the topmost base class down to the most derived type. Properties declared in a base
        /// class appear before those declared in its descendants. Results are cached per <see cref="Type"/>.
        /// </summary>
        /// <remarks>
        /// <paramref name="type"/>: The type whose hierarchy should be walked.
        /// </remarks>
        /// <returns>The properties of <paramref name="type"/> ordered from base to derived.</returns>
        public PropertyInfo[] GetPropertiesInHierarchy()
        {
            if (_hierarchyCache.TryGetValue(type, out PropertyInfo[]? cached))
                return cached;

            PropertyInfo[] allProperties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);
            PropertyInfo[] ordered = OrderPropertiesBaseToDerived(type, allProperties);
            _hierarchyCache.TryAdd(type, ordered);
            return ordered;
        }

        /// <summary>
        /// Gets every public instance property declared in the type hierarchy of <paramref name="type"/>
        /// that is decorated with <typeparamref name="TAttribute"/>, ordered from the topmost base class
        /// down to the most derived type. Results are cached per (type, attribute) pair.
        /// </summary>
        /// <typeparam name="TAttribute">The attribute used to filter properties.</typeparam>
        /// <remarks>
        /// <paramref name="type"/>: The type whose hierarchy should be walked.
        /// </remarks>
        /// <returns>The matching properties ordered from base to derived.</returns>
        public PropertyInfo[] GetPropertiesInHierarchy<TAttribute>() where TAttribute : Attribute
        {
            (Type, Type) key = (type, typeof(TAttribute));
            if (_hierarchyAttrCache.TryGetValue(key, out PropertyInfo[]? cached))
                return cached;

            PropertyInfo[] hierarchyProperties = type.GetPropertiesInHierarchy();
            List<PropertyInfo> filtered = [];
            for (int i = 0; i < hierarchyProperties.Length; i++)
            {
                if (Attribute.IsDefined(hierarchyProperties[i], typeof(TAttribute)))
                    filtered.Add(hierarchyProperties[i]);
            }

            PropertyInfo[] result = [.. filtered];
            _hierarchyAttrCache.TryAdd(key, result);
            return result;
        }

        /// <summary>
        /// Gets the names of every public instance property declared in the hierarchy of <paramref name="type"/>,
        /// ordered from the topmost base class down to the most derived type (same order as
        /// <see cref="GetPropertiesInHierarchy(Type)"/>). Results are cached per <see cref="Type"/>.
        /// </summary>
        /// <remarks>
        /// <paramref name="type"/>: The type whose hierarchy should be walked.
        /// </remarks>
        /// <returns>The property names of <paramref name="type"/> ordered from base to derived.</returns>
        public string[] GetPropertyNamesInHierarchy()
        {
            if (_propertyNamesCache.TryGetValue(type, out string[]? cached))
                return cached;

            PropertyInfo[] properties = type.GetPropertiesInHierarchy();
            string[] names = new string[properties.Length];
            for (int i = 0; i < properties.Length; i++)
                names[i] = properties[i].Name;

            _propertyNamesCache.TryAdd(type, names);
            return names;
        }

        /// <summary>
        /// Gets only the public instance properties declared directly on <paramref name="type"/>,
        /// excluding those inherited from base classes.
        /// </summary>
        /// <remarks>
        /// <paramref name="type"/>: The type whose declared properties should be returned.
        /// </remarks>
        /// <returns>The public instance properties declared on <paramref name="type"/>.</returns>
        public PropertyInfo[] GetDeclaredProperties() => type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);

        /// <summary>
        /// Gets the public instance properties declared directly on <paramref name="type"/> (no inherited
        /// members) that are decorated with <typeparamref name="TAttribute"/>.
        /// </summary>
        /// <typeparam name="TAttribute">The attribute used to filter properties.</typeparam>
        /// <remarks>
        /// <paramref name="type"/>: The type whose declared properties should be inspected.
        /// </remarks>
        /// <returns>The matching declared properties.</returns>
        public PropertyInfo[] GetDeclaredProperties<TAttribute>() where TAttribute : Attribute
        {
            PropertyInfo[] declared = type.GetDeclaredProperties();
            List<PropertyInfo> filtered = [];
            for (int i = 0; i < declared.Length; i++)
            {
                if (declared[i].GetCustomAttribute<TAttribute>() != null)
                    filtered.Add(declared[i]);
            }

            return [.. filtered];
        }

        /// <summary>
        /// Gets the names of the public instance properties declared directly on <paramref name="type"/>,
        /// excluding inherited members.
        /// </summary>
        /// <remarks>
        /// <paramref name="type"/>: The type whose declared property names should be returned.
        /// </remarks>
        /// <returns>The names of the public instance properties declared on <paramref name="type"/>.</returns>
        public string[] GetDeclaredPropertyNames()
        {
            PropertyInfo[] declared = type.GetDeclaredProperties();
            string[] names = new string[declared.Length];
            for (int i = 0; i < declared.Length; i++)
                names[i] = declared[i].Name;

            return names;
        }
    }

    private static PropertyInfo[] OrderPropertiesBaseToDerived(Type type, PropertyInfo[] allProperties)
    {
        if (allProperties.Length <= 1)
            return allProperties;

        List<Type> hierarchy = [];
        for (Type? current = type; current != null; current = current.BaseType)
            hierarchy.Add(current);

        Dictionary<Type, int> depthMap = new(hierarchy.Count);
        for (int i = 0; i < hierarchy.Count; i++)
            depthMap[hierarchy[i]] = hierarchy.Count - 1 - i;

        List<PropertyInfo>?[] buckets = new List<PropertyInfo>?[hierarchy.Count];
        int fallbackBucket = hierarchy.Count - 1;

        for (int i = 0; i < allProperties.Length; i++)
        {
            PropertyInfo property = allProperties[i];
            int bucketIndex = property.DeclaringType != null && depthMap.TryGetValue(property.DeclaringType, out int depth)
                ? depth
                : fallbackBucket;

            buckets[bucketIndex] ??= [];
            buckets[bucketIndex]!.Add(property);
        }

        PropertyInfo[] ordered = new PropertyInfo[allProperties.Length];
        int offset = 0;
        for (int i = 0; i < buckets.Length; i++)
        {
            List<PropertyInfo>? bucket = buckets[i];
            if (bucket is null)
                continue;

            for (int j = 0; j < bucket.Count; j++)
                ordered[offset++] = bucket[j];
        }

        return ordered;
    }
}
