using Amazon.S3;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using System.Net;
using System.Net.Sockets;

namespace MelloSilveiraTools.Core.ResiliencePipelines;

/// <summary>
/// Resilience pipeline for AWS S3 communications providing exponential backoff with jitter and circuit-breaker capabilities.
/// Automatically handles transient cloud errors (throttling, timeouts, service unavailable) while failing fast on permanent errors.
/// </summary>
public class AwsS3ResiliencePipeline : DefaultResiliencePipeline
{
    /// <summary>
    /// Initializes a new instance of <see cref="AwsS3ResiliencePipeline"/>.
    /// </summary>
    /// <param name="logger">Logger used to record retry attempts and failures.</param>
    /// <param name="settings">Pipeline settings driving retry count, backoff, and circuit-breaker behavior.</param>
    public AwsS3ResiliencePipeline(ILogger<AwsS3ResiliencePipeline> logger, ResiliencePipelineSettings settings) : base(logger, settings, new PredicateBuilder()
        .Handle<AmazonS3Exception>(IsTransientS3Error)
        .Handle<HttpRequestException>()
        .Handle<TimeoutException>()
        .Handle<SocketException>()
        .Handle<IOException>())
    { }

    /// <summary>
    /// Initializes a new instance of <see cref="AwsS3ResiliencePipeline"/> with a custom predicate.
    /// </summary>
    /// <param name="logger">See reference at <see cref="ILogger"/>.</param>
    /// <param name="settings">See reference at <see cref="ResiliencePipelineSettings"/>.</param>
    /// <param name="shouldHandle">Predicate that determines whether the retry should be executed for a given outcome.</param>
    public AwsS3ResiliencePipeline(ILogger<AwsS3ResiliencePipeline> logger, ResiliencePipelineSettings settings, Func<RetryPredicateArguments<object>, ValueTask<bool>> shouldHandle) : base(logger, settings, shouldHandle) { }

    /// <summary>
    /// Determines whether a given Amazon S3 exception represents a transient fault eligible for retry.
    /// </summary>
    private static bool IsTransientS3Error(AmazonS3Exception ex) => (ex.StatusCode is HttpStatusCode.InternalServerError or HttpStatusCode.ServiceUnavailable or HttpStatusCode.TooManyRequests)
        || (string.Equals(ex.ErrorCode, "SlowDown", StringComparison.OrdinalIgnoreCase)
            || string.Equals(ex.ErrorCode, "RequestTimeout", StringComparison.OrdinalIgnoreCase)
            || string.Equals(ex.ErrorCode, "RequestTimeTooSkewed", StringComparison.OrdinalIgnoreCase));
}
