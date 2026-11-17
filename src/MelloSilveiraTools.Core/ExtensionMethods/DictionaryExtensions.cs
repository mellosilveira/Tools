using System.Collections.Concurrent;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;

namespace MelloSilveiraTools.Core.ExtensionMethods;

/// <summary>
/// Contains extension methods for <see cref="Dictionary{TKey, TValue}"/>
/// </summary>
public static class DictionaryExtensions
{
    private static readonly ConcurrentDictionary<Type, Dictionary<string, (Action<object, object> Setter, Type PropertyType)>> _typeCache = [];

    extension(IDataReader sqlDataReader)
    {
        /// <summary>
        /// Converts the <see cref="IDataReader"/> to an object.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T ConvertTo<T>() where T : class, new()
        {
            Dictionary<string, (Action<object, object> Setter, Type PropertyType)> setters = _typeCache.GetOrAdd(typeof(T), BuildSetters);
            T obj = new();

            for (int i = 0; i < sqlDataReader.FieldCount; i++)
            {
                if (sqlDataReader.IsDBNull(i))
                    continue;

                string fieldName = sqlDataReader.GetName(i);
                if (!setters.TryGetValue(fieldName, out (Action<object, object> Setter, Type PropertyType) entry))
                    continue;

                object fieldValue = sqlDataReader.GetValue(i);
                Type underlyingType = Nullable.GetUnderlyingType(entry.PropertyType) ?? entry.PropertyType;

                object propertyValue;
                if (underlyingType == typeof(DateTimeOffset) && fieldValue is DateTime dt)
                    propertyValue = new DateTimeOffset(dt);
                else if (underlyingType == typeof(DateTimeOffset))
                    propertyValue = (DateTimeOffset)fieldValue;
                else if (underlyingType.IsEnum)
                    propertyValue = Enum.ToObject(underlyingType, fieldValue);
                else
                    propertyValue = Convert.ChangeType(fieldValue, underlyingType);

                entry.Setter(obj, propertyValue);
            }

            return obj;
        }
    }

    private static Dictionary<string, (Action<object, object> Setter, Type PropertyType)> BuildSetters([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type type)
    {
        Dictionary<string, (Action<object, object> Setter, Type PropertyType)> setters = new(StringComparer.Ordinal);

        foreach (PropertyInfo prop in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (!prop.CanWrite)
                continue;

            ParameterExpression instance = Expression.Parameter(typeof(object), "instance");
            ParameterExpression value = Expression.Parameter(typeof(object), "value");

            Action<object, object> setter = Expression.Lambda<Action<object, object>>(
                Expression.Assign(
                    Expression.Property(Expression.Convert(instance, type), prop),
                    Expression.Convert(value, prop.PropertyType)),
                instance, value).Compile();

            setters[prop.Name] = (setter, prop.PropertyType);
        }

        return setters;
    }
}
