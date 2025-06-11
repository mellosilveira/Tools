using System.Data.Common;
using System.Reflection;

namespace MelloSilveiraTools.ExtensionMethods
{
    /// <summary>
    /// Constains extension methods for <see cref="DbDataReader"/>.
    /// </summary>
    public static class DbDataReaderExtensions
    {
        /// <summary>
        /// Converts a <see cref="DbDataReader"/> to an object.
        /// </summary>
        /// <typeparam name="T">Type of <see cref="DbDataReader"/> must be converted.</typeparam>
        /// <param name="reader"></param>
        /// <returns></returns>
        public static T ToObject<T>(this DbDataReader reader) where T : new()
        {
            T obj = new();
            Type objectType = typeof(T);

            for (int i = 0; i < reader.FieldCount; i++)
            {
                string columnName = reader.GetName(i);
                PropertyInfo? property = objectType.GetProperty(columnName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

                if (property != null && property.CanWrite)
                {
                    object value = reader.GetValue(i);
                    if (value != DBNull.Value)
                        property.SetValue(obj, value);
                }
            }

            return obj;
        }

        /// <summary>
        /// Converts a <see cref="DbDataReader"/> to an object using a cache to store property info of object.
        /// </summary>
        /// <typeparam name="T">Type of <see cref="DbDataReader"/> must be converted.</typeparam>
        /// <param name="reader"></param>
        /// <param name="propertyInfoCache"></param>
        /// <returns></returns>
        public static T ToObject<T>(this DbDataReader reader, Dictionary<string, PropertyInfo?> propertyInfoCache) where T : new()
        {
            T obj = new();
            Type objectType = typeof(T);

            for (int i = 0; i < reader.FieldCount; i++)
            {
                string columnName = reader.GetName(i);

                if (!propertyInfoCache.TryGetValue(columnName, out PropertyInfo? property))
                {
                    property = objectType.GetProperty(columnName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                    propertyInfoCache[columnName] = property;
                }

                if (property != null && property.CanWrite)
                {
                    object value = reader.GetValue(i);
                    if (value != DBNull.Value)
                        property.SetValue(obj, value);
                }
            }

            return obj;
        }
    }
}
