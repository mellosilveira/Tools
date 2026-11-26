using Npgsql;

namespace MelloSilveiraTools.Database.ExtensionMethods;

/// <summary>
/// Contains extension methods for <see cref="NpgsqlBatchCommand"/>.
/// </summary>
public static class NpgsqlBatchCommandExtensions
{
    extension(NpgsqlBatchCommand command)
    {
        /// <summary>
        /// Adds all <see cref="NpgsqlParameter"/> instances from <paramref name="parameters"/> to the batch command,
        /// optionally binding detached <see cref="NpgsqlParameter.Clone"/> copies for retry-safe execution.
        /// </summary>
        /// <param name="parameters">Parameters to bind to the batch command.</param>
        /// <param name="cloneParameters">
        /// When <see langword="true"/>, binds detached <see cref="NpgsqlParameter.Clone"/> copies so retries reuse
        /// pre-built parameters without parameter-collection ownership conflicts.
        /// </param>
        /// <returns>The same <see cref="NpgsqlBatchCommand"/> instance for call chaining.</returns>
        public NpgsqlBatchCommand SetCommandParameters(IEnumerable<NpgsqlParameter>? parameters, bool cloneParameters = false)
        {
            if (parameters is null)
                return command;

            foreach (NpgsqlParameter parameter in parameters)
                command.Parameters.Add(cloneParameters ? parameter.Clone() : parameter);

            return command;
        }
    }
}
