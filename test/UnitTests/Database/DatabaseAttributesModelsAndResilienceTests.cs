using MelloSilveiraTools.Core.Files.Models;
using MelloSilveiraTools.Core.ResiliencePipelines;
using MelloSilveiraTools.Database.RelationalDatabase.Attributes;
using MelloSilveiraTools.Database.RelationalDatabase.Models;
using MelloSilveiraTools.Database.RelationalDatabase.Models.Entities;
using MelloSilveiraTools.Database.RelationalDatabase.Models.Filters;
using MelloSilveiraTools.Database.RelationalDatabase.Settings;
using MelloSilveiraTools.Database.ResiliencePipelines;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using Polly;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Reflection;

namespace UnitTests.Database;

public sealed class DatabaseAttributesModelsAndResilienceTests
{
    [Table("fk_without_table")]
    private sealed record EntityWithUnattributedFk : EntityBase
    {
        [ForeignKeyColumn(typeof(NoTableEntity), JoinType.Left)]
        public long RefId { get; init; }
    }

    [Fact]
    public void Attributes_ExposeConfiguredMetadataAndForeignKeys()
    {
        ColumnAttribute col = new();
        Assert.NotNull(col);

        UniqueColumnAttribute uniq = new();
        Assert.IsAssignableFrom<ColumnAttribute>(uniq);

        PrimaryKeyColumnAttribute pk = new();
        Assert.IsAssignableFrom<ColumnAttribute>(pk);

        ForeignKeyColumnAttribute fk = new(typeof(ProductEntity), JoinType.Right);
        Assert.Equal("RIGHT", fk.JoinType);
        Assert.Equal(typeof(ProductEntity), fk.ReferencedTableType);

        FilterColumnAttribute filterCol = new(FilterClause.GreaterThanOrEqual, "Price", "product");
        Assert.Equal(">=", filterCol.FilterClause);
        Assert.Equal("Price", filterCol.PropertyName);
        Assert.Equal("product", filterCol.TableName);

        FilterAttribute orderItemFilterAttr = new(typeof(OrderItemEntity));
        Assert.Equal("order_item", orderItemFilterAttr.TableDefinition.Name);
        Assert.True(orderItemFilterAttr.JoinTablesDefinition.ContainsKey("product"));

        FilterAttribute noTableFkAttr = new(typeof(EntityWithUnattributedFk));
        Assert.Empty(noTableFkAttr.JoinTablesDefinition);
    }

    [Fact]
    public void ModelsAndSettings_StorePropertiesAndFactoryValues()
    {
        DateTimeOffset now = new(2026, 5, 10, 8, 30, 0, TimeSpan.Zero);
        StorageEntryMetadata fileMeta = new(StorageProvider.Local, "storage/exp/run1.csv", "storage/exp", "run1.csv", 2048, now);

        StorageEntryMetadataEntity entity = StorageEntryMetadataEntity.Create(fileMeta, "Sim_01", "application/json");
        Assert.Equal("Sim_01", entity.ExternalIdentifier);
        Assert.Equal(StorageProvider.Local, entity.StorageProvider);
        Assert.Equal("storage/exp/run1.csv", entity.Location);
        Assert.Equal("storage/exp", entity.Directory);
        Assert.Equal("run1.csv", entity.FileName);
        Assert.Equal(2048, entity.SizeBytes);
        Assert.Equal("application/json", entity.ContentType);

        Pagination pagination = new() { SortOrder = SortOrder.Desc, Limit = 25, Offset = 50 };
        Assert.Equal(SortOrder.Desc, pagination.SortOrder);
        Assert.Equal(25, pagination.Limit);
        Assert.Equal(50, pagination.Offset);

        StorageEntryMetadataFilter filter = new()
        {
            ExternalIdentifier = "ext",
            StorageProvider = StorageProvider.Local,
            Location = "storage/exp/data.csv",
            Directory = "storage/exp",
            FileName = "data.csv",
            ContentType = "text/csv"
        };
        Assert.Equal("ext", filter.ExternalIdentifier);
        Assert.Equal(StorageProvider.Local, filter.StorageProvider);
        Assert.Equal("storage/exp/data.csv", filter.Location);
        Assert.Equal("storage/exp", filter.Directory);
        Assert.Equal("data.csv", filter.FileName);
        Assert.Equal("text/csv", filter.ContentType);

        DatabaseSettings settings = new()
        {
            ConnectionString = "Host=localhost;Database=db;",
            UnitOperationTimeoutInMilliseconds = 5000,
            BulkOperationTimeoutInMilliseconds = 60000
        };
        Assert.Equal("Host=localhost;Database=db;", settings.ConnectionString);
        Assert.Equal(5000, settings.UnitOperationTimeoutInMilliseconds);
        Assert.Equal(60000, settings.BulkOperationTimeoutInMilliseconds);
    }

