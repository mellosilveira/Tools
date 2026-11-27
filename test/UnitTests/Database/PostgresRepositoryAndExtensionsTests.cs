using MelloSilveiraTools.Core.Files.Models;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Core.ResiliencePipelines;
using MelloSilveiraTools.Core.Security;
using MelloSilveiraTools.Database;
using MelloSilveiraTools.Database.ExtensionMethods;
using MelloSilveiraTools.Database.RelationalDatabase.Attributes;
using MelloSilveiraTools.Database.RelationalDatabase.Models;
using MelloSilveiraTools.Database.RelationalDatabase.Models.Entities;
using MelloSilveiraTools.Database.RelationalDatabase.Models.Filters;
using MelloSilveiraTools.Database.RelationalDatabase.Repositories;
using MelloSilveiraTools.Database.RelationalDatabase.Settings;
using MelloSilveiraTools.Database.RelationalDatabase.Sql.Provider;
using MelloSilveiraTools.Database.ResiliencePipelines;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using NpgsqlTypes;
using Polly;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace UnitTests.Database;

/// <summary>
/// Unit tests for <see cref="PostgresRepository"/> and <see cref="NpgsqlConnectionExtensions"/>
/// backed by an in-process loopback PostgreSQL v3 wire-protocol server.
/// </summary>
public sealed class PostgresRepositoryAndExtensionsTests
{
    [Table("simple_item", "si")]
    public sealed record SimpleItemEntity
    {
        [PrimaryKeyColumn]
        public long Id { get; init; }

        [UniqueColumn]
        public string Code { get; init; } = string.Empty;
    }

    [Filter(typeof(SimpleItemEntity))]
    public sealed record SimpleItemFilter : FilterBase
    {
        [FilterColumn(FilterClause.Equal, nameof(SimpleItemEntity.Code))]
        public string? Code { get; init; }
    }

    [Table("simple_item", "si")]
    public sealed class CountingItemEntity
    {
        private readonly string _code;

        public CountingItemEntity() => _code = string.Empty;

        public CountingItemEntity(long id, string code)
        {
            Id = id;
            _code = code;
        }

        public int CodeGetterCalls { get; private set; }

        [PrimaryKeyColumn]
        public long Id { get; init; }

        [UniqueColumn]
        public string Code
        {
            get
            {
                CodeGetterCalls++;
                return _code;
            }
        }
    }

    [Filter(typeof(SimpleItemEntity))]
    public sealed record CountingItemFilter : FilterBase
    {
        private readonly string? _code;

        public CountingItemFilter(string? code) => _code = code;

        public int CodeGetterCalls { get; private set; }

        [FilterColumn(FilterClause.Equal, nameof(SimpleItemEntity.Code))]
        public string? Code
        {
            get
            {
                CodeGetterCalls++;
                return _code;
            }
        }
    }

