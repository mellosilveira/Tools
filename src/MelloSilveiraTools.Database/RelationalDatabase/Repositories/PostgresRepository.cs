using Dapper;
using MelloSilveiraTools.Core.ExtensionMethods;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Database.ExtensionMethods;
using MelloSilveiraTools.Database.RelationalDatabase.Models.Filters;
using MelloSilveiraTools.Database.RelationalDatabase.Settings;
using MelloSilveiraTools.Database.RelationalDatabase.Sql.Provider;
using MelloSilveiraTools.Database.Repositories;
using MelloSilveiraTools.Database.ResiliencePipelines;
using Npgsql;
using NpgsqlTypes;
using System.Data;
using System.Data.Common;
using System.Runtime.CompilerServices;

namespace MelloSilveiraTools.Database.RelationalDatabase.Repositories;

/// <summary>
/// Repository that contains methods to deal with Postgres database.
/// </summary>
/// <remarks>
/// <para>
/// All database operations are wrapped by <see cref="PostgresResiliencePipeline"/>, which transparently
/// retries transient failures (connection drops, deadlocks, timeouts) according to its configured policy.
/// </para>
/// <para>
/// Per-command timeouts are taken from <see cref="DatabaseSettings"/>:
/// <see cref="DatabaseSettings.UnitOperationTimeoutInSeconds"/> applies to single-row operations
/// (insert, update, delete-by-id, get-by-id, exists) and
/// <see cref="DatabaseSettings.BulkOperationTimeoutInSeconds"/> applies to batch operations
/// (bulk insert, bulk upsert).
/// </para>
/// <para>
/// SQL is generated through the injected <see cref="ISqlProvider"/>; the provider implementation memoizes
/// SQL strings per CLR type so the cost of reflection-based generation is paid only once per type for the
/// lifetime of the application.
/// </para>
/// <para>
/// SELECT queries automatically include <c>JOIN</c> clauses for every property decorated with
/// <see cref="Attributes.ForeignKeyColumnAttribute"/>, using the
/// join type configured on the attribute and the metadata of the referenced entity type.
/// </para>
/// </remarks>
public class PostgresRepository(ISqlProvider sqlProvider, PostgresResiliencePipeline resiliencePipeline, DatabaseSettings databaseSettings) : IRepository
{
    /// <summary>
    /// Database settings used to configure connections and command timeouts.
    /// </summary>
    protected DatabaseSettings DatabaseSettings { get; } = databaseSettings;

    /// <inheritdoc/>
    public async Task<long> CountAsync<TEntity, TFilter>(TFilter filter, CancellationToken cancellationToken = default) where TFilter : FilterBase
    {
        (string? sqlWhereClause, DynamicParameters? parameters) = filter.BuildWhereClauseAndParameters();
        string sql = sqlProvider.GetCountSql<TEntity>().Replace("#WHERE", sqlWhereClause);

        return await resiliencePipeline.ExecuteAsync(async _ =>
        {
            await using NpgsqlConnection connection = await GetNewOpenedConnectionAsync(cancellationToken).ConfigureAwait(false);
            return await connection.ExecuteScalarAsync<long>(sql, parameters, cancellationToken).ConfigureAwait(false);
        });
    }

    /// <inheritdoc/>
    public async Task<bool> ExistAsync<TEntity>(long id, CancellationToken cancellationToken = default)
    {
        string sql = sqlProvider.GetExistByPrimaryKeySql<TEntity>();

        DynamicParameters parameters = new();
        parameters.Add("@Id", id, DbType.Int64);

        return await resiliencePipeline.ExecuteAsync(async _ =>
        {
            await using NpgsqlConnection connection = await GetNewOpenedConnectionAsync(cancellationToken).ConfigureAwait(false);
            long count = await connection.ExecuteScalarAsync<long>(sql, parameters, DatabaseSettings.UnitOperationTimeoutInSeconds, cancellationToken).ConfigureAwait(false);
            return count > 0;
        });
    }

    /// <inheritdoc/>
    public async Task<bool> ExistAsync<TEntity, TFilter>(TFilter filter, CancellationToken cancellationToken = default)
        where TFilter : FilterBase
    {
        (string? sqlWhereClause, DynamicParameters? parameters) = filter.BuildWhereClauseAndParameters();
        string sql = sqlProvider.GetCountSql<TEntity>().Replace("#WHERE", sqlWhereClause);

        return await resiliencePipeline.ExecuteAsync(async _ =>
        {
            await using NpgsqlConnection connection = await GetNewOpenedConnectionAsync(cancellationToken).ConfigureAwait(false);
            long count = await connection.ExecuteScalarAsync<long>(sql, parameters, cancellationToken).ConfigureAwait(false);
            return count > 0;
        });
    }

