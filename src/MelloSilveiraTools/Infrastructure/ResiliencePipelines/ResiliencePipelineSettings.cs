using Polly;
using Polly.Retry;

namespace MelloSilveiraTools.Infrastructure.ResiliencePipelines;

/// <summary>
/// Settings for <see cref="ResiliencePipeline"/>.
/// </summary>
public record ResiliencePipelineSettings
{
    /// <inheritdoc cref="DelayBackoffType"/>
    public DelayBackoffType BackoffType { get; init; }

    /// <summary>
    /// Delay between retries in milliseconds.
    /// </summary>
    public int DelayInMilliseconds { get; init; }

    /// <inheritdoc cref="RetryStrategyOptions{Object}.MaxRetryAttempts"/>
    public int MaxRetryAttempts { get; init; }

    /// <inheritdoc cref="RetryStrategyOptions{Object}.UseJitter"/>
    public bool UseJitter { get; internal set; }
}
