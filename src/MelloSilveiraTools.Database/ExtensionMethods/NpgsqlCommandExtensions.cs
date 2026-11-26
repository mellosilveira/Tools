using MelloSilveiraTools.Core.ExtensionMethods;
using MelloSilveiraTools.Database.RelationalDatabase.Attributes;
using Npgsql;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace MelloSilveiraTools.Database.ExtensionMethods;

/// <summary>
/// Contains extension methods for <see cref="NpgsqlCommand"/>.
/// </summary>
public static class NpgsqlCommandExtensions
{
    extension(NpgsqlCommand command)
    {
        /// <summary>
        /// Adds strongly-typed <see cref="NpgsqlParameter"/> entries derived from <c>[Column]</c>-annotated properties of <paramref name="entity"/>.
        /// </summary>
        /// <typeparam name="TEntity">Entity type whose public properties are preserved for trimming and Native AOT.</typeparam>
        /// <param name="entity">Entity instance providing parameter values.</param>
        /// <returns>The same <see cref="NpgsqlCommand"/> instance for call chaining.</returns>
        public NpgsqlCommand SetCommandParametersFromEntity<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TEntity>(TEntity entity)
        {
            if (entity is null)
                return command;

            foreach (PropertyInfo property in typeof(TEntity).GetPropertiesInHierarchy<ColumnAttribute>())
            {
                object? value = property.GetCustomValue(entity);
                command.Parameters.AddWithValue(property.Name, property.PropertyType.GetDbTypeFromPropertyType(), value ?? DBNull.Value);
            }

            return command;
        }

        /// <summary>
        /// Adds all <see cref="NpgsqlParameter"/> instances from <paramref name="parameters"/> to the command.
        /// </summary>
        /// <param name="parameters">Parameters to bind to the command.</param>
        /// <returns>The same <see cref="NpgsqlCommand"/> instance for call chaining.</returns>
        public NpgsqlCommand SetCommandParameters(IEnumerable<NpgsqlParameter>? parameters)
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
