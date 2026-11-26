using Npgsql;
using NpgsqlTypes;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace MelloSilveiraTools.Database.ExtensionMethods;

/// <summary>
/// Contains fluent command composition, RLS scoping, and Native AOT-compatible execution extensions for <see cref="NpgsqlBatch"/>.
/// </summary>
public static class NpgsqlBatchExtensions
{
    private const string SetUserContextSql = "SELECT set_config('app.current_user_id', @UserId, true);";

    extension(NpgsqlBatch batch)
    {
        /// <summary>
        /// Appends a command executing <paramref name="sql"/> with optional <paramref name="parameters"/> to <paramref name="batch"/>.
        /// </summary>
        /// <param name="sql">PostgreSQL statement to append to the batch.</param>
        /// <param name="parameters">Optional parameters to bind to the command.</param>
        /// <param name="cloneParameters">
        /// When <see langword="true"/>, binds detached <see cref="NpgsqlParameter.Clone"/> copies for retry-safe execution.
        /// </param>
        /// <returns>The same <see cref="NpgsqlBatch"/> instance for call chaining.</returns>
        public NpgsqlBatch AddCommand(string sql, IEnumerable<NpgsqlParameter>? parameters = null, bool cloneParameters = false)
        {
            batch.BatchCommands.Add(new NpgsqlBatchCommand(sql).SetCommandParameters(parameters, cloneParameters));
            return batch;
        }

        /// <summary>
        /// Appends a transaction-local PostgreSQL RLS context command
        /// (<c>SELECT set_config('app.current_user_id', @UserId, true)</c>) to <paramref name="batch"/>.
        /// PostgreSQL resets <c>app.current_user_id</c> automatically at the end of the batch's implicit transaction block.
        /// </summary>
        /// <param name="userId">Active user identifier, or <see langword="null"/> for anonymous (public) access.</param>
        /// <returns>The same <see cref="NpgsqlBatch"/> instance for call chaining.</returns>
        public NpgsqlBatch AddUserContext(long? userId)
        {
            string userIdValue = userId.HasValue ? userId.Value.ToString(CultureInfo.InvariantCulture) : string.Empty;

            NpgsqlBatchCommand contextCommand = new(SetUserContextSql);
            contextCommand.Parameters.AddWithValue("@UserId", NpgsqlDbType.Text, userIdValue);
            batch.BatchCommands.Add(contextCommand);
            return batch;
        }

        /// <summary>
        /// Executes <paramref name="batch"/>, advances to <paramref name="resultSetIndex"/>, and converts
        /// the first column of the first row to <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">Expected scalar return type.</typeparam>
        /// <param name="resultSetIndex">Zero-based index of the target result set (e.g. <c>1</c> when preceded by <c>AddUserContext</c>).</param>
        /// <param name="cancellationToken">Token used to cancel the asynchronous execution.</param>
        /// <returns>The converted scalar value, or <c>default</c> when the target result set is empty or SQL <c>NULL</c>.</returns>
        public async Task<T?> ExecuteScalarAsync<T>(int resultSetIndex = 0, CancellationToken cancellationToken = default)
        {
            await using NpgsqlDataReader reader = await batch.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            return await reader.ReadScalarAsync<T>(resultSetIndex, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Executes <paramref name="batch"/>, advances to <paramref name="resultSetIndex"/>, and materializes
        /// the first row as <typeparamref name="T"/>, or returns <see langword="null"/> when no rows match.
        /// </summary>
        /// <typeparam name="T">Target entity type whose public properties are preserved for trimming and Native AOT.</typeparam>
        /// <param name="resultSetIndex">Zero-based index of the target result set (e.g. <c>1</c> when preceded by <c>AddUserContext</c>).</param>
        /// <param name="cancellationToken">Token used to cancel the asynchronous execution.</param>
        /// <returns>The first mapped entity, or <see langword="null"/>.</returns>
        public async Task<T?> QueryFirstOrDefaultAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(int resultSetIndex = 0, CancellationToken cancellationToken = default) where T : class, new()
        {
            await using NpgsqlDataReader reader = await batch.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            return await reader.ReadFirstOrDefaultAsync<T>(resultSetIndex, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Executes <paramref name="batch"/>, advances to <paramref name="resultSetIndex"/>, and streams
        /// materialized <typeparamref name="T"/> instances while deterministically disposing the underlying reader.
        /// </summary>
        /// <typeparam name="T">Target entity type whose public properties are preserved for trimming and Native AOT.</typeparam>
        /// <param name="resultSetIndex">Zero-based index of the target result set (e.g. <c>1</c> when preceded by <c>AddUserContext</c>).</param>
        /// <param name="cancellationToken">Token used to cancel the asynchronous stream.</param>
        /// <returns>An asynchronous stream of mapped entities.</returns>
        public async IAsyncEnumerable<T> StreamAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(int resultSetIndex = 0, [EnumeratorCancellation] CancellationToken cancellationToken = default) where T : class, new()
        {
            await using NpgsqlDataReader reader = await batch.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            await foreach (T entity in reader.StreamAsync<T>(resultSetIndex, cancellationToken).ConfigureAwait(false))
                yield return entity;
        }

        /// <summary>
        /// Executes <paramref name="batch"/> as a non-query and returns the affected row count reported for
        /// the command at <paramref name="commandIndex"/>.
        /// </summary>
        /// <param name="commandIndex">Zero-based index of the target command inside <see cref="NpgsqlBatch.BatchCommands"/>.</param>
        /// <param name="cancellationToken">Token used to cancel the asynchronous execution.</param>
        /// <returns>The number of rows affected by the command at <paramref name="commandIndex"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="commandIndex"/> is outside <see cref="NpgsqlBatch.BatchCommands"/>.</exception>
        public async Task<ulong> ExecuteNonQueryRowsAsync(int commandIndex = 0, CancellationToken cancellationToken = default)
        {
            await batch.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            return batch.BatchCommands[commandIndex].Rows;
        }
    }
}