    [Fact]
    public async Task PostgresResiliencePipeline_RetriesTransientExceptionsAndSupportsCustomPredicate()
    {
        ResiliencePipelineSettings settings = new()
        {
            BackoffType = DelayBackoffType.Constant,
            MaxRetryAttempts = 2,
            DelayInMilliseconds = 1,
            UseJitter = false
        };

        PostgresResiliencePipeline defaultPipeline = new(NullLogger<PostgresResiliencePipeline>.Instance, settings);

        int timeoutAttempts = 0;
        await Assert.ThrowsAsync<TimeoutException>(() => defaultPipeline.ExecuteAsync(_ =>
        {
            timeoutAttempts++;
            return ValueTask.FromException(new TimeoutException("timed out"));
        }).AsTask());
        Assert.Equal(3, timeoutAttempts);

        int socketAttempts = 0;
        await Assert.ThrowsAsync<SocketException>(() => defaultPipeline.ExecuteAsync(_ =>
        {
            socketAttempts++;
            return ValueTask.FromException(new SocketException(10054));
        }).AsTask());
        Assert.Equal(3, socketAttempts);

        int nonTransientAttempts = 0;
        await Assert.ThrowsAsync<InvalidOperationException>(() => defaultPipeline.ExecuteAsync(_ =>
        {
            nonTransientAttempts++;
            return ValueTask.FromException(new InvalidOperationException("fatal"));
        }).AsTask());
        Assert.Equal(1, nonTransientAttempts);

        PostgresResiliencePipeline customPipeline = new(
            NullLogger<PostgresResiliencePipeline>.Instance,
            settings,
            args => ValueTask.FromResult(args.Outcome.Exception is InvalidOperationException));

        int customAttempts = 0;
        string result = await customPipeline.ExecuteAsync(_ =>
        {
            customAttempts++;
            if (customAttempts == 1)
                return ValueTask.FromException<string>(new InvalidOperationException("retry once"));
            return ValueTask.FromResult("ok");
        });
        Assert.Equal("ok", result);
        Assert.Equal(2, customAttempts);

        // Also cover PostgresException (transient, protocol violation, and fatal) and NpgsqlException (transient and fatal)
        PostgresResiliencePipeline pgTransientPipeline = new(NullLogger<PostgresResiliencePipeline>.Instance, settings);
        int pgTransientAttempts = 0;
        await Assert.ThrowsAsync<PostgresException>(() => pgTransientPipeline.ExecuteAsync(_ =>
        {
            pgTransientAttempts++;
            return ValueTask.FromException(new PostgresException("conn fail", "FATAL", "FATAL", PostgresErrorCodes.ConnectionFailure));
        }).AsTask());
        Assert.Equal(3, pgTransientAttempts);

        PostgresResiliencePipeline pgProtocolPipeline = new(NullLogger<PostgresResiliencePipeline>.Instance, settings);
        int pgProtocolAttempts = 0;
        await Assert.ThrowsAsync<PostgresException>(() => pgProtocolPipeline.ExecuteAsync(_ =>
        {
            pgProtocolAttempts++;
            return ValueTask.FromException(new PostgresException("proto", "FATAL", "FATAL", PostgresErrorCodes.ProtocolViolation));
        }).AsTask());
        Assert.Equal(3, pgProtocolAttempts);

        PostgresResiliencePipeline pgFatalPipeline = new(NullLogger<PostgresResiliencePipeline>.Instance, settings);
        int pgFatalAttempts = 0;
        await Assert.ThrowsAsync<PostgresException>(() => pgFatalPipeline.ExecuteAsync(_ =>
        {
            pgFatalAttempts++;
            return ValueTask.FromException(new PostgresException("unique", "ERROR", "ERROR", PostgresErrorCodes.UniqueViolation));
        }).AsTask());
        Assert.Equal(1, pgFatalAttempts);

        PostgresResiliencePipeline npgsqlTransientPipeline = new(NullLogger<PostgresResiliencePipeline>.Instance, settings);
        int npgsqlTransientAttempts = 0;
        await Assert.ThrowsAsync<NpgsqlException>(() => npgsqlTransientPipeline.ExecuteAsync(_ =>
        {
            npgsqlTransientAttempts++;
            return ValueTask.FromException(new NpgsqlException("transient", new TimeoutException()));
        }).AsTask());
        Assert.Equal(3, npgsqlTransientAttempts);

        PostgresResiliencePipeline npgsqlFatalPipeline = new(NullLogger<PostgresResiliencePipeline>.Instance, settings);
        int npgsqlFatalAttempts = 0;
        await Assert.ThrowsAsync<NpgsqlException>(() => npgsqlFatalPipeline.ExecuteAsync(_ =>
        {
            npgsqlFatalAttempts++;
            return ValueTask.FromException(new NpgsqlException("fatal"));
        }).AsTask());
        Assert.Equal(1, npgsqlFatalAttempts);

        // Cover SqlResource constructor and Culture property via reflection
        Type? sqlResourceType = typeof(DatabaseSettings).Assembly.GetType("MelloSilveiraTools.Database.RelationalDatabase.Sql.Provider.SqlResource");
        Assert.NotNull(sqlResourceType);
        object? sqlResInstance = Activator.CreateInstance(sqlResourceType!, nonPublic: true);
        Assert.NotNull(sqlResInstance);
        PropertyInfo? cultureProp = sqlResourceType!.GetProperty("Culture", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.NotNull(cultureProp);
        cultureProp!.SetValue(null, CultureInfo.InvariantCulture);
        Assert.Equal(CultureInfo.InvariantCulture, cultureProp.GetValue(null));
        cultureProp.SetValue(null, null);
    }
}
