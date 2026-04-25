using System.Collections.Concurrent;
using System.Data;
using System.Linq.Expressions;
using System.Reflection;

namespace MelloSilveiraTools.ExtensionMethods;

/// <summary>
/// Contains extension methods for <see cref="Dictionary{TKey, TValue}"/>
/// </summary>
public static class DictionaryExtensions
{
    private static readonly ConcurrentDictionary<Type, Dictionary<string, (Action<object, object> Setter, Type PropertyType)>> _typeCache = [];

    /// <summary>
    /// Converts the <see cref="IDataReader"/> to an object.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="sqlDataReader"></param>
    /// <returns></returns>
    public static T ConvertTo<T>(this IDataReader sqlDataReader) where T : class, new()
    {
        var setters = _typeCache.GetOrAdd(typeof(T), BuildSetters);
        var obj = new T();

        for (int i = 0; i < sqlDataReader.FieldCount; i++)
        {
            if (sqlDataReader.IsDBNull(i))
                continue;

            var fieldName = sqlDataReader.GetName(i);
            if (!setters.TryGetValue(fieldName, out var entry))
                continue;

            object fieldValue = sqlDataReader.GetValue(i);
            var underlyingType = Nullable.GetUnderlyingType(entry.PropertyType) ?? entry.PropertyType;

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

    private static Dictionary<string, (Action<object, object> Setter, Type PropertyType)> BuildSetters(Type type)
    {
        var result = new Dictionary<string, (Action<object, object>, Type)>(StringComparer.Ordinal);

        foreach (var prop in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (!prop.CanWrite)
                continue;

            var instance = Expression.Parameter(typeof(object), "instance");
            var value = Expression.Parameter(typeof(object), "value");

            var setter = Expression.Lambda<Action<object, object>>(
                Expression.Assign(
                    Expression.Property(Expression.Convert(instance, type), prop),
                    Expression.Convert(value, prop.PropertyType)),
                instance, value).Compile();

            result[prop.Name] = (setter, prop.PropertyType);
        }

        return result;
    }
}
