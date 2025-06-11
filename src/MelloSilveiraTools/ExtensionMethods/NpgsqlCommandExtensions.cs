using MelloSilveiraTools.Infrastructure.Database.Attributes;
using Npgsql;
using NpgsqlTypes;
using System.Collections;
using System.Reflection;

namespace MelloSilveiraTools.ExtensionMethods;

/// <summary>
/// Contains extension methods for <see cref="NpgsqlCommand"/>.
/// </summary>
public static class NpgsqlCommandExtensions
{
    /// <summary>
    /// Sets the parameter for sql command.
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <param name="command"></param>
    /// <param name="entity"></param>
    /// <returns></returns>
    public static NpgsqlCommand SetCommandParameters<TEntity>(this NpgsqlCommand command, TEntity entity)
    {
        if (entity is null)
            return command;

        PropertyInfo[] properties = entity.GetType().GetPropertiesInHierarchy();
        foreach (var property in properties)
        {
            var attribute = property.GetCustomAttribute<ColumnAttribute>();
            if (attribute != null)
            {
                object? value = property.GetValue(entity);
                command.Parameters.AddWithValue(property.Name, GetDbTypeFromPropertyType(property.PropertyType), value ?? DBNull.Value);
            }
        }

        return command;
    }

    /// <summary>
    /// Returns the <see cref="NpgsqlDbType"/> from property type.
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    private static NpgsqlDbType GetDbTypeFromPropertyType(Type type)
    {
        if (type == typeof(string)) return NpgsqlDbType.Text;
        if (type == typeof(double)) return NpgsqlDbType.Double;
        if (type == typeof(long)) return NpgsqlDbType.Bigint;
        if (type == typeof(DateTime) || type == typeof(DateTimeOffset)) return NpgsqlDbType.Timestamp;
        if (type == typeof(IList) || type == typeof(IEnumerable) || type == typeof(IEnumerator)) return NpgsqlDbType.Array;
        throw new ArgumentOutOfRangeException(nameof(type), $"Invalid type: '{type.FullName}'.");
    }
}
