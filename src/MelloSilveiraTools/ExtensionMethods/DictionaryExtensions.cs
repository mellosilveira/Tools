using System.Data;

namespace MelloSilveiraTools.ExtensionMethods
{
    /// <summary>
    /// Contains extension methods for <see cref="Dictionary{TKey, TValue}"/>
    /// </summary>
    public static class DictionaryExtensions
    {
        /// <summary>
        /// Converts the <see cref="IDataReader"/> to an object.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sqlDataReader"></param>
        /// <returns></returns>
        public static T ConvertTo<T>(this IDataReader sqlDataReader) where T : class, new()
        {
            Type type = typeof(T);
            var obj = new T();

            for (int i = 0; i < sqlDataReader.FieldCount; i++)
            {
                if (sqlDataReader.IsDBNull(i))
                    continue;

                var fieldName = sqlDataReader.GetName(i);
                var propertyInfo = type.GetProperty(fieldName);
                if (propertyInfo is null)
                    continue;

                var propertyType = Nullable.GetUnderlyingType(propertyInfo.PropertyType) ?? propertyInfo.PropertyType;
                object fieldValue = sqlDataReader.GetValue(i);
                if (fieldValue is not null)
                    propertyInfo.SetValue(obj, propertyInfo.PropertyType.IsEnum
                        ? Enum.Parse(propertyInfo.PropertyType, fieldValue.ToString()!)
                        : Convert.ChangeType(fieldValue, propertyType));
            }

            return obj;
        }
    }
}