    /// <summary>
    /// Verifies all <see cref="PostgresRepository"/> CRUD, streaming, bulk, and upsert operations,
    /// as well as <see cref="NpgsqlConnectionExtensions"/> helper overloads.
    /// </summary>
    [Fact]
    public async Task PostgresRepository_And_NpgsqlConnectionExtensions_ExecuteAllOperations()
    {
        await using FakePostgresWireServer server = new();
        server.Start();

        DatabaseSettings settings = new()
        {
            ConnectionString = server.ConnectionString,
            UnitOperationTimeoutInMilliseconds = 5_000,
            BulkOperationTimeoutInMilliseconds = 10_000
        };
        ResiliencePipelineSettings resilienceSettings = new()
        {
            BackoffType = DelayBackoffType.Constant,
            MaxRetryAttempts = 1,
            DelayInMilliseconds = 1,
            UseJitter = false
        };
        PostgresResiliencePipeline resiliencePipeline = new(NullLogger<PostgresResiliencePipeline>.Instance, resilienceSettings);
        ISqlProvider sqlProvider = new PostgresSqlProvider();
        await using NpgsqlDataSource dataSource = DatabaseDependencyInjection.CreateDataSource(settings, NullLoggerFactory.Instance);
        PostgresRepository repository = new(sqlProvider, new CurrentUserProvider(), dataSource, resiliencePipeline, settings);

        // 1. CountAsync
        server.EnqueueScalarLong(5L);
        long count = await repository.CountAsync<SimpleItemEntity, SimpleItemFilter>(new SimpleItemFilter { Code = "A" });
        Assert.Equal(5L, count);

        // 2. ExistAsync(long id) -> true and false
        server.EnqueueScalarLong(1L);
        Assert.True(await repository.ExistAsync<SimpleItemEntity>(10L));
        server.EnqueueScalarLong(0L);
        Assert.False(await repository.ExistAsync<SimpleItemEntity>(10L));

        // 3. ExistAsync(TFilter) -> true and false
        server.EnqueueScalarLong(2L);
        Assert.True(await repository.ExistAsync<SimpleItemEntity, SimpleItemFilter>(new SimpleItemFilter { Code = "A" }));
        server.EnqueueScalarLong(0L);
        Assert.False(await repository.ExistAsync<SimpleItemEntity, SimpleItemFilter>(new SimpleItemFilter()));

        // 4. DeleteAllAsync, DeleteAsync(id), DeleteAsync(filter with parameters and without parameters)
        server.EnqueueNonQuery(3);
        await repository.DeleteAllAsync<SimpleItemEntity>();

        server.EnqueueNonQuery(1);
        await repository.DeleteAsync<SimpleItemEntity>(10L);

        server.EnqueueNonQuery(1);
        await repository.DeleteAsync<SimpleItemEntity, SimpleItemFilter>(new SimpleItemFilter { Code = "A" });

        server.EnqueueNonQuery(1);
        await repository.DeleteAsync<SimpleItemEntity, SimpleItemFilter>(new SimpleItemFilter());

        // 5. GetFirstOrDefaultAsync
        server.EnqueueSimpleItems((11L, "CODE-11"));
        SimpleItemEntity? first = await repository.GetFirstOrDefaultAsync<SimpleItemEntity, SimpleItemFilter>(new SimpleItemFilter { Code = "CODE-11" }, SortOrder.Desc);
        Assert.NotNull(first);
        Assert.Equal(11L, first.Id);
        Assert.Equal("CODE-11", first.Code);

        // 6. GetAsync(long id)
        server.EnqueueSimpleItems((12L, "CODE-12"));
        SimpleItemEntity? byId = await repository.GetAsync<SimpleItemEntity>(12L);
        Assert.NotNull(byId);
        Assert.Equal(12L, byId.Id);

        // 7. GetByUniqueColumnAsync
        server.EnqueueSimpleItems((13L, "CODE-13"));
        SimpleItemEntity? byUnique = await repository.GetByUniqueColumnAsync<SimpleItemEntity>("CODE-13");
        Assert.NotNull(byUnique);
        Assert.Equal("CODE-13", byUnique.Code);

        // 8. GetAsync(filter, pagination) with null pagination and populated pagination (including a null-first-column row)
        server.EnqueueSimpleItemsWithNullFirstRow((21L, "C21"), (null, "SKIP"), (22L, "C22"));
        List<SimpleItemEntity> streamed = [];
        await foreach (SimpleItemEntity item in repository.GetAsync<SimpleItemEntity, SimpleItemFilter>(
            new SimpleItemFilter { Code = "C" },
            new Pagination { SortOrder = SortOrder.Asc, Limit = 10, Offset = 0 }))
        {
            streamed.Add(item);
        }
        Assert.Equal(2, streamed.Count);

        server.EnqueueSimpleItems((23L, "C23"));
        List<SimpleItemEntity> distinct = [];
        await foreach (SimpleItemEntity item in repository.GetDistinctAsync<SimpleItemEntity, SimpleItemFilter>(
            new SimpleItemFilter(),
            pagination: new Pagination()))
        {
            distinct.Add(item);
        }
        Assert.Single(distinct);

        server.EnqueueSimpleItems((24L, "C24"));
        List<SimpleItemEntity> noPagination = [];
        await foreach (SimpleItemEntity item in repository.GetAsync<SimpleItemEntity, SimpleItemFilter>(new SimpleItemFilter(), pagination: null))
        {
            noPagination.Add(item);
        }
        Assert.Single(noPagination);

        // 9. InsertAsync(entity) and InsertAsync(entities[])
        server.EnqueueScalarLong(101L);
        long insertedId = await repository.InsertAsync(new SimpleItemEntity { Id = 101L, Code = "NEW-101" });
        Assert.Equal(101L, insertedId);

        server.EnqueueLongRows(201L, 202L);
        long[] bulkIds = await repository.InsertAsync([
            new SimpleItemEntity { Id = 201L, Code = "B1" },
            new SimpleItemEntity { Id = 202L, Code = "B2" }
        ]);
        Assert.Equal([201L, 202L], bulkIds);

        // 10. TryInsertAsync -> Ok, Conflict, and empty reader (UnknownError)
        server.EnqueueTryInsertRow(301L, true);
        Result<long> tryOk = await repository.TryInsertAsync(new SimpleItemEntity { Id = 301L, Code = "T1" });
        Assert.True(tryOk.Success);
        Assert.Equal(301L, tryOk.Data);

        server.EnqueueTryInsertRow(301L, false);
        Result<long> tryConflict = await repository.TryInsertAsync(new SimpleItemEntity { Id = 301L, Code = "T1" });
        Assert.True(tryConflict.IsConflict);

        server.EnqueueEmptyTryInsert();
        Result<long> tryEmpty = await repository.TryInsertAsync(new SimpleItemEntity { Id = 302L, Code = "T2" });
        Assert.False(tryEmpty.Success);

        // 11. UpsertAsync(entity) and UpsertAsync(entities[], filter)
        server.EnqueueScalarLong(401L);
        long upsertedId = await repository.UpsertAsync(new SimpleItemEntity { Id = 401L, Code = "U1" });
        Assert.Equal(401L, upsertedId);

        server.EnqueueNonQuery(1);
        server.EnqueueLongRows(501L, 502L);
        long[] upsertedBulk = await repository.UpsertAsync(
            [new SimpleItemEntity { Id = 501L, Code = "UB1" }, new SimpleItemEntity { Id = 502L, Code = "UB2" }],
            new SimpleItemFilter { Code = "OLD" });
        Assert.Equal([501L, 502L], upsertedBulk);

        // 12. TryUpdateAsync -> true and false
        server.EnqueueNonQuery(1);
        Assert.True(await repository.TryUpdateAsync(new SimpleItemEntity { Id = 1L, Code = "UPD" }));

        server.EnqueueNonQuery(0);
        Assert.False(await repository.TryUpdateAsync(new SimpleItemEntity { Id = 1L, Code = "UPD" }));

        Assert.True(server.SetConfigCallCount > 0);
        Assert.Equal(0, server.ExplicitTransactionCommandCount);
        Assert.DoesNotContain(server.RecordedBoundQueries, q => q.Query.Contains("app.is_superuser", StringComparison.OrdinalIgnoreCase));

        // 13. Direct NpgsqlConnectionExtensions overloads (QueryFirstOrDefaultAsync, QueryAsync, ExecuteScalarAsync)
        await using NpgsqlConnection conn = await dataSource.OpenConnectionAsync();

        server.EnqueueSimpleItems((77L, "EXT-77"));
        SimpleItemEntity? qFirst = await conn.QueryFirstOrDefaultAsync<SimpleItemEntity>(
            "SELECT id, code FROM simple_item WHERE code = @Code",
            [new NpgsqlParameter("@Code", NpgsqlDbType.Text) { Value = "EXT-77" }],
            cancellationToken: CancellationToken.None);
        Assert.NotNull(qFirst);
        Assert.Equal(77L, qFirst.Id);

        server.EnqueueSimpleItems((88L, "EXT-88"));
        List<SimpleItemEntity> qAll = await conn.QueryAsync<SimpleItemEntity>(
            "SELECT id, code FROM simple_item",
            cancellationToken: CancellationToken.None);
        Assert.Single(qAll);

        server.EnqueueScalarLong(99L);
        long scalarVal = await conn.ExecuteScalarAsync<long>("SELECT 99", cancellationToken: CancellationToken.None);
        Assert.Equal(99L, scalarVal);

        // 14. Exception paths inside await-using blocks (triggers pendingExceptionState == 1 disposal branches)
        server.EnqueueError();
        await Assert.ThrowsAsync<PostgresException>(() => repository.CountAsync<SimpleItemEntity, SimpleItemFilter>(new SimpleItemFilter()));

        server.EnqueueError();
        await Assert.ThrowsAsync<PostgresException>(() => repository.ExistAsync<SimpleItemEntity>(1L));

        server.EnqueueError();
        await Assert.ThrowsAsync<PostgresException>(() => repository.ExistAsync<SimpleItemEntity, SimpleItemFilter>(new SimpleItemFilter()));

        server.EnqueueError();
        await Assert.ThrowsAsync<PostgresException>(() => repository.DeleteAllAsync<SimpleItemEntity>());

        server.EnqueueError();
        await Assert.ThrowsAsync<PostgresException>(() => repository.DeleteAsync<SimpleItemEntity>(1L));

        server.EnqueueError();
        await Assert.ThrowsAsync<PostgresException>(() => repository.DeleteAsync<SimpleItemEntity, SimpleItemFilter>(new SimpleItemFilter()));

        server.EnqueueError();
        await Assert.ThrowsAsync<PostgresException>(() => repository.GetFirstOrDefaultAsync<SimpleItemEntity, SimpleItemFilter>(new SimpleItemFilter()));

        server.EnqueueError();
        await Assert.ThrowsAsync<PostgresException>(() => repository.GetAsync<SimpleItemEntity>(1L));

        server.EnqueueError();
        await Assert.ThrowsAsync<PostgresException>(() => repository.GetByUniqueColumnAsync<SimpleItemEntity>("X"));

        server.EnqueueError();
        await Assert.ThrowsAsync<PostgresException>(() => repository.InsertAsync(new SimpleItemEntity { Id = 1L, Code = "E" }));

        server.EnqueueNullLongRow();
        await Assert.ThrowsAsync<InvalidCastException>(() => repository.InsertAsync([new SimpleItemEntity { Id = 1L, Code = "E" }]));

        server.EnqueueNullLongRow();
        await Assert.ThrowsAsync<InvalidCastException>(() => repository.TryInsertAsync(new SimpleItemEntity { Id = 1L, Code = "E" }));

        server.EnqueueError();
        await Assert.ThrowsAsync<PostgresException>(() => repository.UpsertAsync(new SimpleItemEntity { Id = 1L, Code = "E" }));

        server.EnqueueNonQuery(1);
        server.EnqueueNullLongRow();
        await Assert.ThrowsAsync<InvalidCastException>(() => repository.UpsertAsync([new SimpleItemEntity { Id = 1L, Code = "E" }], new SimpleItemFilter()));

        server.EnqueueError();
        await Assert.ThrowsAsync<PostgresException>(() => repository.TryUpdateAsync(new SimpleItemEntity { Id = 1L, Code = "E" }));
    }

