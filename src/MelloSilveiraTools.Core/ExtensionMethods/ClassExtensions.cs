using MelloSilveiraTools.Database.ExtensionMethods;
using System.Reflection;

namespace MelloSilveiraTools.Core.ExtensionMethods;

public static class ClassExtensions
{
    extension<T>(T obj)
    {
        /// <summary>
        /// Wraps the supplied instance in a completed <see cref="Task{T}"/>.
        /// </summary>
        public Task<T> ToTask() => Task.FromResult(obj);

        /// <summary>
        /// Gets the values from object which is following the hierarchy order from parent to child.
        /// </summary>
        /// <returns>A <see cref="List{T}"/> containing the values from object which is following the hierarchy order from parent to child.</returns>
        public IEnumerable<object?> GetValuesInHierarchy()
        {
            PropertyInfo[] properties = typeof(T).GetPropertiesInHierarchy();
            return obj.GetValues(properties);
        }

        /// <summary>
        /// Gets the values from object using an <see cref="IEnumerable{T}"/> of properties as reference.
        /// </summary>
        /// <param name="properties">Properties to be used as reference to get the values from object.</param>
        /// <returns></returns>
        public IEnumerable<object?> GetValues(IEnumerable<PropertyInfo> properties)
        {
            return properties.Select(property => property.GetValue(obj));
        }

        /// <summary>
        /// Gets the name and value of properties from object which is following the hierarchy order from parent to child.
        /// It is also possible to filter by a custom attribute.
        /// </summary>
        /// <typeparam name="TCustomAttribute">The type of custom attribute to be used in search.</typeparam>
        /// <returns>
        /// A <see cref="Dictionary{TKey, TValue}"/> which the key is the property name and the value is the property value.
        /// </returns>
        public Dictionary<string, object?>? GetPropertyNamesAndValuesInHierarchy<TCustomAttribute>() where TCustomAttribute : Attribute
        {
            if (obj is null)
                return null;

            Dictionary<string, object?> dict = [];
            foreach (PropertyInfo property in typeof(T).GetPropertiesInHierarchy<TCustomAttribute>())
                dict.Add(property.Name, property.GetValue(obj));

            return dict;
        }
    }
}
