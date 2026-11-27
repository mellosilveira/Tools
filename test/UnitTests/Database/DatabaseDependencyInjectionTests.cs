using MelloSilveiraTools.Core.ResiliencePipelines;
using MelloSilveiraTools.Core.Security;
using MelloSilveiraTools.Database;
using MelloSilveiraTools.Database.RelationalDatabase.Repositories;
using MelloSilveiraTools.Database.RelationalDatabase.Settings;
using MelloSilveiraTools.Database.RelationalDatabase.Sql.Provider;
using MelloSilveiraTools.Database.Repositories;
using MelloSilveiraTools.Database.ResiliencePipelines;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using Polly;

namespace UnitTests.Database;

/// <summary>
/// Unit tests for <see cref="DatabaseDependencyInjection.AddDatabaseServices"/> verifying DI registration
/// of database settings, singleton <see cref="NpgsqlDataSource"/>, resilience pipelines, SQL provider, and repository.
/// </summary>
public sealed class DatabaseDependencyInjectionTests
{
    private static readonly ResiliencePipelineSettings TestResilienceSettings = new()
    {
        BackoffType = DelayBackoffType.Constant,
        MaxRetryAttempts = 1,
        DelayInMilliseconds = 1,
        UseJitter = false
    };

    private static readonly DatabaseSettings TestDatabaseSettings = new()
    {
        ConnectionString = "Host=localhost;Database=testdb;Username=test;Password=test;",
        UnitOperationTimeoutInMilliseconds = 30000,
        BulkOperationTimeoutInMilliseconds = 120000
    };

    /// <summary>
    /// Verifies that AddDatabaseServices registers settings, singleton NpgsqlDataSource, resilience pipeline, SQL provider, and repository singletons.
    /// </summary>
    [Fact]
    public async Task AddDatabaseServices_RegistersExpectedSingletons()
    {
        ServiceCollection services = new();
        services.AddLogging();
        services.AddSingleton<ICurrentUserProvider, CurrentUserProvider>();

        services.AddDatabaseServices(
            defaultResiliencePipelineSettings: TestResilienceSettings,
            databaseSettings: TestDatabaseSettings);

        await using ServiceProvider provider = services.BuildServiceProvider();

        DatabaseSettings? resolvedSettings = provider.GetService<DatabaseSettings>();
        NpgsqlDataSource? dataSource1 = provider.GetService<NpgsqlDataSource>();
        NpgsqlDataSource? dataSource2 = provider.GetService<NpgsqlDataSource>();
        PostgresResiliencePipeline? resolvedPipeline = provider.GetService<PostgresResiliencePipeline>();
        ISqlProvider? resolvedSqlProvider = provider.GetService<ISqlProvider>();
        IRepository? resolvedRepository = provider.GetService<IRepository>();

        Assert.NotNull(resolvedSettings);
        Assert.Same(TestDatabaseSettings, resolvedSettings);
        Assert.NotNull(dataSource1);
        Assert.Same(dataSource1, dataSource2);
        Assert.NotNull(resolvedPipeline);
        Assert.NotNull(resolvedSqlProvider);
        Assert.IsType<PostgresSqlProvider>(resolvedSqlProvider);
        Assert.NotNull(resolvedRepository);
        Assert.IsType<PostgresRepository>(resolvedRepository);

        ServiceCollection overrideServices = new();
        overrideServices.AddLogging();
        overrideServices.AddDatabaseServices(
            TestResilienceSettings,
            TestDatabaseSettings with { ResiliencePipelineSettings = TestResilienceSettings });
        await using ServiceProvider overrideProvider = overrideServices.BuildServiceProvider();
        Assert.NotNull(overrideProvider.GetService<PostgresResiliencePipeline>());
        Assert.NotNull(overrideProvider.GetService<NpgsqlDataSource>());
    }

    /// <summary>
    /// Verifies that <see cref="DatabaseDependencyInjection.CreateDataSource"/> builds valid data sources both with and without an explicit logger factory.
    /// </summary>
    [Fact]
    public async Task CreateDataSource_BuildsDataSource_WithAndWithoutLoggerFactory()
    {
        await using NpgsqlDataSource withoutLogger = DatabaseDependencyInjection.CreateDataSource(TestDatabaseSettings);
        await using NpgsqlDataSource withLogger = DatabaseDependencyInjection.CreateDataSource(TestDatabaseSettings, NullLoggerFactory.Instance);

        Assert.NotNull(withoutLogger);
        Assert.NotNull(withLogger);
        Assert.Contains("Host=localhost", withoutLogger.ConnectionString, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Database=testdb", withLogger.ConnectionString, StringComparison.OrdinalIgnoreCase);
    }
}