    /// <summary>
    /// Verifies over the PostgreSQL v3 wire protocol that <see cref="PostgresRepository"/>:
    /// (1) executes transaction-local <c>set_config('app.current_user_id', @UserId, true)</c> inside a transaction
    ///     and never sends <c>app.is_superuser</c>,
    /// (2) enforces <c>NULL</c> for anonymous callers (even if payload attempts to spoof <c>UserId</c>),
    /// (3) enforces the authenticated user's <c>UserId</c> for non-superusers across single insert, bulk insert, and update, and
    /// (4) preserves explicit <c>UserId</c> values (<c>null</c> for public or a tenant ID) for superusers.
    /// </summary>
    [Fact]
    public async Task PostgresRepository_PropagatesTransactionalRlsConfig_And_EnforcesUserIdOnWireProtocol()
    {
        await using FakePostgresWireServer server = new();
        server.Start();

        DatabaseSettings settings = new()
        {
            ConnectionString = server.ConnectionString,
            UnitOperationTimeoutInMilliseconds = 5_000,
            BulkOperationTimeoutInMilliseconds = 10_000
        };
        ResiliencePipelineSettings resilienceSettings = new()
        {
            BackoffType = DelayBackoffType.Constant,
            MaxRetryAttempts = 1,
            DelayInMilliseconds = 1,
            UseJitter = false
        };
        PostgresResiliencePipeline resiliencePipeline = new(NullLogger<PostgresResiliencePipeline>.Instance, resilienceSettings);
        ISqlProvider sqlProvider = new PostgresSqlProvider();
        CurrentUserProvider currentUserProvider = new();
        await using NpgsqlDataSource dataSource = DatabaseDependencyInjection.CreateDataSource(settings, NullLoggerFactory.Instance);
        PostgresRepository repository = new(sqlProvider, currentUserProvider, dataSource, resiliencePipeline, settings);

        StorageEntryMetadata metadata = new(StorageProvider.Local, "loc/a.csv", "loc", "a.csv", 10, DateTimeOffset.UtcNow);
        StorageEntryMetadataEntity defaultUserEntity = StorageEntryMetadataEntity.Create(metadata, "EXP-1");

        // 1. Anonymous call -> set_config receives "" and spoofed UserId (99L) is forced to NULL (DBNull)
        server.EnqueueScalarLong(90L);
        await repository.InsertAsync(defaultUserEntity with { UserId = 99L });
        Assert.Equal([""], server.RecordedSetConfigUserIds);

        // 2. Authenticated non-superuser (42L) -> set_config receives "42" and @UserId is forced to 42L
        using (currentUserProvider.BeginScope(42L))
        {
            server.EnqueueScalarLong(100L);
            long id = await repository.InsertAsync(defaultUserEntity);
            Assert.Equal(100L, id);

            server.EnqueueScalarLong(101L);
            StorageEntryMetadataEntity spoofedUserEntity = defaultUserEntity with { UserId = 99L };
            await repository.InsertAsync(spoofedUserEntity);

            server.EnqueueLongRows(201L, 202L);
            await repository.InsertAsync([defaultUserEntity, defaultUserEntity]);

            server.EnqueueNonQuery(1);
            Assert.True(await repository.TryUpdateAsync(defaultUserEntity with { Id = 100L }));
        }

        // 3. Superuser (1L) -> preserves explicit NULL (public) and explicit tenant ID (20L) verbatim
        using (currentUserProvider.BeginScope(1L, isSuperuser: true))
        {
            server.EnqueueScalarLong(300L);
            await repository.InsertAsync(defaultUserEntity with { UserId = null });

            server.EnqueueNonQuery(1);
            Assert.True(await repository.TryUpdateAsync(defaultUserEntity with { Id = 100L, UserId = 20L }));
        }

        // 4. Back outside scope -> set_config returns to ""
        server.EnqueueScalarLong(0L);
        await repository.ExistAsync<StorageEntryMetadataEntity>(1L);

        Assert.Equal(["", "42", "42", "42", "42", "1", "1", ""], server.RecordedSetConfigUserIds);
        Assert.Equal(8, server.TransactionLocalSetConfigCount);
        Assert.Equal(0, server.ExplicitTransactionCommandCount);
        Assert.Equal(8, server.SyncMessageCount);
        Assert.DoesNotContain(server.RecordedBoundQueries, q => q.Query.Contains("app.is_superuser", StringComparison.OrdinalIgnoreCase));

        List<(string Query, List<byte[]?> Params)> nonSetConfig = [.. server.RecordedBoundQueries
            .Where(q => !q.Query.Contains("set_config('app.current_user_id'", StringComparison.Ordinal))];

        List<(string Query, List<byte[]?> Params)> storedFileInserts = [.. nonSetConfig.Where(q =>
            q.Query.Contains("INSERT INTO storage_entry_metadata", StringComparison.Ordinal))];
        Assert.Equal(5, storedFileInserts.Count);

        // Anonymous insert: first parameter (UserId) forced to SQL NULL
        Assert.Null(storedFileInserts[0].Params[0]);

        // Authenticated non-superuser single inserts: UserId forced to 42L even when spoofing 99L
        Assert.Equal(42L, DecodeInt64Param(storedFileInserts[1].Params[0]!));
        Assert.Equal(42L, DecodeInt64Param(storedFileInserts[2].Params[0]!));

        // Bulk insert with 2 entities: InsertColumns has 8 cols (UserId=0, StorageProvider=1, ..., ContentType=7), so UserId_1 is index 0 and UserId_2 is index 8
        List<byte[]?> bulkParams = storedFileInserts[3].Params;
        Assert.Equal(16, bulkParams.Count);
        Assert.Equal(42L, DecodeInt64Param(bulkParams[0]!));
        Assert.Equal(42L, DecodeInt64Param(bulkParams[8]!));

        // Superuser public insert: explicit NULL preserved
        Assert.Null(storedFileInserts[4].Params[0]);

        // Updates: PK omitted from SET (CreationTimestamp=0, UserId=1, ...), so UserId is index 1 -> non-superuser forced to 42L; superuser preserves 20L
        List<(string Query, List<byte[]?> Params)> storedFileUpdates = [.. nonSetConfig.Where(q =>
            q.Query.Contains("UPDATE storage_entry_metadata", StringComparison.Ordinal))];
        Assert.Equal(2, storedFileUpdates.Count);
        Assert.Equal(42L, DecodeInt64Param(storedFileUpdates[0].Params[1]!));
        Assert.Equal(20L, DecodeInt64Param(storedFileUpdates[1].Params[1]!));
    }

