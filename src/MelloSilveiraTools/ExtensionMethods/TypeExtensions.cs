using System.Collections;
using System.Reflection;

namespace MelloSilveiraTools.ExtensionMethods;

/// <summary>
/// Contains extension methods for <see cref="Type"/>.
/// </summary>
public static class TypeExtensions
{
    /// <summary>
    /// Gets the properties from a <see cref="Type"/> in following the hierarchy order from parent to child.
    /// </summary>
    /// <param name="type"></param>
    /// <returns>A <see cref="List{T}"/> with the properties of the <paramref name="type"/>.</returns>
    public static PropertyInfo[] GetPropertiesInHierarchy(this Type type)
    {
        Type? localType = type;
        List<PropertyInfo> properties = [];

        while (localType != null)
        {
            properties.InsertRange(0, localType.GetDeclaredProperties());
            localType = localType.BaseType;
        }

        return [.. properties];
    }


    /// <summary>
    /// Gets the properties from a <see cref="Type"/> in following the hierarchy order from parent to child.
    /// </summary>
    /// <param name="type"></param>
    /// <returns>A <see cref="List{T}"/> with the properties of the <paramref name="type"/>.</returns>
    public static PropertyInfo[] GetPropertiesInHierarchy<TAttribute>(this Type type) where TAttribute : Attribute
    {
        Type? localType = type;
        List<PropertyInfo> properties = [];

        while (localType != null)
        {
            properties.InsertRange(0, localType.GetDeclaredProperties<TAttribute>());
            localType = localType.BaseType;
        }

        return [.. properties];
    }

    /// <summary>
    /// Gets the properties name defined in the <see cref="Type"/>.
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static string[] GetPropertyNamesInHierarchy(this Type type)
    {
        Type? localType = type;
        List<string> propertyNames = [];

        while (localType != null)
        {
            propertyNames.InsertRange(0, localType.GetDeclaredPropertyNames());
            localType = localType.BaseType;
        }

        return [.. propertyNames];
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
}