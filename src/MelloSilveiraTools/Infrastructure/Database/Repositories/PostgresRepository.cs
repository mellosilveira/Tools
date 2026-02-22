using Dapper;
using MelloSilveiraTools.Domain.Models;
using MelloSilveiraTools.Domain.Repositories;
using MelloSilveiraTools.ExtensionMethods;
using MelloSilveiraTools.Infrastructure.Database.Models.Filters;
using MelloSilveiraTools.Infrastructure.Database.Settings;
using MelloSilveiraTools.Infrastructure.Database.Sql.Provider;
using MelloSilveiraTools.Infrastructure.ResiliencePipelines;
using Npgsql;
using NpgsqlTypes;
using System.Data;
using System.Data.Common;
using static Dapper.SqlMapper;

namespace MelloSilveiraTools.Infrastructure.Database.Repositories;

/// <summary>
/// Repository that contains methods to deal with Postgres database.
/// </summary>
public class PostgresRepository(ISqlProvider sqlProvider, PostgresResiliencePipeline resiliencePipeline, DatabaseSettings databaseSettings) : IRepository
{
    protected DatabaseSettings DatabaseSettings { get; } = databaseSettings;

    /// <inheritdoc/>
    public async Task<long> CountAsync<TEntity, TFilter>(TFilter filter, CancellationToken cancellationToken = default)
        where TFilter : FilterBase
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
            long count = await connection.ExecuteScalarAsync<long>(sql, parameters, cancellationToken).ConfigureAwait(false);
            return count > 0;
        });
    }

    /// <inheritdoc/>
    public async Task<bool> ExistAsync<TEntity, TFilter>(TFilter filter, CancellationToken cancellationToken = default)
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
            await using NpgsqlCommand command = new(sql, connection);

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
            return await connection.QueryFirstOrDefaultAsync<TEntity>(sql, parameters, cancellationToken).ConfigureAwait(false);
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
            await using NpgsqlCommand command = new(sql, connection);
            return await connection.QueryFirstOrDefaultAsync<TEntity>(sql, parameters, cancellationToken).ConfigureAwait(false);
        });
    }

    /// <inheritdoc/>
    public IAsyncEnumerable<TEntity> GetAsync<TEntity, TFilter>(TFilter filter, Pagination? pagination = null, CancellationToken cancellationToken = default)
        where TEntity : class, new()
        where TFilter : FilterBase
    {
        (string? sqlWhereClause, DynamicParameters? parameters) = filter.BuildWhereClauseAndParameters();
        string sql = sqlProvider.GetSelectSql<TEntity>()
            .Replace("#WHERE", sqlWhereClause)
            .Replace("#ORDERBY", pagination?.SortOrder?.ToNpgsqlString())
            .Replace("#LIMIT", pagination?.Limit is null ? null : $"LIMIT {pagination.Limit}")
            .Replace("#OFFSET", pagination?.Offset is null ? null : $"OFFSET {pagination.Offset}");

        return GetAsync<TEntity>(sql, parameters, cancellationToken);
    }

    /// <inheritdoc/>
    public IAsyncEnumerable<TEntity> GetDistinctAsync<TEntity, TFilter>(TFilter filter, Pagination? pagination = null, CancellationToken cancellationToken = default)
        where TEntity : class, new()
        where TFilter : FilterBase
    {
        (string? sqlWhereClause, DynamicParameters? parameters) = filter.BuildWhereClauseAndParameters();
        string sql = sqlProvider.GetSelectDistinctSql<TEntity>()
            .Replace("#WHERE", sqlWhereClause)
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
            await using NpgsqlCommand command = new(sql, connection) { CommandTimeout = DatabaseSettings.UnitOperationTimeoutInMilliseconds };
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
            await using NpgsqlCommand command = new(sql, connection) { CommandTimeout = DatabaseSettings.UnitOperationTimeoutInMilliseconds };
            object? insertedIds = await command
                .SetCommandParameters(parameters)
                .ExecuteScalarAsync(cancellationToken)
                .ConfigureAwait(false);

            return (long[])insertedIds!;
        });
    }

    /// <inheritdoc/>
    public async Task<long[]> UpsertAsync<TEntity, TFilter>(TEntity[] entities, TFilter filter, CancellationToken cancellationToken = default)
    {
        (string? sqlWhereClause, List<NpgsqlParameter> deleteParameters) = filter.BuildWhereClauseAndNpgsqlParameters();
        string deleteSql = sqlProvider.GetDeleteSql<TEntity>().Replace("#WHERE", sqlWhereClause);

        IEnumerable<NpgsqlParameter> insertParameters = entities.BuildParametersFromCollection();
        string insertSql = sqlProvider.GetBulkInsertSql<TEntity>(entities.Length);

        string sql = new SpanStringBuilder()
            .Append(deleteSql)
            .AppendLine(';')
            .AppendLine(insertSql)
            .ToString();

        return await resiliencePipeline.ExecuteAsync(async _ =>
        {
            await using NpgsqlConnection connection = await GetNewOpenedConnectionAsync(cancellationToken).ConfigureAwait(false);
            await using NpgsqlCommand command = new(sql, connection) { CommandTimeout = DatabaseSettings.UnitOperationTimeoutInMilliseconds };
            object? insertedIds = await command
                .SetCommandParameters(deleteParameters)
                .SetCommandParameters(insertParameters)
                .ExecuteScalarAsync(cancellationToken)
                .ConfigureAwait(false);

            return (long[])insertedIds!;
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
            await using NpgsqlCommand command = new(sql, connection) { CommandTimeout = DatabaseSettings.UnitOperationTimeoutInMilliseconds };
            int affectedRows = await command
                .SetCommandParameters(parameters)
                .ExecuteNonQueryAsync(cancellationToken)
                .ConfigureAwait(false);
            return affectedRows > 0;
        });
    }

    protected async IAsyncEnumerable<TEntity> GetAsync<TEntity>(string sql, DynamicParameters? parameters, CancellationToken cancellationToken = default)
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

    protected async Task<NpgsqlConnection> GetNewOpenedConnectionAsync(CancellationToken cancellationToken = default)
    {
        NpgsqlConnection connection = new(DatabaseSettings.ConnectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        return connection;
    }
}