    /// <summary>
    /// Verifies that Polly retries on transient PostgreSQL errors clone pre-built <see cref="NpgsqlParameter"/>
    /// instances instead of re-running reflection on filters/entities, while preserving parameter values
    /// across attempts and keeping every execution in a single-round-trip <see cref="NpgsqlBatch"/>.
    /// </summary>
    [Fact]
    public async Task PostgresRepository_RetriesTransientFailure_ClonesParametersWithoutReReflecting()
    {
        await using FakePostgresWireServer server = new();
        server.Start();

        DatabaseSettings settings = new()
        {
            ConnectionString = server.ConnectionString,
            UnitOperationTimeoutInMilliseconds = 5_000,
            BulkOperationTimeoutInMilliseconds = 10_000
        };
        ResiliencePipelineSettings resilienceSettings = new()
        {
            BackoffType = DelayBackoffType.Constant,
            MaxRetryAttempts = 2,
            DelayInMilliseconds = 1,
            UseJitter = false,
            CircuitBreakerOptions = new()
            {
                FailureRatio = 1.0,
                MinimumThroughput = 1_000,
                SamplingDuration = TimeSpan.FromSeconds(30),
                BreakDuration = TimeSpan.FromSeconds(1)
            }
        };
        PostgresResiliencePipeline resiliencePipeline = new(NullLogger<PostgresResiliencePipeline>.Instance, resilienceSettings);
        ISqlProvider sqlProvider = new PostgresSqlProvider();
        await using NpgsqlDataSource dataSource = DatabaseDependencyInjection.CreateDataSource(settings, NullLoggerFactory.Instance);
        PostgresRepository repository = new(sqlProvider, new CurrentUserProvider(), dataSource, resiliencePipeline, settings);

        // 1. CountAsync retry (filter property read only once)
        CountingItemFilter countFilter = new("RETRY-COUNT");
        server.EnqueueTransientError();
        server.EnqueueScalarLong(7L);
        Assert.Equal(7L, await repository.CountAsync<SimpleItemEntity, CountingItemFilter>(countFilter));
        Assert.Equal(1, countFilter.CodeGetterCalls);

        // 2. ExistAsync(id) retry
        server.EnqueueTransientError();
        server.EnqueueScalarLong(1L);
        Assert.True(await repository.ExistAsync<SimpleItemEntity>(10L));

        // 3. ExistAsync(filter) retry (filter property read only once)
        CountingItemFilter existFilter = new("RETRY-EXIST");
        server.EnqueueTransientError();
        server.EnqueueScalarLong(1L);
        Assert.True(await repository.ExistAsync<SimpleItemEntity, CountingItemFilter>(existFilter));
        Assert.Equal(1, existFilter.CodeGetterCalls);

        // 4. DeleteAsync(id) and DeleteAsync(filter) retry
        server.EnqueueTransientError();
        server.EnqueueNonQuery(1);
        await repository.DeleteAsync<SimpleItemEntity>(10L);

        CountingItemFilter deleteFilter = new("RETRY-DEL");
        server.EnqueueTransientError();
        server.EnqueueNonQuery(1);
        await repository.DeleteAsync<SimpleItemEntity, CountingItemFilter>(deleteFilter);
        Assert.Equal(1, deleteFilter.CodeGetterCalls);

        // 5. GetFirstOrDefaultAsync, GetAsync(id), GetByUniqueColumnAsync retry
        CountingItemFilter firstFilter = new("RETRY-FIRST");
        server.EnqueueTransientError();
        server.EnqueueSimpleItems((11L, "RETRY-FIRST"));
        SimpleItemEntity? first = await repository.GetFirstOrDefaultAsync<SimpleItemEntity, CountingItemFilter>(firstFilter);
        Assert.NotNull(first);
        Assert.Equal(11L, first.Id);
        Assert.Equal(1, firstFilter.CodeGetterCalls);

        server.EnqueueTransientError();
        server.EnqueueSimpleItems((12L, "RETRY-ID"));
        Assert.NotNull(await repository.GetAsync<SimpleItemEntity>(12L));

        server.EnqueueTransientError();
        server.EnqueueSimpleItems((13L, "RETRY-UNIQ"));
        Assert.NotNull(await repository.GetByUniqueColumnAsync<SimpleItemEntity>("RETRY-UNIQ"));

        // 6. InsertAsync(entity) retry (entity property read only once)
        CountingItemEntity insertEntity = new(101L, "RETRY-INS");
        server.EnqueueTransientError();
        server.EnqueueScalarLong(101L);
        Assert.Equal(101L, await repository.InsertAsync(insertEntity));
        Assert.Equal(1, insertEntity.CodeGetterCalls);

        // 7. UpsertAsync(entities[], filter) 3-command batch retry (DELETE fails transiently, then full batch retries)
        CountingItemEntity upsertEntity = new(201L, "RETRY-UPS");
        CountingItemFilter upsertFilter = new("RETRY-OLD");
        server.EnqueueTransientError();
        server.EnqueueNonQuery(1);
        server.EnqueueLongRows(201L);
        long[] upsertIds = await repository.UpsertAsync([upsertEntity], upsertFilter);
        Assert.Equal([201L], upsertIds);
        Assert.Equal(1, upsertEntity.CodeGetterCalls);
        Assert.Equal(1, upsertFilter.CodeGetterCalls);

        // 8. TryUpdateAsync(entity) retry (entity property read only once)
        CountingItemEntity updateEntity = new(301L, "RETRY-UPD");
        server.EnqueueTransientError();
        server.EnqueueNonQuery(1);
        Assert.True(await repository.TryUpdateAsync(updateEntity));
        Assert.Equal(1, updateEntity.CodeGetterCalls);

        // Zero explicit BEGIN/COMMIT commands; every attempt ran as a single-round-trip NpgsqlBatch
        Assert.Equal(0, server.ExplicitTransactionCommandCount);

        // Verify both attempts of CountAsync bound identical cloned parameter values ("RETRY-COUNT")
        List<string> countBoundValues = [.. server.RecordedBoundQueries
            .Where(q => q.Query.Contains("SELECT COUNT(1)", StringComparison.Ordinal) && q.Params.Count == 1)
            .Select(q => Encoding.UTF8.GetString(q.Params[0]!))
            .Where(v => v == "RETRY-COUNT")];
        Assert.Equal(2, countBoundValues.Count);
    }

