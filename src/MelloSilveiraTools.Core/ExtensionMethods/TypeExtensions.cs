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
        /// Gets every public instance property declared anywhere in the type hierarchy of <paramref name="type"/>,
        /// ordered from the topmost base class down to the most derived type. Properties declared in a base
        /// class appear before those declared in its descendants. Results are cached per <see cref="Type"/>.
        /// </summary>
        /// <remarks>
        /// <paramref name="type"/>: The type whose hierarchy should be walked.
        /// </remarks>
        /// <returns>The properties of <paramref name="type"/> ordered from base to derived.</returns>
        public PropertyInfo[] GetPropertiesInHierarchy() => _hierarchyCache.GetOrAdd(
            type,
            static t =>
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
        public PropertyInfo[] GetPropertiesInHierarchy<TAttribute>() where TAttribute : Attribute => _hierarchyAttrCache.GetOrAdd(
            (type, typeof(TAttribute)),
            static key =>
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

        /// <summary>
        /// Gets the names of every public instance property declared in the hierarchy of <paramref name="type"/>,
        /// ordered from the topmost base class down to the most derived type (same order as
        /// <see cref="GetPropertiesInHierarchy(Type)"/>). Results are cached per <see cref="Type"/>.
        /// </summary>
        /// <remarks>
        /// <paramref name="type"/>: The type whose hierarchy should be walked.
        /// </remarks>
        /// <returns>The property names of <paramref name="type"/> ordered from base to derived.</returns>
        public string[] GetPropertyNamesInHierarchy() => _propertyNamesCache.GetOrAdd(type, static t => [.. t.GetPropertiesInHierarchy().Select(p => p.Name)]);

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
        public PropertyInfo[] GetDeclaredProperties<TAttribute>() where TAttribute : Attribute => [.. type
            .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Where(p => p.GetCustomAttribute<TAttribute>() != null)];

        /// <summary>
        /// Gets the names of the public instance properties declared directly on <paramref name="type"/>,
        /// excluding inherited members.
        /// </summary>
        /// <remarks>
        /// <paramref name="type"/>: The type whose declared property names should be returned.
        /// </remarks>
        /// <returns>The names of the public instance properties declared on <paramref name="type"/>.</returns>
        public string[] GetDeclaredPropertyNames() => [.. type.GetDeclaredProperties().Select(property => property.Name)];
    }
}
