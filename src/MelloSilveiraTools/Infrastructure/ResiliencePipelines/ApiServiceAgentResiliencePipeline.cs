using MelloSilveiraTools.Infrastructure.Logger;
using MelloSilveiraTools.UseCases.Operations;
using Polly;
using Polly.Retry;
using System.Net;

namespace MelloSilveiraTools.Infrastructure.ResiliencePipelines;

/// <summary>
/// Resilience pipeline for PostgreSQL repositories that provides a strategy to handle retriable exceptions.
/// </summary>
public class ApiServiceAgentResiliencePipeline : DefaultResiliencePipeline
{
    private static readonly List<HttpStatusCode> StatusCodesToRetry = [HttpStatusCode.InternalServerError, HttpStatusCode.ServiceUnavailable];
    
    /// <summary>
    /// Initialize a new instance of <see cref="PostgresResiliencePipeline"/>.
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="settings"></param>
    public ApiServiceAgentResiliencePipeline(ILogger logger, ResiliencePipelineSettings settings)
        : base(logger, settings, new PredicateBuilder()
            .Handle<Exception>()
            .HandleResult(new Func<OperationResponse, bool>(r => StatusCodesToRetry.Contains(r.StatusCode))))
    { }

    /// <summary>
    /// Initializes a new instance of <see cref="PostgresResiliencePipeline"/>.
    /// </summary>
    /// <param name="logger">See reference at <see cref="ILogger"/>.</param>
    /// <param name="settings">See reference at <see cref="ResiliencePipelineSettings"/>.</param>
    /// <param name="shouldHandle">Predicate that determines whether the retry should be executed for a given outcome.</param>
    public ApiServiceAgentResiliencePipeline(ILogger logger, ResiliencePipelineSettings settings, Func<RetryPredicateArguments<object>, ValueTask<bool>> shouldHandle) : base(logger, settings, shouldHandle) { }
}