    /// <inheritdoc/>
    public async Task DeleteAllAsync<TEntity>(CancellationToken cancellationToken = default)
    {
        string sql = sqlProvider.GetDeleteSql<TEntity>().Replace("#WHERE", null);

        await resiliencePipeline.ExecuteAsync(async _ =>
        {
            await using NpgsqlConnection connection = await GetNewOpenedConnectionAsync(cancellationToken).ConfigureAwait(false);
            await using NpgsqlCommand command = new(sql, connection);
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        });
    }

    /// <inheritdoc/>
    public async Task DeleteAsync<TEntity>(long id, CancellationToken cancellationToken = default)
    {
        string sql = sqlProvider.GetDeleteByPrimaryKeySql<TEntity>();

        await resiliencePipeline.ExecuteAsync(async _ =>
        {
            await using NpgsqlConnection connection = await GetNewOpenedConnectionAsync(cancellationToken).ConfigureAwait(false);
            await using NpgsqlCommand command = new(sql, connection) { CommandTimeout = DatabaseSettings.UnitOperationTimeoutInSeconds };

            command.Parameters.AddWithValue("@Id", NpgsqlDbType.Bigint, id);
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        });
    }

    /// <inheritdoc/>
    public async Task DeleteAsync<TEntity, TFilter>(TFilter filter, CancellationToken cancellationToken = default)
    {
        (string? sqlWhereClause, List<NpgsqlParameter>? parameters) = filter.BuildWhereClauseAndNpgsqlParameters();
        string sql = sqlProvider.GetDeleteSql<TEntity>().Replace("#WHERE", sqlWhereClause);

        await resiliencePipeline.ExecuteAsync(async _ =>
        {
            await using NpgsqlConnection connection = await GetNewOpenedConnectionAsync(cancellationToken).ConfigureAwait(false);
            await using NpgsqlCommand command = new(sql, connection);

            foreach (var parameter in parameters!)
            {
                command.Parameters.Add(parameter);
            }

            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        });
    }

    /// <inheritdoc/>
    public async Task<TEntity?> GetFirstOrDefaultAsync<TEntity, TFilter>(TFilter filter, SortOrder sortOrder = SortOrder.Asc, CancellationToken cancellationToken = default)
        where TFilter : FilterBase
    {
        (string? sqlWhereClause, DynamicParameters? parameters) = filter.BuildWhereClauseAndParameters();
        string sql = sqlProvider.GetSelectSql<TEntity>()
            .Replace("#WHERE", sqlWhereClause)
            .Replace("#LIMIT", "LIMIT 1")
            .Replace("#ORDERBY", sortOrder.ToNpgsqlString())
            .Remove("#OFFSET");

        return await resiliencePipeline.ExecuteAsync(async _ =>
        {
            await using NpgsqlConnection connection = await GetNewOpenedConnectionAsync(cancellationToken).ConfigureAwait(false);
            return await connection.QueryFirstOrDefaultAsync<TEntity>(sql, parameters, DatabaseSettings.UnitOperationTimeoutInSeconds, cancellationToken).ConfigureAwait(false);
        });
    }

    /// <inheritdoc/>
    public async Task<TEntity?> GetAsync<TEntity>(long id, CancellationToken cancellationToken = default)
    {
        string sql = sqlProvider.GetSelectByPrimaryKeySql<TEntity>();

        DynamicParameters parameters = new();
        parameters.Add("@Id", id, DbType.Int64);

        return await resiliencePipeline.ExecuteAsync(async _ =>
        {
            await using NpgsqlConnection connection = await GetNewOpenedConnectionAsync(cancellationToken).ConfigureAwait(false);
            return await connection.QueryFirstOrDefaultAsync<TEntity>(sql, parameters, DatabaseSettings.UnitOperationTimeoutInSeconds, cancellationToken).ConfigureAwait(false);
        });
    }

    /// <inheritdoc/>
    public IAsyncEnumerable<TEntity> GetAsync<TEntity, TFilter>(TFilter filter, Pagination? pagination = null, CancellationToken cancellationToken = default)
        where TEntity : class, new()
        where TFilter : FilterBase
        => StreamAsync<TEntity, TFilter>(sqlProvider.GetSelectSql<TEntity>(), filter, pagination, cancellationToken);

    /// <inheritdoc/>
    public IAsyncEnumerable<TEntity> GetDistinctAsync<TEntity, TFilter>(TFilter filter, Pagination? pagination = null, CancellationToken cancellationToken = default)
        where TEntity : class, new()
        where TFilter : FilterBase
        => StreamAsync<TEntity, TFilter>(sqlProvider.GetSelectDistinctSql<TEntity>(), filter, pagination, cancellationToken);

