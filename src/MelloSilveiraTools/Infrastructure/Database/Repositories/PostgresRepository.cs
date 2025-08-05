using Dapper;
using MelloSilveiraTools.Domain.Models;
using MelloSilveiraTools.ExtensionMethods;
using MelloSilveiraTools.Infrastructure.Database.Models.Entities;
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
public class PostgresRepository(ISqlProvider sqlProvider, PostgresResiliencePipeline resiliencePipeline, DatabaseSettings databaseSettings) : IDatabaseRepository
{
    /// <inheritdoc/>
    public async Task<long> CountAsync<TEntity, TFilter>(TFilter filter)
        where TEntity : EntityBase, new()
        where TFilter : FilterBase
    {
        (string? sqlWhereClause, DynamicParameters? parameters) = filter.BuildWhereClauseAndParameters();
        string sql = sqlProvider.GetCountSql<TEntity>().Replace("#WHERE", sqlWhereClause);

        return await resiliencePipeline.ExecuteAsync(async _ =>
        {
            await using NpgsqlConnection connection = await GetNewOpenedConnectionAsync().ConfigureAwait(false);
            return await connection
                .ExecuteScalarAsync<long>(sql, parameters, commandTimeout: databaseSettings.ConnectionTimeoutInMilliseconds)
                .ConfigureAwait(false);
        });
    }

    /// <inheritdoc/>
    public async Task<bool> ExistAsync<TEntity>(long id) where TEntity : EntityBase
    {
        string sql = sqlProvider.GetExistByPrimaryKeySql<TEntity>();

        DynamicParameters parameters = new();
        parameters.Add("@Id", id, DbType.Int64);

        return await resiliencePipeline.ExecuteAsync(async _ =>
        {
            await using NpgsqlConnection connection = await GetNewOpenedConnectionAsync().ConfigureAwait(false);
            long count = await connection
                .ExecuteScalarAsync<long>(sql, parameters, commandTimeout: databaseSettings.ConnectionTimeoutInMilliseconds)
                .ConfigureAwait(false);

            return count > 0;
        });
    }

    /// <inheritdoc/>
    public async Task<bool> ExistAsync<TEntity, TFilter>(TFilter filter) where TEntity : EntityBase
    {
        (string? sqlWhereClause, DynamicParameters? parameters) = filter.BuildWhereClauseAndParameters();
        string sql = sqlProvider.GetCountSql<TEntity>().Replace("#WHERE", sqlWhereClause);

        return await resiliencePipeline.ExecuteAsync(async _ =>
        {
            await using NpgsqlConnection connection = await GetNewOpenedConnectionAsync().ConfigureAwait(false);
            long count = await connection
                .ExecuteScalarAsync<long>(sql, parameters, commandTimeout: databaseSettings.ConnectionTimeoutInMilliseconds)
                .ConfigureAwait(false);

            return count > 0;
        });
    }

    /// <inheritdoc/>
    public async Task DeleteAllAsync<TEntity>() where TEntity : EntityBase
    {
        string sql = sqlProvider.GetDeleteSql<TEntity>().Replace("#WHERE", null);

        await resiliencePipeline.ExecuteAsync(async _ =>
        {
            CancellationToken cancellationToken = GetCancellationToken(databaseSettings.UnitOperationTimeoutInMilliseconds);

            await using NpgsqlConnection connection = await GetNewOpenedConnectionAsync(cancellationToken).ConfigureAwait(false);
            await using NpgsqlCommand command = new(sql, connection);
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        });
    }

    /// <inheritdoc/>
    public async Task DeleteAsync<TEntity>(long id) where TEntity : EntityBase
    {
        string sql = sqlProvider.GetDeleteByPrimaryKeySql<TEntity>();

        await resiliencePipeline.ExecuteAsync(async _ =>
        {
            CancellationToken cancellationToken = GetCancellationToken(databaseSettings.UnitOperationTimeoutInMilliseconds);

            await using NpgsqlConnection connection = await GetNewOpenedConnectionAsync(cancellationToken).ConfigureAwait(false);
            await using NpgsqlCommand command = new(sql, connection);

            command.Parameters.AddWithValue("@Id", NpgsqlDbType.Bigint, id);
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        });
    }

