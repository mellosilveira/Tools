using MelloSilveiraTools.Database.ExtensionMethods;
using MelloSilveiraTools.Database.RelationalDatabase.Settings;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace MelloSilveiraTools.Database.RelationalDatabase.HealthChecks;

/// <summary>
/// Native ASP.NET Core <see cref="IHealthCheck"/> probing PostgreSQL connectivity via
/// <see cref="NpgsqlDataSource"/> using the configured <see cref="DatabaseSettings.UnitOperationTimeoutInSeconds"/>.
/// </summary>
/// <param name="logger">Logger recording connectivity failures.</param>
/// <param name="dataSource">Shared PostgreSQL data source probed with <c>SELECT 1</c>.</param>
/// <param name="databaseSettings">Database settings supplying the unit-operation command timeout.</param>
public sealed class PostgresHealthCheck(
    ILogger<PostgresHealthCheck> logger,
    NpgsqlDataSource dataSource,
    DatabaseSettings databaseSettings) : IHealthCheck
{
    /// <summary>
    /// Default registration name used when adding <see cref="PostgresHealthCheck"/> to ASP.NET Core health checks.
    /// </summary>
    public const string DefaultName = "postgresql";

    /// <inheritdoc/>
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await dataSource.PingAsync(databaseSettings.UnitOperationTimeoutInSeconds, cancellationToken).ConfigureAwait(false);
            return HealthCheckResult.Healthy("PostgreSQL connection is healthy.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Health check '{HealthCheckName}' failed while connecting to PostgreSQL.", context.Registration.Name);
            return HealthCheckResult.Unhealthy("PostgreSQL connectivity check failed.", ex);
        }
    }
}