    /// <inheritdoc/>
    public async Task<TEntity?> GetByUniqueColumnAsync<TEntity>(object value, CancellationToken cancellationToken = default)
    {
        string sql = sqlProvider.GetSelectByUniqueColumnSql<TEntity>();

        DynamicParameters parameters = new();
        parameters.Add("@UniqueColumnValue", value);

        return await resiliencePipeline.ExecuteAsync(async _ =>
        {
            await using NpgsqlConnection connection = await GetNewOpenedConnectionAsync(cancellationToken).ConfigureAwait(false);
            return await connection
                .QueryFirstOrDefaultAsync<TEntity>(sql, parameters, DatabaseSettings.UnitOperationTimeoutInSeconds, cancellationToken)
                .ConfigureAwait(false);
        });
    }

    private IAsyncEnumerable<TEntity> StreamAsync<TEntity, TFilter>(string baseSql, TFilter filter, Pagination? pagination, CancellationToken cancellationToken)
        where TEntity : class, new()
        where TFilter : FilterBase
    {
        (string? whereClause, DynamicParameters? parameters) = filter.BuildWhereClauseAndParameters();
        string sql = baseSql
            .Replace("#WHERE", whereClause)
            .Replace("#ORDERBY", pagination?.SortOrder?.ToNpgsqlString())
            .Replace("#LIMIT", pagination?.Limit is null ? null : $"LIMIT {pagination.Limit}")
            .Replace("#OFFSET", pagination?.Offset is null ? null : $"OFFSET {pagination.Offset}");
        return GetAsync<TEntity>(sql, parameters, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<long> InsertAsync<TEntity>(TEntity entity, CancellationToken cancellationToken = default)
    {
        string sql = sqlProvider.GetInsertSql<TEntity>();
        IEnumerable<NpgsqlParameter> parameters = entity.BuildParameters(useDeclaredProperties: true);

        return await resiliencePipeline.ExecuteAsync(async _ =>
        {
            await using NpgsqlConnection connection = await GetNewOpenedConnectionAsync(cancellationToken).ConfigureAwait(false);
            await using NpgsqlCommand command = new(sql, connection) { CommandTimeout = DatabaseSettings.UnitOperationTimeoutInSeconds };
            object? insertedIdentifier = await command
                .SetCommandParameters(parameters)
                .ExecuteScalarAsync(cancellationToken)
                .ConfigureAwait(false);

            return Convert.ToInt64(insertedIdentifier!);
        });
    }

    /// <inheritdoc/>
    public async Task<long[]> InsertAsync<TEntity>(TEntity[] entities, CancellationToken cancellationToken = default)
    {
        string sql = sqlProvider.GetBulkInsertSql<TEntity>(entities.Length);
        IEnumerable<NpgsqlParameter> parameters = entities.BuildParametersFromCollection();

        return await resiliencePipeline.ExecuteAsync(async _ =>
        {
            await using NpgsqlConnection connection = await GetNewOpenedConnectionAsync(cancellationToken).ConfigureAwait(false);
            await using NpgsqlCommand command = new(sql, connection) { CommandTimeout = DatabaseSettings.BulkOperationTimeoutInSeconds };
            await using NpgsqlDataReader reader = await command
                .SetCommandParameters(parameters)
                .ExecuteReaderAsync(cancellationToken)
                .ConfigureAwait(false);

            long[] ids = new long[entities.Length];
            int i = 0;
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                ids[i++] = reader.GetInt64(0);

            return ids;
        });
    }

    /// <inheritdoc/>
    public async Task<Result<long>> TryInsertAsync<TEntity>(TEntity entity, CancellationToken cancellationToken = default)
    {
        string sql = sqlProvider.GetTryInsertSql<TEntity>();
        IEnumerable<NpgsqlParameter> parameters = entity.BuildParameters(useDeclaredProperties: true);

        return await resiliencePipeline.ExecuteAsync(async _ =>
        {
            await using NpgsqlConnection connection = await GetNewOpenedConnectionAsync(cancellationToken).ConfigureAwait(false);
            await using NpgsqlCommand command = new(sql, connection) { CommandTimeout = DatabaseSettings.UnitOperationTimeoutInSeconds };
            await using NpgsqlDataReader reader = await command
                .SetCommandParameters(parameters)
                .ExecuteReaderAsync(cancellationToken)
                .ConfigureAwait(false);

            if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                return Result.CreateUnknownError();

            long id = reader.GetInt64(0);
            bool inserted = reader.GetBoolean(1);
            return inserted ? Result.CreateSuccessOk(id) : Result.CreateConflict(id);
        });
    }

    /// <inheritdoc/>
    public async Task<long> UpsertAsync<TEntity>(TEntity entity, CancellationToken cancellationToken = default)
    {
        string sql = sqlProvider.GetInsertSql<TEntity>();
        IEnumerable<NpgsqlParameter> parameters = entity.BuildParameters(useDeclaredProperties: true);

        return await resiliencePipeline.ExecuteAsync(async _ =>
        {
            await using NpgsqlConnection connection = await GetNewOpenedConnectionAsync(cancellationToken).ConfigureAwait(false);
            await using NpgsqlCommand command = new(sql, connection) { CommandTimeout = DatabaseSettings.UnitOperationTimeoutInSeconds };
            object? upsertedId = await command
                .SetCommandParameters(parameters)
                .ExecuteScalarAsync(cancellationToken)
                .ConfigureAwait(false);

            return Convert.ToInt64(upsertedId!);
        });
    }

    /// <inheritdoc/>
    public async Task<long[]> UpsertAsync<TEntity, TFilter>(TEntity[] entities, TFilter filter, CancellationToken cancellationToken = default)
    {
        (string? sqlWhereClause, List<NpgsqlParameter> deleteParameters) = filter.BuildWhereClauseAndNpgsqlParameters();
        string deleteSql = sqlProvider.GetDeleteSql<TEntity>().Replace("#WHERE", sqlWhereClause);

        IEnumerable<NpgsqlParameter> insertParameters = entities.BuildParametersFromCollection();
        string insertSql = sqlProvider.GetBulkInsertSql<TEntity>(entities.Length);

        string sql = BuildUpsertSql(deleteSql, insertSql);

        return await resiliencePipeline.ExecuteAsync(async _ =>
        {
            await using NpgsqlConnection connection = await GetNewOpenedConnectionAsync(cancellationToken).ConfigureAwait(false);
            await using NpgsqlCommand command = new(sql, connection) { CommandTimeout = DatabaseSettings.BulkOperationTimeoutInSeconds };
            await using NpgsqlDataReader reader = await command
                .SetCommandParameters(deleteParameters)
                .SetCommandParameters(insertParameters)
                .ExecuteReaderAsync(cancellationToken)
                .ConfigureAwait(false);

            List<long> ids = [];
            do
            {
                while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                    ids.Add(reader.GetInt64(0));
            } while (await reader.NextResultAsync(cancellationToken).ConfigureAwait(false));

            return ids.ToArray();
        });
    }

    /// <inheritdoc/>
    public async Task<bool> TryUpdateAsync<TEntity>(TEntity entity, CancellationToken cancellationToken = default)
    {
        string sql = sqlProvider.GetUpdateByPrimaryKeySql<TEntity>();
        IEnumerable<NpgsqlParameter> parameters = entity.BuildParameters();

        return await resiliencePipeline.ExecuteAsync(async _ =>
        {
            await using NpgsqlConnection connection = await GetNewOpenedConnectionAsync(cancellationToken).ConfigureAwait(false);
            await using NpgsqlCommand command = new(sql, connection) { CommandTimeout = DatabaseSettings.UnitOperationTimeoutInSeconds };
            int affectedRows = await command
                .SetCommandParameters(parameters)
                .ExecuteNonQueryAsync(cancellationToken)
                .ConfigureAwait(false);
            return affectedRows > 0;
        });
    }

    /// <summary>
    /// Streams rows returned by <paramref name="sql"/> and materializes each of them into a <typeparamref name="TEntity"/>.
    /// </summary>
    /// <param name="sql">SQL statement to execute.</param>
    /// <param name="parameters">Parameters to bind to the statement, if any.</param>
    /// <param name="cancellationToken">Token used to cancel the streaming operation.</param>
    protected async IAsyncEnumerable<TEntity> GetAsync<TEntity>(string sql, DynamicParameters? parameters, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        where TEntity : class, new()
    {
        await using NpgsqlConnection connection = await GetNewOpenedConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using DbDataReader dataReader = await connection.ExecuteReaderAsync(sql, parameters, cancellationToken).ConfigureAwait(false);
        while (await dataReader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            if (!await dataReader.IsDBNullAsync(0, cancellationToken).ConfigureAwait(false))
                yield return dataReader.ConvertTo<TEntity>();
        }
    }

    private static string BuildUpsertSql(string deleteSql, string insertSql)
    {
        using SpanStringBuilder ssb = new();
        ssb.Append(deleteSql).AppendLine(';').AppendLine(insertSql);
        return ssb.ToString();
    }

    /// <summary>
    /// Creates and opens a new Npgsql connection using the configured connection string.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the asynchronous open operation.</param>
    /// <returns>An opened <see cref="NpgsqlConnection"/>.</returns>
    protected async Task<NpgsqlConnection> GetNewOpenedConnectionAsync(CancellationToken cancellationToken = default)
    {
        NpgsqlConnection connection = new(DatabaseSettings.ConnectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        return connection;
    }
}