    private static long DecodeInt64Param(byte[] bytes)
        => bytes.Length == 8
            ? BinaryPrimitives.ReadInt64BigEndian(bytes)
            : long.Parse(Encoding.UTF8.GetString(bytes), CultureInfo.InvariantCulture);

    private sealed class FakePostgresWireServer : IAsyncDisposable
    {
        private readonly TcpListener _listener = new(IPAddress.Loopback, 0);
        private readonly CancellationTokenSource _cts = new();
        private readonly Queue<QueryReply> _replies = new();
        private readonly ConcurrentQueue<string> _setConfigUserIds = new();
        private readonly ConcurrentQueue<(string Query, List<byte[]?> Params)> _boundQueries = new();
        private Task? _acceptLoop;

        public int Port { get; private set; }

        public string ConnectionString =>
            $"Host=127.0.0.1;Port={Port};Username=postgres;Database=test;SSL Mode=Disable;Pooling=false;Server Compatibility Mode=NoTypeLoading";

        public void Start()
        {
            _listener.Start();
            Port = ((IPEndPoint)_listener.LocalEndpoint).Port;
            _acceptLoop = Task.Run(() => AcceptLoopAsync(_cts.Token));
        }

        public void EnqueueScalarLong(long value)
        {
            _replies.Enqueue(new QueryReply(
                Columns: [("val", 20, 8)],
                Rows: [[value]],
                Tag: "SELECT 1"));
        }

        public void EnqueueNonQuery(int affectedRows)
        {
            _replies.Enqueue(new QueryReply(
                Columns: [],
                Rows: [],
                Tag: $"UPDATE {affectedRows}"));
        }

        public void EnqueueLongRows(params long[] ids)
        {
            List<object?[]> rows = [.. ids.Select(id => new object?[] { id })];
            _replies.Enqueue(new QueryReply(
                Columns: [("id", 20, 8)],
                Rows: rows,
                Tag: $"INSERT 0 {ids.Length}"));
        }

        public void EnqueueTryInsertRow(long id, bool inserted)
        {
            _replies.Enqueue(new QueryReply(
                Columns: [("id", 20, 8), ("inserted", 16, 1)],
                Rows: [[id, inserted]],
                Tag: "SELECT 1"));
        }

        public void EnqueueEmptyTryInsert()
        {
            _replies.Enqueue(new QueryReply(
                Columns: [("id", 20, 8), ("inserted", 16, 1)],
                Rows: [],
                Tag: "SELECT 0"));
        }

        public void EnqueueSimpleItems(params (long Id, string Code)[] items)
        {
            List<object?[]> rows = [.. items.Select(x => new object?[] { x.Id, x.Code })];
            _replies.Enqueue(new QueryReply(
                Columns: [("id", 20, 8), ("code", 25, -1)],
                Rows: rows,
                Tag: $"SELECT {items.Length}"));
        }

        public void EnqueueSimpleItemsWithNullFirstRow(params (long? Id, string Code)[] items)
        {
            List<object?[]> rows = [.. items.Select(x => new object?[] { x.Id, x.Code })];
            _replies.Enqueue(new QueryReply(
                Columns: [("id", 20, 8), ("code", 25, -1)],
                Rows: rows,
                Tag: $"SELECT {items.Length}"));
        }

        public void EnqueueError(string sqlState = "23505")
        {
            _replies.Enqueue(new QueryReply(
                Columns: [("id", 20, 8), ("inserted", 16, 1)],
                Rows: [],
                Tag: "ERROR",
                ErrorOnExecute: true,
                ErrorSqlState: sqlState));
        }

        public void EnqueueTransientError(string sqlState = "40001") => EnqueueError(sqlState);

        public void EnqueueNullLongRow()
        {
            _replies.Enqueue(new QueryReply(
                Columns: [("id", 20, 8), ("inserted", 16, 1)],
                Rows: [[null, true]],
                Tag: "SELECT 1"));
        }

        private async Task AcceptLoopAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                TcpClient client;
                try
                {
                    client = await _listener.AcceptTcpClientAsync(ct);
                }
                catch
                {
                    break;
                }

