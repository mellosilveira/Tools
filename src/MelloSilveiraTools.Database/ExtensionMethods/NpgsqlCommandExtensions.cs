using MelloSilveiraTools.Core.ExtensionMethods;
using MelloSilveiraTools.Database.RelationalDatabase.Attributes;
using Npgsql;

namespace MelloSilveiraTools.Database.ExtensionMethods;

/// <summary>
/// Contains extension methods for <see cref="NpgsqlCommand"/>.
/// </summary>
public static class NpgsqlCommandExtensions
{
    extension(NpgsqlCommand command)
    {
        /// <summary>
        /// Sets the parameter for sql command.
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="entity"></param>
        /// <returns></returns>
        public NpgsqlCommand SetCommandParametersFromEntity<TEntity>(TEntity entity)
        {
            if (entity is null)
                return command;

            foreach (var property in entity.GetType().GetPropertiesInHierarchy<ColumnAttribute>())
            {
                object? value = property.GetValue(entity);
                command.Parameters.AddWithValue(property.Name, property.PropertyType.GetDbTypeFromPropertyType(), value ?? DBNull.Value);
            }

            return command;
        }

        /// <summary>
        /// Sets the parameter for sql command.
        /// </summary>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public NpgsqlCommand SetCommandParameters(IEnumerable<NpgsqlParameter> parameters)
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
}