    /// <inheritdoc/>
    public async Task DeleteAsync<TEntity, TFilter>(TFilter filter) where TEntity : EntityBase
    {
        (string? sqlWhereClause, List<NpgsqlParameter>? parameters) = filter.BuildWhereClauseAndNpgsqlParameters();
        string sql = sqlProvider.GetDeleteSql<TEntity>().Replace("#WHERE", sqlWhereClause);

        await resiliencePipeline.ExecuteAsync(async _ =>
        {
            CancellationToken cancellationToken = GetCancellationToken(databaseSettings.UnitOperationTimeoutInMilliseconds);

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
    public async Task<TEntity?> GetFirstOrDefaultAsync<TEntity, TFilter>(TFilter filter)
         where TEntity : EntityBase, new()
        where TFilter : FilterBase
    {
        (string? sqlWhereClause, DynamicParameters? parameters) = filter.BuildWhereClauseAndParameters();
        string sql = sqlProvider.GetSelectSql<TEntity>()
            .Replace("#WHERE", sqlWhereClause)
            .Replace("#LIMIT", "LIMIT 1")
            .Remove("#ORDERBY")
            .Remove("#OFFSET");

        return await resiliencePipeline.ExecuteAsync(async _ =>
        {
            await using NpgsqlConnection connection = await GetNewOpenedConnectionAsync().ConfigureAwait(false);
            return await connection
                .QueryFirstOrDefaultAsync<TEntity>(sql, parameters, commandTimeout: databaseSettings.ConnectionTimeoutInMilliseconds)
                .ConfigureAwait(false);
        });
    }

    /// <inheritdoc/>
    public async Task<TEntity?> GetAsync<TEntity>(long id) where TEntity : EntityBase
    {
        string sql = sqlProvider.GetSelectByPrimaryKeySql<TEntity>();

        DynamicParameters parameters = new();
        parameters.Add("@Id", id, DbType.Int64);

        return await resiliencePipeline.ExecuteAsync(async _ =>
        {
            await using NpgsqlConnection connection = await GetNewOpenedConnectionAsync().ConfigureAwait(false);
            return await connection
                .QueryFirstOrDefaultAsync<TEntity>(sql, parameters, commandTimeout: databaseSettings.ConnectionTimeoutInMilliseconds)
                .ConfigureAwait(false);
        });
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<TEntity> GetAsync<TEntity, TFilter>(TFilter filter)
         where TEntity : EntityBase, new()
        where TFilter : FilterBase
    {
        (string? sqlWhereClause, DynamicParameters? parameters) = filter.BuildWhereClauseAndParameters();
        string sql = sqlProvider.GetSelectSql<TEntity>()
            .Replace("#WHERE", sqlWhereClause)
            .Replace("#ORDERBY", filter?.SortOrder is null ? null : $"ORDER BY 1 {filter.SortOrder.ToString()!.ToUpperInvariant()}")
            .Replace("#LIMIT", filter?.Limit is null ? null : $"LIMIT {filter?.Limit}")
            .Replace("#OFFSET", filter?.Offset is null ? null : $"OFFSET {filter?.Offset}");

        CancellationToken cancellationToken = GetCancellationToken(databaseSettings.ConnectionTimeoutInMilliseconds);

        await using NpgsqlConnection connection = await GetNewOpenedConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using DbDataReader dataReader = await connection.ExecuteReaderAsync(sql, parameters, commandTimeout: databaseSettings.ConnectionTimeoutInMilliseconds).ConfigureAwait(false);
        while (await dataReader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            if (!await dataReader.IsDBNullAsync(0, cancellationToken).ConfigureAwait(false))
                yield return dataReader.ConvertTo<TEntity>();
        }
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<TEntity> GetDistinctAsync<TEntity, TFilter>(TFilter filter)
         where TEntity : EntityBase, new()
        where TFilter : FilterBase
    {
        (string? sqlWhereClause, DynamicParameters? parameters) = filter.BuildWhereClauseAndParameters();
        string sql = sqlProvider.GetSelectDistinctSql<TEntity>()
            .Replace("#WHERE", sqlWhereClause)
            .Replace("#ORDERBY", filter?.SortOrder is null ? null : $"ORDER BY 1 {filter.SortOrder.ToString()!.ToUpperInvariant()}")
            .Replace("#LIMIT", filter?.Limit is null ? null : $"LIMIT {filter?.Limit}")
            .Replace("#OFFSET", filter?.Offset is null ? null : $"OFFSET {filter?.Offset}");

        CancellationToken cancellationToken = GetCancellationToken(databaseSettings.ConnectionTimeoutInMilliseconds);

        await using NpgsqlConnection connection = await GetNewOpenedConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using DbDataReader dataReader = await connection.ExecuteReaderAsync(sql, parameters, commandTimeout: databaseSettings.ConnectionTimeoutInMilliseconds).ConfigureAwait(false);
        while (await dataReader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            if (!await dataReader.IsDBNullAsync(0, cancellationToken).ConfigureAwait(false))
                yield return dataReader.ConvertTo<TEntity>();
        }
    }

    /// <inheritdoc/>
    public async Task<long> InsertAsync<TEntity>(TEntity entity) where TEntity : EntityBase
    {
        string sql = sqlProvider.GetInsertSql<TEntity>();
        IEnumerable<NpgsqlParameter> parameters = entity.BuildParameters(useDeclaredProperties: true);

        return await resiliencePipeline.ExecuteAsync(async _ =>
        {
            CancellationToken cancellationToken = GetCancellationToken(databaseSettings.UnitOperationTimeoutInMilliseconds);

            await using NpgsqlConnection connection = await GetNewOpenedConnectionAsync(cancellationToken).ConfigureAwait(false);
            await using NpgsqlCommand command = new(sql, connection);
            object? insertedIdentifier = await command
                .SetCommandParameters(parameters)
                .ExecuteScalarAsync(cancellationToken)
                .ConfigureAwait(false);

            return Convert.ToInt64(insertedIdentifier!);
        });
    }

    /// <inheritdoc/>
    public async Task<long[]> InsertAsync<TEntity>(TEntity[] entities) where TEntity : EntityBase
    {
        string sql = sqlProvider.GetBulkInsertSql<TEntity>(entities.Length);
        IEnumerable<NpgsqlParameter> parameters = entities.BuildParametersFromCollection();

        return await resiliencePipeline.ExecuteAsync(async _ =>
        {
            CancellationToken cancellationToken = GetCancellationToken(databaseSettings.UnitOperationTimeoutInMilliseconds);

            await using NpgsqlConnection connection = await GetNewOpenedConnectionAsync(cancellationToken).ConfigureAwait(false);
            await using NpgsqlCommand command = new(sql, connection);
            object? insertedIds = await command
                .SetCommandParameters(parameters)
                .ExecuteScalarAsync(cancellationToken)
                .ConfigureAwait(false);

            return (long[])insertedIds!;
        });
    }

    /// <inheritdoc/>
    public async Task<long[]> UpsertAsync<TEntity, TFilter>(TEntity[] entities, TFilter filter) where TEntity : EntityBase
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
            CancellationToken cancellationToken = GetCancellationToken(databaseSettings.UnitOperationTimeoutInMilliseconds);

            await using NpgsqlConnection connection = await GetNewOpenedConnectionAsync(cancellationToken).ConfigureAwait(false);
            await using NpgsqlCommand command = new(sql, connection);
            object? insertedIds = await command
                .SetCommandParameters(deleteParameters)
                .SetCommandParameters(insertParameters)
                .ExecuteScalarAsync(cancellationToken)
                .ConfigureAwait(false);

            return (long[])insertedIds!;
        });
    }

    /// <inheritdoc/>
    public async Task UpdateAsync<TEntity>(TEntity entity) where TEntity : EntityBase
    {
        string sql = sqlProvider.GetUpdateByPrimaryKeySql<TEntity>();
        IEnumerable<NpgsqlParameter> parameters = entity.BuildParameters();

        await resiliencePipeline.ExecuteAsync(async _ =>
        {
            CancellationToken cancellationToken = GetCancellationToken(databaseSettings.UnitOperationTimeoutInMilliseconds);

            await using NpgsqlConnection connection = await GetNewOpenedConnectionAsync(cancellationToken).ConfigureAwait(false);
            await using NpgsqlCommand command = new(sql, connection);
            await command
                .SetCommandParameters(parameters)
                .ExecuteNonQueryAsync(cancellationToken)
                .ConfigureAwait(false);
        });
    }

    protected async Task<NpgsqlConnection> GetNewOpenedConnectionAsync(CancellationToken cancellationToken = default)
    {
        NpgsqlConnection connection = new(databaseSettings.ConnectionString);
        await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        return connection;
    }

    protected CancellationToken GetCancellationToken(int timeoutInMilliseconds) => new CancellationTokenSource(timeoutInMilliseconds).Token;
}
