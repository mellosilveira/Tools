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
            // TODO: ISSO É CUSTOSO POR USAR REFLECTION, DEVE SER OTIMIZADO.
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
                {
                    object propertyValue;
                    if (propertyInfo.PropertyType == typeof(DateTimeOffset))
                        propertyValue = DateTimeOffset.Parse(fieldValue.ToString()!);
                    else if (propertyInfo.PropertyType.IsEnum)
                        propertyValue = Enum.Parse(propertyInfo.PropertyType, fieldValue.ToString()!);
                    else
                        propertyValue = Convert.ChangeType(fieldValue, propertyType);

                    propertyInfo.SetValue(obj, propertyValue);
                }
            }

            return obj;
        }
    }
}
