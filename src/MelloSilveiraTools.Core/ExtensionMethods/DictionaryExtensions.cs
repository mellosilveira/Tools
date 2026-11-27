using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace MelloSilveiraTools.Core.ExtensionMethods;

/// <summary>
/// Contains extension methods for mapping <see cref="IDataReader"/> rows into CLR instances.
/// Uses compiled expression setters on JIT runtimes and falls back to reflection setters
/// automatically under Native AOT (<see cref="RuntimeFeature.IsDynamicCodeSupported"/> == <see langword="false"/>).
/// </summary>
public static class DictionaryExtensions
{
    private static class SetterCache<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>
    {
        public static readonly Dictionary<string, (Action<object, object> Setter, Type PropertyType)> Setters = BuildSetters(typeof(T));
    }

    extension(IDataReader sqlDataReader)
    {
        /// <summary>
        /// Converts the current row of <paramref name="sqlDataReader"/> into a new instance of <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">Target entity type whose public properties are preserved for trimming and Native AOT.</typeparam>
        /// <returns>A populated instance of <typeparamref name="T"/>.</returns>
        public T ConvertTo<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>() where T : class, new()
        {
            Dictionary<string, (Action<object, object> Setter, Type PropertyType)> setters = SetterCache<T>.Setters;
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
                else if (underlyingType.IsInstanceOfType(fieldValue))
                    propertyValue = fieldValue;
                else
                    propertyValue = Convert.ChangeType(fieldValue, underlyingType, CultureInfo.InvariantCulture);

                entry.Setter(obj, propertyValue);
            }

            return obj;
        }
    }

    private static Dictionary<string, (Action<object, object> Setter, Type PropertyType)> BuildSetters([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type type)
    {
        Dictionary<string, (Action<object, object> Setter, Type PropertyType)> setters = new(StringComparer.OrdinalIgnoreCase);

        foreach (PropertyInfo prop in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (!prop.CanWrite)
                continue;

            Action<object, object> setter = RuntimeFeature.IsDynamicCodeSupported ? CompileSetter(type, prop) : prop.SetValue;
            setters[prop.Name] = (setter, prop.PropertyType);
        }

        return setters;
    }

    private static Action<object, object> CompileSetter(Type type, PropertyInfo prop)
    {
        ParameterExpression instance = Expression.Parameter(typeof(object), "instance");
        ParameterExpression value = Expression.Parameter(typeof(object), "value");

        return Expression.Lambda<Action<object, object>>(
            Expression.Assign(
                Expression.Property(Expression.Convert(instance, type), prop),
                Expression.Convert(value, prop.PropertyType)),
            instance, value).Compile();
    }
}
