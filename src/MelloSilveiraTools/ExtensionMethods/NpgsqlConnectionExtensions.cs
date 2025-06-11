using Dapper;
using MelloSilveiraTools.ExtensionMethods;
using Npgsql;

namespace MelloSilveiraTools.ExtensionMethods
{
    /// <summary>
    /// Contains extension methods for <see cref="NpgsqlConnection"/>.
    /// </summary>
    public static class NpgsqlConnectionExtensions
    {
        /// <summary>
        /// Executes a query, returning the data typed as <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">Type of results to return.</typeparam>
        /// <param name="connection">Connection to query on.</param>
        /// <param name="sql">Text query to be executed.</param>
        /// <param name="parameters">The parameters for this command.</param>
        /// <param name="cancellationToken">Cancellation token for this command.</param>
        /// <returns>
        /// A single or null instance of the supplied type; if a basic type (int, string, etc) is queried then the data from the first column is assumed,
        /// otherwise an instance is created per row, and a direct column-name === member-name mapping is assumed (case insensitive).
        /// </returns>
        public static Task<T?> QueryFirstOrDefaultAsync<T>(this NpgsqlConnection connection, string sql, DynamicParameters parameters, CancellationToken cancellationToken)
        {
            return connection.QueryFirstOrDefaultAsync<T>(new CommandDefinition(sql, parameters, cancellationToken: cancellationToken));
        }

        /// <summary>
        /// Execute a query asynchronously using Task.
        /// </summary>
        /// <typeparam name="T">Type of results to return.</typeparam>
        /// <param name="connection">Connection to query on.</param>
        /// <param name="sql">Text query to be executed.</param>
        /// <param name="parameters">The parameters for this command.</param>
        /// <param name="cancellationToken">Cancellation token for this command.</param>
        /// <returns>
        /// A sequence of data of <typeparamref name="T"/>; if a basic type (int, string, etc) is queried then the data from the first column is assumed, otherwise an instance is
        /// created per row, and a direct column-name===member-name mapping is assumed (case insensitive).
        /// </returns>
        public static Task<IEnumerable<T>> QueryAsync<T>(this NpgsqlConnection connection, string sql, DynamicParameters parameters, CancellationToken cancellationToken)
        {
            return connection.QueryAsync<T>(new CommandDefinition(sql, parameters, cancellationToken: cancellationToken));
        }
    }
}
