using Azure;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using System.Net;
using System.Net.Sockets;

namespace MelloSilveiraTools.Core.ResiliencePipelines;

/// <summary>
/// Resilience pipeline for Azure Blob Storage communications providing exponential backoff with jitter and circuit-breaker capabilities.
/// Automatically handles transient cloud errors (throttling, timeouts, service unavailable) while failing fast on permanent errors.
/// </summary>
public class BlobStorageResiliencePipeline : DefaultResiliencePipeline
{
    /// <summary>
    /// Initializes a new instance of <see cref="BlobStorageResiliencePipeline"/>.
    /// </summary>
    /// <param name="logger">Logger used to record retry attempts and failures.</param>
    /// <param name="settings">Pipeline settings driving retry count, backoff, and circuit-breaker behavior.</param>
    public BlobStorageResiliencePipeline(ILogger<BlobStorageResiliencePipeline> logger, ResiliencePipelineSettings settings) : base(logger, settings, new PredicateBuilder()
        .Handle<RequestFailedException>(IsTransientAzureError)
        .Handle<HttpRequestException>()
        .Handle<TimeoutException>()
        .Handle<SocketException>()
        .Handle<IOException>())
    { }

    /// <summary>
    /// Initializes a new instance of <see cref="BlobStorageResiliencePipeline"/> with a custom predicate.
    /// </summary>
    /// <param name="logger">See reference at <see cref="ILogger"/>.</param>
    /// <param name="settings">See reference at <see cref="ResiliencePipelineSettings"/>.</param>
    /// <param name="shouldHandle">Predicate that determines whether the retry should be executed for a given outcome.</param>
    public BlobStorageResiliencePipeline(ILogger<BlobStorageResiliencePipeline> logger, ResiliencePipelineSettings settings, Func<RetryPredicateArguments<object>, ValueTask<bool>> shouldHandle) : base(logger, settings, shouldHandle) { }

    /// <summary>
    /// Determines whether a given Azure <see cref="RequestFailedException"/> represents a transient fault eligible for retry.
    /// </summary>
    private static bool IsTransientAzureError(RequestFailedException ex) => ex.Status is (int)HttpStatusCode.RequestTimeout
            or (int)HttpStatusCode.TooManyRequests
            or (int)HttpStatusCode.InternalServerError
            or (int)HttpStatusCode.BadGateway
            or (int)HttpStatusCode.ServiceUnavailable
            or (int)HttpStatusCode.GatewayTimeout
        || string.Equals(ex.ErrorCode, "ServerBusy", StringComparison.OrdinalIgnoreCase)
        || string.Equals(ex.ErrorCode, "OperationTimedOut", StringComparison.OrdinalIgnoreCase)
        || string.Equals(ex.ErrorCode, "InternalError", StringComparison.OrdinalIgnoreCase);
}
