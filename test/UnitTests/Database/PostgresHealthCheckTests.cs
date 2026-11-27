using MelloSilveiraTools.Database;
using MelloSilveiraTools.Database.ExtensionMethods;
using MelloSilveiraTools.Database.RelationalDatabase.HealthChecks;
using MelloSilveiraTools.Database.RelationalDatabase.Settings;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;

namespace UnitTests.Database;

public sealed class PostgresHealthCheckTests
{
    private static DatabaseSettings CreateUnreachableSettings() => new()
    {
        ConnectionString = "Host=127.0.0.1;Port=1;Database=none;Username=none;Password=none;Timeout=1",
        UnitOperationTimeoutInMilliseconds = 1000,
        BulkOperationTimeoutInMilliseconds = 2000
    };

    [Fact]
    public async Task CheckHealthAsync_WhenDatabaseIsUnreachable_ReturnsUnhealthyWithException()
    {
        DatabaseSettings settings = CreateUnreachableSettings();
        await using NpgsqlDataSource dataSource = DatabaseDependencyInjection.CreateDataSource(settings);
        PostgresHealthCheck healthCheck = new(NullLogger<PostgresHealthCheck>.Instance, dataSource, settings);
        HealthCheckContext context = new()
        {
            Registration = new HealthCheckRegistration(PostgresHealthCheck.DefaultName, healthCheck, HealthStatus.Unhealthy, null)
        };

        HealthCheckResult result = await healthCheck.CheckHealthAsync(context);

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Equal("PostgreSQL connectivity check failed.", result.Description);
        Assert.NotNull(result.Exception);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenCancellationIsRequested_PropagatesOperationCanceledException()
    {
        DatabaseSettings settings = CreateUnreachableSettings();
        await using NpgsqlDataSource dataSource = DatabaseDependencyInjection.CreateDataSource(settings);
        PostgresHealthCheck healthCheck = new(NullLogger<PostgresHealthCheck>.Instance, dataSource, settings);
        HealthCheckContext context = new()
        {
            Registration = new HealthCheckRegistration(PostgresHealthCheck.DefaultName, healthCheck, HealthStatus.Unhealthy, null)
        };

        using CancellationTokenSource cts = new();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => healthCheck.CheckHealthAsync(context, cts.Token));
    }

    [Fact]
    public async Task PingAsync_WhenCancelledOrUnreachable_ThrowsExpectedException()
    {
        DatabaseSettings settings = CreateUnreachableSettings();
        await using NpgsqlDataSource dataSource = DatabaseDependencyInjection.CreateDataSource(settings);

        using CancellationTokenSource cts = new();
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => dataSource.PingAsync(1, cts.Token));

        await Assert.ThrowsAnyAsync<Exception>(() => dataSource.PingAsync(1));
    }
}
