using Dapper;
using MelloSilveiraTools.Database.ExtensionMethods;
using Npgsql;
using System.Data;
using System.Data.Common;

namespace MelloSilveiraTools.Database.ExtensionMethods;

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
    public static Task<T?> QueryFirstOrDefaultAsync<T>(this NpgsqlConnection connection, string sql, DynamicParameters? parameters, CancellationToken cancellationToken) 
        => connection.QueryFirstOrDefaultAsync<T>(new CommandDefinition(sql, parameters, cancellationToken: cancellationToken));

    /// <summary>
    /// Executes a query, returning the data typed as <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">Type of results to return.</typeparam>
    /// <param name="connection">Connection to query on.</param>
    /// <param name="sql">Text query to be executed.</param>
    /// <param name="parameters">The parameters for this command.</param>
    /// <param name="commandTimeout">Number of seconds before command execution timeout.</param>
    /// <param name="cancellationToken">Cancellation token for this command.</param>
    /// <returns>
    /// A single or null instance of the supplied type; if a basic type (int, string, etc) is queried then the data from the first column is assumed,
    /// otherwise an instance is created per row, and a direct column-name === member-name mapping is assumed (case insensitive).
    /// </returns>
    public static Task<T?> QueryFirstOrDefaultAsync<T>(this NpgsqlConnection connection, string sql, DynamicParameters? parameters, int commandTimeout, CancellationToken cancellationToken) 
        => connection.QueryFirstOrDefaultAsync<T>(new CommandDefinition(sql, parameters, commandTimeout: commandTimeout, cancellationToken: cancellationToken));

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
        => connection.QueryAsync<T>(new CommandDefinition(sql, parameters, cancellationToken: cancellationToken));

    /// <summary>
    /// Execute parameterized SQL that selects a single value.
    /// </summary>
    /// <typeparam name="T">The type to return.</typeparam>
    /// <param name="connection">The connection to execute on.</param>
    /// <param name="sql">The SQL to execute.</param>
    /// <param name="parameters">The parameters to use for this command.</param>
    /// <param name="cancellationToken">Cancellation token for this command.</param>
    /// <returns>The first cell returned, as <typeparamref name="T"/>.</returns>
    public static Task<T?> ExecuteScalarAsync<T>(this NpgsqlConnection connection, string sql, DynamicParameters? parameters, CancellationToken cancellationToken)
        => connection.ExecuteScalarAsync<T>(new CommandDefinition(sql, parameters, cancellationToken: cancellationToken));

    /// <summary>
    /// Execute parameterized SQL that selects a single value.
    /// </summary>
    /// <typeparam name="T">The type to return.</typeparam>
    /// <param name="connection">The connection to execute on.</param>
    /// <param name="sql">The SQL to execute.</param>
    /// <param name="parameters">The parameters to use for this command.</param>
    /// <param name="commandTimeout">Number of seconds before command execution timeout.</param>
    /// <param name="cancellationToken">Cancellation token for this command.</param>
    /// <returns>The first cell returned, as <typeparamref name="T"/>.</returns>
    public static Task<T?> ExecuteScalarAsync<T>(this NpgsqlConnection connection, string sql, DynamicParameters? parameters, int commandTimeout, CancellationToken cancellationToken)
        => connection.ExecuteScalarAsync<T>(new CommandDefinition(sql, parameters, commandTimeout: commandTimeout, cancellationToken: cancellationToken));

    /// <summary>
    /// Execute parameterized SQL and return an <see cref="IDataReader"/>.
    /// </summary>
    /// <param name="connection">The connection to execute on.</param>
    /// <param name="sql">The SQL to execute.</param>
    /// <param name="parameters">The parameters to use for this command.</param>
    /// <param name="cancellationToken">Cancellation token for this command.</param>
    /// <returns>An <see cref="IDataReader"/> that can be used to iterate over the results of the SQL query.</returns>
    /// <remarks>
    /// This is typically used when the results of a query are not processed by Dapper, for example, used to fill a <see cref="DataTable"/>
    /// or <see cref="T:DataSet"/>.
    /// </remarks>
    public static Task<DbDataReader> ExecuteReaderAsync(this NpgsqlConnection connection, string sql, DynamicParameters? parameters, CancellationToken cancellationToken)
        => connection.ExecuteReaderAsync(new CommandDefinition(sql, parameters, cancellationToken: cancellationToken));
}