                _ = Task.Run(() => HandleClientAsync(client, ct), ct);
            }
        }

        private async Task HandleClientAsync(TcpClient client, CancellationToken ct)
        {
            using (client)
            await using (NetworkStream stream = client.GetStream())
            {
                // 1. Startup handshake
                byte[] header = new byte[8];
                if (!await ReadExactAsync(stream, header, ct))
                    return;

                int startupLen = BinaryPrimitives.ReadInt32BigEndian(header.AsSpan(0, 4));
                int code = BinaryPrimitives.ReadInt32BigEndian(header.AsSpan(4, 4));
                if (code is 80877103 or 80877104)
                {
                    await stream.WriteAsync(new byte[] { (byte)'N' }, ct);
                    if (!await ReadExactAsync(stream, header, ct))
                        return;
                    startupLen = BinaryPrimitives.ReadInt32BigEndian(header.AsSpan(0, 4));
                }

                byte[] startupBody = new byte[startupLen - 8];
                if (!await ReadExactAsync(stream, startupBody, ct))
                    return;

                await WriteAuthOkAsync(stream, ct);
                await WriteParamStatusAsync(stream, "server_version", "17.0", ct);
                await WriteParamStatusAsync(stream, "client_encoding", "UTF8", ct);
                await WriteParamStatusAsync(stream, "integer_datetimes", "on", ct);
                await WriteBackendKeyDataAsync(stream, ct);
                await WriteReadyForQueryAsync(stream, ct);
                await stream.FlushAsync(ct);

                short[] currentFormatCodes = [];
                string currentParsedQuery = string.Empty;
                QueryReply? activeReply = null;
                bool rowDescSent = false;
                bool ignoringUntilSync = false;
                byte txStatus = (byte)'I';

                byte[] msgHeader = new byte[5];
                while (await ReadExactAsync(stream, msgHeader, ct))
                {
                    char msgType = (char)msgHeader[0];
                    int len = BinaryPrimitives.ReadInt32BigEndian(msgHeader.AsSpan(1, 4));
                    byte[] payload = new byte[len - 4];
                    if (payload.Length > 0 && !await ReadExactAsync(stream, payload, ct))
                        break;

                    if (ignoringUntilSync && msgType is not ('S' or 'X'))
                        continue;

                    switch (msgType)
                    {
                        case 'X':
                            return;

                        case 'Q':
                            string simpleSql = Encoding.UTF8.GetString(payload).TrimEnd('\0').Trim();
                            activeReply = TryHandleControlQuery(simpleSql, ref txStatus) ?? DequeueOrDefault();
                            if (activeReply.Columns.Count > 0)
                            {
                                await WriteRowDescriptionAsync(stream, activeReply.Columns, [], ct);
                                foreach (object?[] row in activeReply.Rows)
                                    await WriteDataRowAsync(stream, activeReply.Columns, row, [], ct);
                            }
                            await WriteCommandCompleteAsync(stream, activeReply.Tag, ct);
                            await WriteReadyForQueryAsync(stream, txStatus, ct);
                            await stream.FlushAsync(ct);
                            activeReply = null;
                            break;

                        case 'P':
                            currentParsedQuery = ExtractParseQuery(payload);
                            activeReply = TryHandleControlQuery(currentParsedQuery, ref txStatus) ?? DequeueOrDefault();
                            rowDescSent = false;
                            await WriteSimpleFrameAsync(stream, '1', [], ct);
                            break;

                        case 'B':
                            (currentFormatCodes, List<byte[]?> boundParams) = ParseBindMessage(payload);
                            _boundQueries.Enqueue((currentParsedQuery, boundParams));
                            if (currentParsedQuery.Contains("set_config('app.current_user_id'", StringComparison.Ordinal) && boundParams.Count > 0)
                                _setConfigUserIds.Enqueue(boundParams[0] is null ? string.Empty : Encoding.UTF8.GetString(boundParams[0]!));
                            await WriteSimpleFrameAsync(stream, '2', [], ct);
                            break;

                        case 'D':
                            activeReply ??= DequeueOrDefault();
                            char descType = (char)payload[0];
                            if (descType == 'S')
                            {
                                byte[] paramDesc = new byte[2];
                                BinaryPrimitives.WriteInt16BigEndian(paramDesc, 0);
                                await WriteSimpleFrameAsync(stream, 't', paramDesc, ct);
                            }
                            if (activeReply.Columns.Count > 0)
                            {
                                await WriteRowDescriptionAsync(stream, activeReply.Columns, currentFormatCodes, ct);
                                rowDescSent = true;
                            }
                            else
                            {
                                await WriteSimpleFrameAsync(stream, 'n', [], ct);
                            }
                            break;

                        case 'E':
                            activeReply ??= DequeueOrDefault();
                            if (activeReply.ErrorOnExecute)
                            {
                                if (txStatus == (byte)'T')
                                    txStatus = (byte)'E';
                                ignoringUntilSync = true;
                                await WriteErrorResponseAsync(stream, activeReply.ErrorSqlState, ct);
                            }
                            else
                            {
                                if (activeReply.Columns.Count > 0 && !rowDescSent)
                                {
                                    await WriteRowDescriptionAsync(stream, activeReply.Columns, currentFormatCodes, ct);
                                    rowDescSent = true;
                                }
                                foreach (object?[] row in activeReply.Rows)
                                    await WriteDataRowAsync(stream, activeReply.Columns, row, currentFormatCodes, ct);
                                await WriteCommandCompleteAsync(stream, activeReply.Tag, ct);
                            }
                            activeReply = null;
                            rowDescSent = false;
                            break;

                        case 'C':
                            await WriteSimpleFrameAsync(stream, '3', [], ct);
                            break;

                        case 'H':
                            await stream.FlushAsync(ct);
                            break;

                        case 'S':
                            ignoringUntilSync = false;
                            Interlocked.Increment(ref _syncMessageCount);
                            await WriteReadyForQueryAsync(stream, txStatus, ct);
                            await stream.FlushAsync(ct);
                            break;
                    }
                }
            }
        }

        public int SetConfigCallCount => _setConfigCallCount;

        public int TransactionLocalSetConfigCount => _transactionLocalSetConfigCount;

        public int ExplicitTransactionCommandCount => _explicitTransactionCommandCount;

        public int SyncMessageCount => _syncMessageCount;

        public IReadOnlyList<string> RecordedSetConfigUserIds => [.. _setConfigUserIds];

        public IReadOnlyList<(string Query, List<byte[]?> Params)> RecordedBoundQueries => [.. _boundQueries];

        private static string ExtractParseQuery(byte[] payload)
        {
            int pos = 0;
            while (pos < payload.Length && payload[pos] != 0) pos++;
            pos++; // skip stmt\0
            int start = pos;
            while (pos < payload.Length && payload[pos] != 0) pos++;
            return Encoding.UTF8.GetString(payload, start, pos - start);
        }

        private int _setConfigCallCount;
        private int _transactionLocalSetConfigCount;
        private int _explicitTransactionCommandCount;
        private int _syncMessageCount;

        private QueryReply? TryHandleControlQuery(string sql, ref byte txStatus)
        {
            string trimmed = sql.Trim();
            if (trimmed.StartsWith("BEGIN", StringComparison.OrdinalIgnoreCase))
            {
                Interlocked.Increment(ref _explicitTransactionCommandCount);
                txStatus = (byte)'T';
                return new QueryReply([], [], "BEGIN");
            }

            if (trimmed.StartsWith("COMMIT", StringComparison.OrdinalIgnoreCase))
            {
                Interlocked.Increment(ref _explicitTransactionCommandCount);
                txStatus = (byte)'I';
                return new QueryReply([], [], "COMMIT");
            }

            if (trimmed.StartsWith("ROLLBACK", StringComparison.OrdinalIgnoreCase))
            {
                Interlocked.Increment(ref _explicitTransactionCommandCount);
                txStatus = (byte)'I';
                return new QueryReply([], [], "ROLLBACK");
            }

            if (trimmed.Contains("set_config('app.current_user_id'", StringComparison.Ordinal))
            {
                Interlocked.Increment(ref _setConfigCallCount);
                if (trimmed.Contains(", true)", StringComparison.OrdinalIgnoreCase))
                    Interlocked.Increment(ref _transactionLocalSetConfigCount);
                return new QueryReply([("set_config", 25, -1)], [[string.Empty]], "SELECT 1");
            }

            return null;
        }

        private QueryReply DequeueOrDefault()
        {
            lock (_replies)
            {
                return _replies.Count > 0
                    ? _replies.Dequeue()
                    : new QueryReply([], [], "SELECT 0");
            }
        }

        private static (short[] ResultFormats, List<byte[]?> ParamValues) ParseBindMessage(byte[] payload)
        {
            int pos = 0;
            while (pos < payload.Length && payload[pos] != 0) pos++;
            pos++; // skip portal\0
            while (pos < payload.Length && payload[pos] != 0) pos++;
            pos++; // skip stmt\0

            short numParamFormats = BinaryPrimitives.ReadInt16BigEndian(payload.AsSpan(pos, 2));
            pos += 2 + (numParamFormats * 2);

            short numParams = BinaryPrimitives.ReadInt16BigEndian(payload.AsSpan(pos, 2));
            pos += 2;
            List<byte[]?> paramValues = new(numParams);
            for (int i = 0; i < numParams; i++)
            {
                int pLen = BinaryPrimitives.ReadInt32BigEndian(payload.AsSpan(pos, 4));
                pos += 4;
                if (pLen < 0)
                {
                    paramValues.Add(null);
                }
                else
                {
                    byte[] valueBytes = new byte[pLen];
                    Buffer.BlockCopy(payload, pos, valueBytes, 0, pLen);
                    paramValues.Add(valueBytes);
                    pos += pLen;
                }
            }

            short numResultFormats = BinaryPrimitives.ReadInt16BigEndian(payload.AsSpan(pos, 2));
            pos += 2;
            short[] formats = new short[numResultFormats];
            for (int i = 0; i < numResultFormats; i++)
            {
                formats[i] = BinaryPrimitives.ReadInt16BigEndian(payload.AsSpan(pos, 2));
                pos += 2;
            }
            return (formats, paramValues);
        }

        private static short[] ParseBindResultFormats(byte[] payload)
        {
            int pos = 0;
            while (pos < payload.Length && payload[pos] != 0) pos++;
            pos++; // skip portal\0
            while (pos < payload.Length && payload[pos] != 0) pos++;
            pos++; // skip stmt\0

            short numParamFormats = BinaryPrimitives.ReadInt16BigEndian(payload.AsSpan(pos, 2));
            pos += 2 + (numParamFormats * 2);

            short numParams = BinaryPrimitives.ReadInt16BigEndian(payload.AsSpan(pos, 2));
            pos += 2;
            for (int i = 0; i < numParams; i++)
            {
                int pLen = BinaryPrimitives.ReadInt32BigEndian(payload.AsSpan(pos, 4));
                pos += 4;
                if (pLen > 0)
                    pos += pLen;
            }

            short numResultFormats = BinaryPrimitives.ReadInt16BigEndian(payload.AsSpan(pos, 2));
            pos += 2;
            short[] formats = new short[numResultFormats];
            for (int i = 0; i < numResultFormats; i++)
            {
                formats[i] = BinaryPrimitives.ReadInt16BigEndian(payload.AsSpan(pos, 2));
                pos += 2;
            }
            return formats;
        }

        private static short GetColumnFormat(short[] resultFormats, int colIndex)
        {
            if (resultFormats.Length == 0)
                return 0;
            if (resultFormats.Length == 1)
                return resultFormats[0];
            return colIndex < resultFormats.Length ? resultFormats[colIndex] : (short)0;
        }

        private static async Task WriteRowDescriptionAsync(
            Stream stream,
            IReadOnlyList<(string Name, int Oid, short Size)> cols,
            short[] resultFormats,
            CancellationToken ct)
        {
            using MemoryStream ms = new();
            byte[] countBytes = new byte[2];
            BinaryPrimitives.WriteInt16BigEndian(countBytes, (short)cols.Count);
            ms.Write(countBytes);

            byte[] meta = new byte[18];
            for (int i = 0; i < cols.Count; i++)
            {
                (string name, int oid, short size) = cols[i];
                ms.Write(Encoding.UTF8.GetBytes(name));
                ms.WriteByte(0);

                short fmt = GetColumnFormat(resultFormats, i);
                BinaryPrimitives.WriteInt32BigEndian(meta.AsSpan(0, 4), 0); // tableOid
                BinaryPrimitives.WriteInt16BigEndian(meta.AsSpan(4, 2), 0); // attrNum
                BinaryPrimitives.WriteInt32BigEndian(meta.AsSpan(6, 4), oid);
                BinaryPrimitives.WriteInt16BigEndian(meta.AsSpan(10, 2), size);
                BinaryPrimitives.WriteInt32BigEndian(meta.AsSpan(12, 4), -1); // typeMod
                BinaryPrimitives.WriteInt16BigEndian(meta.AsSpan(16, 2), fmt);
                ms.Write(meta);
            }

            await WriteSimpleFrameAsync(stream, 'T', ms.ToArray(), ct);
        }

        private static async Task WriteDataRowAsync(
            Stream stream,
            IReadOnlyList<(string Name, int Oid, short Size)> cols,
            object?[] row,
            short[] resultFormats,
            CancellationToken ct)
        {
            using MemoryStream ms = new();
            byte[] countBytes = new byte[2];
            BinaryPrimitives.WriteInt16BigEndian(countBytes, (short)cols.Count);
            ms.Write(countBytes);

            byte[] lenBytes = new byte[4];
            for (int i = 0; i < cols.Count; i++)
            {
                object? val = row[i];
                if (val is null)
                {
                    BinaryPrimitives.WriteInt32BigEndian(lenBytes, -1);
                    ms.Write(lenBytes);
                    continue;
                }

                short fmt = GetColumnFormat(resultFormats, i);
                byte[] encoded = EncodeValue(val, fmt);
                BinaryPrimitives.WriteInt32BigEndian(lenBytes, encoded.Length);
                ms.Write(lenBytes);
                ms.Write(encoded);
            }

            await WriteSimpleFrameAsync(stream, 'D', ms.ToArray(), ct);
        }

        private static byte[] EncodeValue(object val, short fmt)
        {
            if (fmt == 0)
            {
                return val switch
                {
                    bool b => Encoding.UTF8.GetBytes(b ? "t" : "f"),
                    long l => Encoding.UTF8.GetBytes(l.ToString(CultureInfo.InvariantCulture)),
                    int n => Encoding.UTF8.GetBytes(n.ToString(CultureInfo.InvariantCulture)),
                    string s => Encoding.UTF8.GetBytes(s),
                    _ => Encoding.UTF8.GetBytes(Convert.ToString(val, CultureInfo.InvariantCulture) ?? string.Empty)
                };
            }

            return val switch
            {
                bool b => [b ? (byte)1 : (byte)0],
                long l => WriteInt64(l),
                int n => WriteInt32(n),
                string s => Encoding.UTF8.GetBytes(s),
                _ => Encoding.UTF8.GetBytes(Convert.ToString(val, CultureInfo.InvariantCulture) ?? string.Empty)
            };

            static byte[] WriteInt64(long v)
            {
                byte[] b = new byte[8];
                BinaryPrimitives.WriteInt64BigEndian(b, v);
                return b;
            }

            static byte[] WriteInt32(int v)
            {
                byte[] b = new byte[4];
                BinaryPrimitives.WriteInt32BigEndian(b, v);
                return b;
            }
        }

        private static async Task WriteAuthOkAsync(Stream stream, CancellationToken ct)
        {
            byte[] payload = new byte[4];
            await WriteSimpleFrameAsync(stream, 'R', payload, ct);
        }

        private static async Task WriteParamStatusAsync(Stream stream, string key, string value, CancellationToken ct)
        {
            using MemoryStream ms = new();
            ms.Write(Encoding.UTF8.GetBytes(key));
            ms.WriteByte(0);
            ms.Write(Encoding.UTF8.GetBytes(value));
            ms.WriteByte(0);
            await WriteSimpleFrameAsync(stream, 'S', ms.ToArray(), ct);
        }

        private static async Task WriteBackendKeyDataAsync(Stream stream, CancellationToken ct)
        {
            byte[] payload = new byte[8];
            BinaryPrimitives.WriteInt32BigEndian(payload.AsSpan(0, 4), 1);
            BinaryPrimitives.WriteInt32BigEndian(payload.AsSpan(4, 4), 2);
            await WriteSimpleFrameAsync(stream, 'K', payload, ct);
        }

        private static async Task WriteReadyForQueryAsync(Stream stream, byte txStatus, CancellationToken ct)
            => await WriteSimpleFrameAsync(stream, 'Z', [txStatus], ct);

        private static async Task WriteReadyForQueryAsync(Stream stream, CancellationToken ct)
            => await WriteReadyForQueryAsync(stream, (byte)'I', ct);

        private static async Task WriteCommandCompleteAsync(Stream stream, string tag, CancellationToken ct)
        {
            byte[] tagBytes = Encoding.UTF8.GetBytes(tag + "\0");
            await WriteSimpleFrameAsync(stream, 'C', tagBytes, ct);
        }

        private static async Task WriteErrorResponseAsync(Stream stream, string sqlState, CancellationToken ct)
        {
            using MemoryStream ms = new();
            ms.WriteByte((byte)'S');
            ms.Write("ERROR\0"u8);
            ms.WriteByte((byte)'V');
            ms.Write("ERROR\0"u8);
            ms.WriteByte((byte)'C');
            ms.Write(Encoding.UTF8.GetBytes(sqlState + "\0"));
            ms.WriteByte((byte)'M');
            ms.Write("simulated failure\0"u8);
            ms.WriteByte(0);
            await WriteSimpleFrameAsync(stream, 'E', ms.ToArray(), ct);
        }

        private static async Task WriteSimpleFrameAsync(Stream stream, char type, byte[] payload, CancellationToken ct)
        {
            byte[] frame = new byte[5 + payload.Length];
            frame[0] = (byte)type;
            BinaryPrimitives.WriteInt32BigEndian(frame.AsSpan(1, 4), payload.Length + 4);
            payload.CopyTo(frame.AsSpan(5));
            await stream.WriteAsync(frame, ct);
        }

        private static async Task<bool> ReadExactAsync(Stream stream, byte[] buffer, CancellationToken ct)
        {
            int offset = 0;
            while (offset < buffer.Length)
            {
                int read = await stream.ReadAsync(buffer.AsMemory(offset, buffer.Length - offset), ct);
                if (read == 0)
                    return false;
                offset += read;
            }
            return true;
        }

        public async ValueTask DisposeAsync()
        {
            await _cts.CancelAsync();
            _listener.Stop();
            if (_acceptLoop is not null)
            {
                try
                {
                    await _acceptLoop;
                }
                catch
                {
                    // Ignore shutdown cancellation
                }
            }
            _cts.Dispose();
        }

        private sealed record QueryReply(
            IReadOnlyList<(string Name, int Oid, short Size)> Columns,
            IReadOnlyList<object?[]> Rows,
            string Tag,
            bool ErrorOnExecute = false,
            string ErrorSqlState = "23505");
    }
}
