using MelloSilveiraTools.Core.Commands;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.WebApi.Application.Models;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace MelloSilveiraTools.WebApi.Application.Commands.Health;

/// <summary>
/// Evaluates all registered ASP.NET Core <see cref="IHealthCheck"/> dependencies (aggregated across
/// <c>Core</c>, <c>Database</c>, and host-registered checks) via <see cref="HealthCheckService"/> and maps
/// the resulting <see cref="HealthReport"/> into a structured <see cref="Result{T}"/> carrying
/// <see cref="HealthCheckResultData"/> (<c>200 OK</c> for Healthy/Degraded; <c>503 Service Unavailable</c> for Unhealthy).
/// </summary>
/// <param name="healthCheckService">Native ASP.NET Core health-check orchestrator aggregating every package's checks.</param>
public class CheckHealthCommand(HealthCheckService healthCheckService) : CommandBaseWithoutRequest<HealthCheckResultData>()
{
    /// <inheritdoc/>
    protected override async Task<Result<HealthCheckResultData>> ExecuteCommandAsync(CancellationToken cancellationToken = default)
    {
        HealthReport report = await healthCheckService.CheckHealthAsync(cancellationToken).ConfigureAwait(false);

        Dictionary<string, HealthCheckEntryResultData> checks = new(report.Entries.Count, StringComparer.OrdinalIgnoreCase);
        foreach (KeyValuePair<string, HealthReportEntry> entry in report.Entries)
        {
            checks[entry.Key] = new HealthCheckEntryResultData
            {
                Status = entry.Value.Status.ToString(),
                DurationMs = entry.Value.Duration.TotalMilliseconds,
                Description = entry.Value.Description,
                Error = entry.Value.Exception?.Message,
                Data = BuildSanitizedData(entry.Value.Data)
            };
        }

        HealthCheckResultData payload = new()
        {
            Status = report.Status.ToString(),
            TotalDurationMs = report.TotalDuration.TotalMilliseconds,
            Checks = checks
        };

        return report.Status == HealthStatus.Unhealthy ? Result.CreateServiceUnavailable(payload) : Result.CreateSuccessOk(payload);
    }

    private static IReadOnlyDictionary<string, object>? BuildSanitizedData(IReadOnlyDictionary<string, object> data)
    {
        if (data.Count == 0)
            return null;

        Dictionary<string, object> sanitized = new(data.Count, StringComparer.OrdinalIgnoreCase);
        foreach (KeyValuePair<string, object> item in data)
        {
            sanitized[item.Key] = item.Value is Exception ex ? ex.Message : item.Value;
        }

        return sanitized;
    }
}
