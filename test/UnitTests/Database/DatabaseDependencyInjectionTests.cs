using MelloSilveiraTools.Core.ResiliencePipelines;
using MelloSilveiraTools.Database;
using MelloSilveiraTools.Database.RelationalDatabase.Repositories;
using MelloSilveiraTools.Database.RelationalDatabase.Settings;
using MelloSilveiraTools.Database.RelationalDatabase.Sql.Provider;
using MelloSilveiraTools.Database.Repositories;
using MelloSilveiraTools.Database.ResiliencePipelines;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;

namespace UnitTests.Database;

/// <summary>
/// Unit tests for <see cref="DatabaseDependencyInjection.AddDatabaseServices"/> verifying DI registration
/// of database settings, resilience pipelines, SQL provider, and repository.
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
    /// Verifies that AddDatabaseServices registers settings, resilience pipeline, SQL provider, and repository singletons.
    /// </summary>
    [Fact]
    public void AddDatabaseServices_RegistersExpectedSingletons()
    {
        // Arrange
        ServiceCollection services = new();
        services.AddLogging();

        // Act
        services.AddDatabaseServices(
            defaultResiliencePipelineSettings: TestResilienceSettings,
            databaseSettings: TestDatabaseSettings);

        ServiceProvider provider = services.BuildServiceProvider();

        // Assert
        DatabaseSettings? resolvedSettings = provider.GetService<DatabaseSettings>();
        PostgresResiliencePipeline? resolvedPipeline = provider.GetService<PostgresResiliencePipeline>();
        ISqlProvider? resolvedSqlProvider = provider.GetService<ISqlProvider>();
        IRepository? resolvedRepository = provider.GetService<IRepository>();

        Assert.NotNull(resolvedSettings);
        Assert.Same(TestDatabaseSettings, resolvedSettings);
        Assert.NotNull(resolvedPipeline);
        Assert.NotNull(resolvedSqlProvider);
        Assert.IsType<PostgresSqlProvider>(resolvedSqlProvider);
        Assert.NotNull(resolvedRepository);
        Assert.IsType<PostgresRepository>(resolvedRepository);
    }
}
