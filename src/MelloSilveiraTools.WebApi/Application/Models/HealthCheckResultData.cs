using System.Text.Json.Serialization;

namespace MelloSilveiraTools.WebApi.Application.Models;

/// <summary>
/// Structured HTTP payload returned by the native ASP.NET Core health-check endpoint.
/// </summary>
public sealed record HealthCheckResultData
{
    /// <summary>
    /// Aggregate health status across all evaluated checks (<c>Healthy</c>, <c>Degraded</c>, or <c>Unhealthy</c>).
    /// </summary>
    public required string Status { get; init; }

    /// <summary>
    /// Total elapsed execution time in milliseconds across all evaluated checks.
    /// </summary>
    public required double TotalDurationMs { get; init; }

    /// <summary>
    /// Individual health-check outcomes keyed by registration name.
    /// </summary>
    public required IReadOnlyDictionary<string, HealthCheckEntryResultData> Checks { get; init; }
}

/// <summary>
/// Individual health-check entry included in <see cref="HealthCheckResultData.Checks"/>.
/// </summary>
public sealed record HealthCheckEntryResultData
{
    /// <summary>
    /// Health status reported by this check (<c>Healthy</c>, <c>Degraded</c>, or <c>Unhealthy</c>).
    /// </summary>
    public required string Status { get; init; }

    /// <summary>
    /// Execution duration of this check in milliseconds.
    /// </summary>
    public required double DurationMs { get; init; }

    /// <summary>
    /// Optional human-readable description returned by the health check.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Description { get; init; }

    /// <summary>
    /// Optional error message populated when the health check fails with an exception.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Error { get; init; }

    /// <summary>
    /// Optional structured status metadata reported by the health check (such as individual storage provider statuses).
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyDictionary<string, object>? Data { get; init; }
}
