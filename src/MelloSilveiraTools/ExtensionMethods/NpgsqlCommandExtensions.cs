using MelloSilveiraTools.Infrastructure.Database.Attributes;
using Npgsql;
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
    public static NpgsqlCommand SetCommandParametersFromEntity<TEntity>(this NpgsqlCommand command, TEntity entity)
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
                command.Parameters.AddWithValue(property.Name, property.PropertyType.GetDbTypeFromPropertyType(), value ?? DBNull.Value);
            }
        }

        return command;
    }

    /// <summary>
    /// Sets the parameter for sql command.
    /// </summary>
    /// <param name="command"></param>
    /// <param name="parameters"></param>
    /// <returns></returns>
    public static NpgsqlCommand SetCommandParameters(this NpgsqlCommand command, IEnumerable<NpgsqlParameter> parameters)
    {
        if (parameters.IsNullOrEmpty())
            return command;

        foreach (NpgsqlParameter parameter in parameters)
        {
            command.Parameters.Add(parameter);
        }

        return command;
    }
}
