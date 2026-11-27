using Npgsql;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace MelloSilveiraTools.Database.ExtensionMethods;

/// <summary>
/// Contains pure-Npgsql, Native AOT-compatible query and scalar execution extensions for <see cref="NpgsqlConnection"/>.
/// </summary>
public static class NpgsqlConnectionExtensions
{
    /// <summary>
    /// Contains extension methods for <see cref="NpgsqlConnection"/> instances.
    /// </summary>
    /// <param name="connection">Opened PostgreSQL connection to execute statements on.</param>
    extension(NpgsqlConnection connection)
    {
        /// <summary>
        /// Executes a parameterized SQL query and materializes the first row as <typeparamref name="T"/>,
        /// or returns <see langword="null"/> when no rows match.
        /// </summary>
        /// <typeparam name="T">Target entity type whose public properties are preserved for trimming and Native AOT.</typeparam>
        /// <param name="sql">Parameterized PostgreSQL statement to execute.</param>
        /// <param name="parameters">Optional <see cref="NpgsqlParameter"/> collection to bind to the command.</param>
        /// <param name="commandTimeout">Optional command timeout in seconds.</param>
        /// <param name="transaction">Optional active PostgreSQL transaction enclosing the command.</param>
        /// <param name="cancellationToken">Token used to cancel the asynchronous query.</param>
        /// <returns>The first mapped entity, or <see langword="null"/> if no rows are returned.</returns>
        public async Task<T?> QueryFirstOrDefaultAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(
            string sql,
            IEnumerable<NpgsqlParameter>? parameters = null,
            int? commandTimeout = null,
            NpgsqlTransaction? transaction = null,
            CancellationToken cancellationToken = default)
            where T : class, new()
        {
            await using NpgsqlCommand command = CreateCommand(connection, sql, parameters, commandTimeout, transaction);
            await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, cancellationToken).ConfigureAwait(false);
            return await reader.ReadFirstOrDefaultAsync<T>(0, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Executes a parameterized SQL query and materializes every returned row into a list of <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">Target entity type whose public properties are preserved for trimming and Native AOT.</typeparam>
        /// <param name="sql">Parameterized PostgreSQL statement to execute.</param>
        /// <param name="parameters">Optional <see cref="NpgsqlParameter"/> collection to bind to the command.</param>
        /// <param name="commandTimeout">Optional command timeout in seconds.</param>
        /// <param name="transaction">Optional active PostgreSQL transaction enclosing the command.</param>
        /// <param name="cancellationToken">Token used to cancel the asynchronous query.</param>
        /// <returns>A list containing all mapped entities.</returns>
        public async Task<List<T>> QueryAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(
            string sql,
            IEnumerable<NpgsqlParameter>? parameters = null,
            int? commandTimeout = null,
            NpgsqlTransaction? transaction = null,
            CancellationToken cancellationToken = default)
            where T : class, new()
        {
            List<T> items = [];
            await foreach (T item in connection.StreamAsync<T>(sql, parameters, commandTimeout, transaction, cancellationToken).ConfigureAwait(false))
                items.Add(item);

            return items;
        }

        /// <summary>
        /// Streams rows returned by a parameterized SQL query and materializes each row as <typeparamref name="T"/>,
        /// deterministically disposing both the <see cref="NpgsqlCommand"/> and <see cref="NpgsqlDataReader"/>.
        /// </summary>
        /// <typeparam name="T">Target entity type whose public properties are preserved for trimming and Native AOT.</typeparam>
        /// <param name="sql">Parameterized PostgreSQL statement to execute.</param>
        /// <param name="parameters">Optional <see cref="NpgsqlParameter"/> collection to bind to the command.</param>
        /// <param name="commandTimeout">Optional command timeout in seconds.</param>
        /// <param name="transaction">Optional active PostgreSQL transaction enclosing the command.</param>
        /// <param name="cancellationToken">Token used to cancel the asynchronous stream.</param>
        /// <returns>An asynchronous stream of mapped entities.</returns>
        public async IAsyncEnumerable<T> StreamAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(
            string sql,
            IEnumerable<NpgsqlParameter>? parameters = null,
            int? commandTimeout = null,
            NpgsqlTransaction? transaction = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
            where T : class, new()
        {
            await using NpgsqlCommand command = CreateCommand(connection, sql, parameters, commandTimeout, transaction);
            await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            await foreach (T item in reader.StreamAsync<T>(0, cancellationToken).ConfigureAwait(false))
                yield return item;
        }

        /// <summary>
        /// Executes a parameterized SQL query and converts the first column of the first row to <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">Expected scalar return type.</typeparam>
        /// <param name="sql">Parameterized PostgreSQL statement to execute.</param>
        /// <param name="parameters">Optional <see cref="NpgsqlParameter"/> collection to bind to the command.</param>
        /// <param name="commandTimeout">Optional command timeout in seconds.</param>
        /// <param name="transaction">Optional active PostgreSQL transaction enclosing the command.</param>
        /// <param name="cancellationToken">Token used to cancel the asynchronous command.</param>
        /// <returns>The converted scalar value, or <c>default</c> when the query returns no rows or SQL <c>NULL</c>.</returns>
        public async Task<T?> ExecuteScalarAsync<T>(
            string sql,
            IEnumerable<NpgsqlParameter>? parameters = null,
            int? commandTimeout = null,
            NpgsqlTransaction? transaction = null,
            CancellationToken cancellationToken = default)
        {
            await using NpgsqlCommand command = CreateCommand(connection, sql, parameters, commandTimeout, transaction);
            object? rawValue = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            return NpgsqlDataReaderExtensions.ConvertScalar<T>(rawValue);
        }
    }

    private static NpgsqlCommand CreateCommand(NpgsqlConnection connection, string sql, IEnumerable<NpgsqlParameter>? parameters, int? commandTimeout, NpgsqlTransaction? transaction)
    {
        NpgsqlCommand command = new(sql, connection, transaction);
        if (commandTimeout.HasValue)
            command.CommandTimeout = commandTimeout.Value;

        return command.SetCommandParameters(parameters);
    }
}
