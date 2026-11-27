using Polly;
using Polly.CircuitBreaker;
using Polly.RateLimiting;
using Polly.Retry;
using System.Threading.RateLimiting;

namespace MelloSilveiraTools.Core.ResiliencePipelines;

/// <summary>
/// Settings for <see cref="ResiliencePipeline"/>.
/// </summary>
public record ResiliencePipelineSettings
{
    /// <inheritdoc cref="DelayBackoffType"/>
    public required DelayBackoffType BackoffType { get; init; }

    /// <summary>
    /// Delay between retries in milliseconds.
    /// </summary>
    public required int DelayInMilliseconds { get; init; }

    /// <inheritdoc cref="RetryStrategyOptions{Object}.MaxRetryAttempts"/>
    public required int MaxRetryAttempts { get; init; }

    /// <inheritdoc cref="RetryStrategyOptions{Object}.UseJitter"/>
    public required bool UseJitter { get; init; }

    /// <summary>
    /// Optional circuit breaker strategy options. If not provided, sensible defaults are configured.
    /// </summary>
    public CircuitBreakerStrategyOptions? CircuitBreakerOptions { get; init; }

    /// <summary>
    /// Optional concurrency limiter options. If not provided, sensible defaults are configured.
    /// </summary>
    public ConcurrencyLimiterOptions? ConcurrencyLimiterOptions { get; init; }

    /// <summary>
    /// Optional rate limiter strategy options. If not provided, sensible defaults are configured.
    /// </summary>
    public RateLimiterStrategyOptions? RateLimiterOptions { get; init; }
}
