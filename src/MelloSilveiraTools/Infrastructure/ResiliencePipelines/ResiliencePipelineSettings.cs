using Polly;

namespace MelloSilveiraTools.Infrastructure.ResiliencePipelines;

/// <summary>
/// Settings for <see cref="ResiliencePipeline"/>.
/// </summary>
public record ResiliencePipelineSettings
{
    /// <inheritdoc cref="DelayBackoffType"/>
    public DelayBackoffType BackoffType { get; init; }

    public int DelayInMilliseconds { get; init; }

    public int MaxRetryAttempts { get; init; }
    public bool UseJitter { get; internal set; }
}
