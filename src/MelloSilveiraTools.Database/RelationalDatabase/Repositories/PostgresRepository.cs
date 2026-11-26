using MelloSilveiraTools.Core.ExtensionMethods;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Core.Security;
using MelloSilveiraTools.Database.ExtensionMethods;
using MelloSilveiraTools.Database.RelationalDatabase.Models.Entities;
using MelloSilveiraTools.Database.RelationalDatabase.Models.Filters;
using MelloSilveiraTools.Database.RelationalDatabase.Settings;
using MelloSilveiraTools.Database.RelationalDatabase.Sql.Provider;
using MelloSilveiraTools.Database.Repositories;
using MelloSilveiraTools.Database.ResiliencePipelines;
using Npgsql;
using NpgsqlTypes;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace MelloSilveiraTools.Database.RelationalDatabase.Repositories;

/// <summary>
/// Pure-Npgsql, Native AOT-compatible repository for PostgreSQL.
/// Registered as Singleton in DI; borrows pooled connections from the DI-managed singleton
/// <see cref="NpgsqlDataSource"/> and executes every operation inside a single-round-trip
/// <see cref="NpgsqlBatch"/> scoped by transaction-local Row-Level Security context. Safe for concurrent use.
/// </summary>
/// <remarks>
/// <para>
/// Every operation is executed via <see cref="NpgsqlDataSource.CreateBatch"/> with
/// <c>SELECT set_config('app.current_user_id', @UserId, true)</c> prepended as the first batch command.
/// Under PostgreSQL's extended query protocol, all commands in an <see cref="NpgsqlBatch"/> run atomically
/// inside a single implicit transaction block until the trailing <c>Sync</c> message — eliminating explicit
/// <c>BEGIN</c>/<c>COMMIT</c> network round-trips while guaranteeing that PostgreSQL resets
/// <c>app.current_user_id</c> automatically before returning the connection to the pool.
/// </para>
/// <para>
/// All non-streaming database operations are wrapped by <see cref="PostgresResiliencePipeline"/>, which transparently
/// retries transient failures (connection drops, deadlocks, serialization failures, timeouts).
/// <see cref="NpgsqlParameter"/> collections are constructed once per repository call (avoiding repeated reflection);
/// the first attempt binds the original instances directly (zero extra allocations), and retries bind detached
/// <see cref="NpgsqlParameter.Clone"/> copies to prevent parameter-collection ownership conflicts.
/// </para>
/// <para>
/// Per-command timeouts are taken from <see cref="DatabaseSettings"/>:
/// <see cref="DatabaseSettings.UnitOperationTimeoutInSeconds"/> applies to single-row operations
/// (insert, update, delete-by-id, get-by-id, exists) and
/// <see cref="DatabaseSettings.BulkOperationTimeoutInSeconds"/> applies to batch operations
/// (bulk insert, bulk upsert).
/// </para>
/// </remarks>
/// <param name="sqlProvider">Provider responsible for generating and caching PostgreSQL statements.</param>
/// <param name="currentUserProvider">Mandatory provider resolving the active user identifier for PostgreSQL RLS batch scoping.</param>
/// <param name="dataSource">Singleton <see cref="NpgsqlDataSource"/> managed by the DI container.</param>
/// <param name="resiliencePipeline">Resilience pipeline wrapping transient PostgreSQL failures.</param>
/// <param name="databaseSettings">Database connection and command timeout settings.</param>
public class PostgresRepository(
    ISqlProvider sqlProvider,
    ICurrentUserProvider currentUserProvider,
    NpgsqlDataSource dataSource,
    PostgresResiliencePipeline resiliencePipeline,
    DatabaseSettings databaseSettings) : IRepository
{
    // Index 0 in every scoped NpgsqlBatch is always the RLS context command added by AddUserContext
    // ("SELECT set_config('app.current_user_id', @UserId, true);"), which occupies BatchCommands[0]
    // and emits result set 0 (1 row). Index 1 therefore targets the actual repository statement
    // (for BatchCommands[1].Rows in non-queries and for skipping the set_config result set in readers;
    // in 3-command bulk UpsertAsync, DELETE emits NoData so result set 1 lands directly on INSERT RETURNING).
    private const int TargetBatchIndex = 1;

    /// <summary>
    /// Database settings used to configure command timeouts.
    /// </summary>
    protected DatabaseSettings DatabaseSettings { get; } = databaseSettings;

    /// <inheritdoc/>
    public async Task<long> CountAsync<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TEntity,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TFilter>(
        TFilter filter,
        CancellationToken cancellationToken = default)
        where TFilter : FilterBase
    {
        (string? sqlWhereClause, List<NpgsqlParameter> parameters) = filter.BuildWhereClauseAndNpgsqlParameters();
        string sql = sqlProvider.GetCountSql<TEntity>().Replace("#WHERE", sqlWhereClause);
        return await ExecuteBatchAsync(sql, parameters, null, batch => batch.ExecuteScalarAsync<long>(TargetBatchIndex, cancellationToken)).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<bool> ExistAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TEntity>(long id, CancellationToken cancellationToken = default)
    {
        string sql = sqlProvider.GetExistByPrimaryKeySql<TEntity>();
        NpgsqlParameter[] parameters = [new NpgsqlParameter("@Id", NpgsqlDbType.Bigint) { Value = id }];
        long count = await ExecuteBatchAsync(sql, parameters, DatabaseSettings.UnitOperationTimeoutInSeconds, batch => batch.ExecuteScalarAsync<long>(TargetBatchIndex, cancellationToken)).ConfigureAwait(false);
        return count > 0;
    }

    /// <inheritdoc/>
    public async Task<bool> ExistAsync<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TEntity,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TFilter>(
        TFilter filter,
        CancellationToken cancellationToken = default)
        where TFilter : FilterBase
    {
        long count = await CountAsync<TEntity, TFilter>(filter, cancellationToken).ConfigureAwait(false);
        return count > 0;
    }

    /// <inheritdoc/>
    public Task DeleteAllAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TEntity>(CancellationToken cancellationToken = default)
        => ExecuteNonQueryBatchAsync(sqlProvider.GetDeleteSql<TEntity>().Replace("#WHERE", null), null, null, cancellationToken);

    /// <inheritdoc/>
    public Task DeleteAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TEntity>(long id, CancellationToken cancellationToken = default)
    {
        string sql = sqlProvider.GetDeleteByPrimaryKeySql<TEntity>();
        NpgsqlParameter[] parameters = [new NpgsqlParameter("@Id", NpgsqlDbType.Bigint) { Value = id }];
        return ExecuteNonQueryBatchAsync(sql, parameters, DatabaseSettings.UnitOperationTimeoutInSeconds, cancellationToken);
    }

    /// <inheritdoc/>
    public Task DeleteAsync<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TEntity,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TFilter>(
        TFilter filter,
        CancellationToken cancellationToken = default)
    {
        (string? sqlWhereClause, List<NpgsqlParameter> parameters) = filter.BuildWhereClauseAndNpgsqlParameters();
        string sql = sqlProvider.GetDeleteSql<TEntity>().Replace("#WHERE", sqlWhereClause);
        return ExecuteNonQueryBatchAsync(sql, parameters, null, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<TEntity?> GetFirstOrDefaultAsync<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TEntity,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TFilter>(
        TFilter filter,
        SortOrder sortOrder = SortOrder.Asc,
        CancellationToken cancellationToken = default)
        where TEntity : class, new()
        where TFilter : FilterBase
    {
        (string? sqlWhereClause, List<NpgsqlParameter> parameters) = filter.BuildWhereClauseAndNpgsqlParameters();
        string sql = sqlProvider.GetSelectSql<TEntity>()
            .Replace("#WHERE", sqlWhereClause)
            .Replace("#LIMIT", "LIMIT 1")
            .Replace("#ORDERBY", sortOrder.ToNpgsqlString())
            .Remove("#OFFSET");

        return ExecuteBatchAsync(sql, parameters, DatabaseSettings.UnitOperationTimeoutInSeconds, batch => batch.QueryFirstOrDefaultAsync<TEntity>(TargetBatchIndex, cancellationToken));
    }

    /// <inheritdoc/>
    public Task<TEntity?> GetAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TEntity>(long id, CancellationToken cancellationToken = default) where TEntity : class, new()
    {
        string sql = sqlProvider.GetSelectByPrimaryKeySql<TEntity>();
        NpgsqlParameter[] parameters = [new NpgsqlParameter("@Id", NpgsqlDbType.Bigint) { Value = id }];
        return ExecuteBatchAsync(sql, parameters, DatabaseSettings.UnitOperationTimeoutInSeconds, batch => batch.QueryFirstOrDefaultAsync<TEntity>(TargetBatchIndex, cancellationToken));
    }

    /// <inheritdoc/>
    public IAsyncEnumerable<TEntity> GetAsync<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TEntity,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TFilter>(
        TFilter filter,
        Pagination? pagination = null,
        CancellationToken cancellationToken = default)
        where TEntity : class, new()
        where TFilter : FilterBase
        => StreamAsync<TEntity, TFilter>(sqlProvider.GetSelectSql<TEntity>(), filter, pagination, cancellationToken);

    /// <inheritdoc/>
    public IAsyncEnumerable<TEntity> GetDistinctAsync<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TEntity,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TFilter>(
        TFilter filter,
        Pagination? pagination = null,
        CancellationToken cancellationToken = default)
        where TEntity : class, new()
        where TFilter : FilterBase
        => StreamAsync<TEntity, TFilter>(sqlProvider.GetSelectDistinctSql<TEntity>(), filter, pagination, cancellationToken);

    /// <inheritdoc/>
    public Task<TEntity?> GetByUniqueColumnAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TEntity>(object value, CancellationToken cancellationToken = default) where TEntity : class, new()
    {
        string sql = sqlProvider.GetSelectByUniqueColumnSql<TEntity>();
        NpgsqlParameter[] parameters = [BuildUniqueColumnParameter(value)];
        return ExecuteBatchAsync(sql, parameters, DatabaseSettings.UnitOperationTimeoutInSeconds, batch => batch.QueryFirstOrDefaultAsync<TEntity>(TargetBatchIndex, cancellationToken));
    }

    private IAsyncEnumerable<TEntity> StreamAsync<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TEntity,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TFilter>(
        string baseSql,
        TFilter filter,
        Pagination? pagination,
        CancellationToken cancellationToken)
        where TEntity : class, new()
        where TFilter : FilterBase
    {
        (string? whereClause, List<NpgsqlParameter> parameters) = filter.BuildWhereClauseAndNpgsqlParameters();
        string sql = baseSql
            .Replace("#WHERE", whereClause)
            .Replace("#ORDERBY", pagination?.SortOrder?.ToNpgsqlString())
            .Replace("#LIMIT", pagination?.Limit is null ? null : $"LIMIT {pagination.Limit}")
            .Replace("#OFFSET", pagination?.Offset is null ? null : $"OFFSET {pagination.Offset}");
        return GetAsync<TEntity>(sql, parameters, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<long> InsertAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TEntity>(
        TEntity entity,
        CancellationToken cancellationToken = default)
    {
        string sql = sqlProvider.GetInsertSql<TEntity>();
        List<NpgsqlParameter> parameters = ApplyCurrentUserIdParameters(entity, true);
        return ExecuteBatchAsync(sql, parameters, DatabaseSettings.UnitOperationTimeoutInSeconds, batch => batch.ExecuteScalarAsync<long>(TargetBatchIndex, cancellationToken));
    }

    /// <inheritdoc/>
    public Task<long[]> InsertAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TEntity>(TEntity[] entities, CancellationToken cancellationToken = default)
    {
        string sql = sqlProvider.GetBulkInsertSql<TEntity>(entities.Length);
        List<NpgsqlParameter> parameters = ApplyCurrentUserIdParameters(entities);

        return ExecuteBatchAsync(sql, parameters, DatabaseSettings.BulkOperationTimeoutInSeconds, async batch =>
        {
            long[] ids = new long[entities.Length];
            await using NpgsqlDataReader reader = await batch.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            if (await reader.AdvanceToResultSetAsync(TargetBatchIndex, cancellationToken).ConfigureAwait(false))
            {
                int i = 0;
                while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                    ids[i++] = reader.GetInt64(0);
            }

            return ids;
        });
    }

    /// <inheritdoc/>
    public Task<Result<long>> TryInsertAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TEntity>(TEntity entity, CancellationToken cancellationToken = default)
    {
        string sql = sqlProvider.GetTryInsertSql<TEntity>();
        List<NpgsqlParameter> parameters = ApplyCurrentUserIdParameters(entity, true);

        return ExecuteBatchAsync(sql, parameters, DatabaseSettings.UnitOperationTimeoutInSeconds, async batch =>
        {
            await using NpgsqlDataReader reader = await batch.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            if (!await reader.AdvanceToResultSetAsync(TargetBatchIndex, cancellationToken).ConfigureAwait(false) || !await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                return Result.CreateUnknownError();

            long id = reader.GetInt64(0);
            bool inserted = reader.GetBoolean(1);
            return inserted ? Result.CreateSuccessOk(id) : Result.CreateConflict(id);
        });
    }

    /// <inheritdoc/>
    public Task<long> UpsertAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TEntity>(TEntity entity, CancellationToken cancellationToken = default)
        => InsertAsync(entity, cancellationToken);

    /// <inheritdoc/>
    public async Task<long[]> UpsertAsync<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TEntity,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TFilter>(
        TEntity[] entities,
        TFilter filter,
        CancellationToken cancellationToken = default)
    {
        (string? sqlWhereClause, List<NpgsqlParameter> deleteParameters) = filter.BuildWhereClauseAndNpgsqlParameters();
        string deleteSql = sqlProvider.GetDeleteSql<TEntity>().Replace("#WHERE", sqlWhereClause);
        string insertSql = sqlProvider.GetBulkInsertSql<TEntity>(entities.Length);
        List<NpgsqlParameter> insertParameters = ApplyCurrentUserIdParameters(entities);

        return await resiliencePipeline.ExecuteWithRetryFlagAsync(async isRetry =>
        {
            await using NpgsqlBatch batch = dataSource
                .CreateBatch(DatabaseSettings.BulkOperationTimeoutInSeconds)
                .AddUserContext(currentUserProvider.GetCurrentUserId())
                .AddCommand(deleteSql, deleteParameters, isRetry)
                .AddCommand(insertSql, insertParameters, isRetry);

            List<long> ids = [];
            await using NpgsqlDataReader reader = await batch.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            if (await reader.AdvanceToResultSetAsync(TargetBatchIndex, cancellationToken).ConfigureAwait(false))
            {
                do
                {
                    while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                        ids.Add(reader.GetInt64(0));
                } while (await reader.NextResultAsync(cancellationToken).ConfigureAwait(false));
            }

            return ids.ToArray();
        }).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<bool> TryUpdateAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TEntity>(
        TEntity entity,
        CancellationToken cancellationToken = default)
    {
        string sql = sqlProvider.GetUpdateByPrimaryKeySql<TEntity>();
        List<NpgsqlParameter> parameters = ApplyCurrentUserIdParameters(entity);
        ulong affectedRows = await ExecuteBatchAsync(sql, parameters, DatabaseSettings.UnitOperationTimeoutInSeconds, batch => batch.ExecuteNonQueryRowsAsync(TargetBatchIndex, cancellationToken)).ConfigureAwait(false);
        return affectedRows > 0;
    }

    /// <summary>
    /// Streams rows returned by <paramref name="sql"/> inside a single-round-trip RLS-scoped <see cref="NpgsqlBatch"/>
    /// and materializes each of them into a <typeparamref name="TEntity"/>.
    /// </summary>
    /// <typeparam name="TEntity">Target entity type whose public properties are preserved for trimming and Native AOT.</typeparam>
    /// <param name="sql">SQL statement to execute.</param>
    /// <param name="parameters">Parameters to bind to the statement, if any.</param>
    /// <param name="cancellationToken">Token used to cancel the streaming operation.</param>
    protected async IAsyncEnumerable<TEntity> GetAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TEntity>(
        string sql,
        IEnumerable<NpgsqlParameter>? parameters,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
        where TEntity : class, new()
    {
        await using NpgsqlBatch batch = CreateScopedBatch(sql, parameters);
        await foreach (TEntity entity in batch.StreamAsync<TEntity>(TargetBatchIndex, cancellationToken).ConfigureAwait(false))
            yield return entity;
    }

    /// <summary>
    /// Creates an <see cref="NpgsqlBatch"/> from the DI-managed <see cref="NpgsqlDataSource"/> prepended with
    /// transaction-local RLS context (<c>SELECT set_config('app.current_user_id', @UserId, true)</c>) followed by
    /// <paramref name="sql"/>, executing both atomically in a single network round-trip.
    /// </summary>
    /// <param name="sql">Target PostgreSQL statement to append after the RLS context command.</param>
    /// <param name="parameters">Optional parameters to bind to <paramref name="sql"/>.</param>
    /// <param name="commandTimeoutSeconds">Optional per-command timeout in seconds.</param>
    /// <param name="cloneParameters">
    /// When <see langword="true"/>, binds detached <see cref="NpgsqlParameter.Clone"/> copies so Polly retries
    /// reuse pre-built parameters without repeated reflection or parameter-collection ownership conflicts.
    /// </param>
    /// <returns>A configured <see cref="NpgsqlBatch"/> ready for execution.</returns>
    protected NpgsqlBatch CreateScopedBatch(string sql, IEnumerable<NpgsqlParameter>? parameters = null, int? commandTimeoutSeconds = null, bool cloneParameters = false) => dataSource
        .CreateBatch(commandTimeoutSeconds)
        .AddUserContext(currentUserProvider.GetCurrentUserId())
        .AddCommand(sql, parameters, cloneParameters);

    private async Task<T> ExecuteBatchAsync<T>(
        string sql,
        IEnumerable<NpgsqlParameter>? parameters,
        int? commandTimeoutSeconds,
        Func<NpgsqlBatch, Task<T>> operation) => await resiliencePipeline.ExecuteWithRetryFlagAsync(async isRetry =>
        {
            await using NpgsqlBatch batch = CreateScopedBatch(sql, parameters, commandTimeoutSeconds, isRetry);
            return await operation(batch).ConfigureAwait(false);
        }).ConfigureAwait(false);

    private async Task ExecuteNonQueryBatchAsync(
        string sql,
        IEnumerable<NpgsqlParameter>? parameters,
        int? commandTimeoutSeconds,
        CancellationToken cancellationToken) => await resiliencePipeline.ExecuteWithRetryFlagAsync(async isRetry =>
        {
            await using NpgsqlBatch batch = CreateScopedBatch(sql, parameters, commandTimeoutSeconds, isRetry);
            await batch.ExecuteNonQueryRowsAsync(TargetBatchIndex, cancellationToken).ConfigureAwait(false);
        }).ConfigureAwait(false);

    private static NpgsqlParameter BuildUniqueColumnParameter(object value) => value is Enum enumValue
        ? new NpgsqlParameter("@UniqueColumnValue", NpgsqlDbType.Integer) { Value = Convert.ToInt32(enumValue, CultureInfo.InvariantCulture) }
        : new NpgsqlParameter("@UniqueColumnValue", value.GetType().GetDbTypeFromPropertyType()) { Value = value };

    private List<NpgsqlParameter> ApplyCurrentUserIdParameters<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TEntity>(TEntity[] entities)
        => ApplyCurrentUserIdParameters(entities.BuildParametersFromCollection(), typeof(UserScopedEntityBase).IsAssignableFrom(typeof(TEntity)));

    private List<NpgsqlParameter> ApplyCurrentUserIdParameters<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TEntity>(TEntity entity, bool useDeclaredProperties = false)
        => ApplyCurrentUserIdParameters(entity.BuildParameters(useDeclaredProperties), typeof(UserScopedEntityBase).IsAssignableFrom(typeof(TEntity)));

    private List<NpgsqlParameter> ApplyCurrentUserIdParameters(IEnumerable<NpgsqlParameter> parameters, bool isUserScoped)
    {
        List<NpgsqlParameter> resolved = parameters as List<NpgsqlParameter> ?? [.. parameters];
        if (!isUserScoped || currentUserProvider.IsSuperuser())
            return resolved;

        object enforcedUserId = currentUserProvider.GetCurrentUserId() is long userId ? userId : DBNull.Value;
        foreach (NpgsqlParameter parameter in resolved)
        {
            if (parameter.ParameterName.Equals(nameof(UserScopedEntityBase.UserId), StringComparison.Ordinal)
                || parameter.ParameterName.StartsWith($"{nameof(UserScopedEntityBase.UserId)}_", StringComparison.Ordinal))
            {
                parameter.Value = enforcedUserId;
            }
        }

        return resolved;
    }
}
