using MelloSilveiraTools.Infrastructure.Logger;
using Npgsql;
using Polly;
using Polly.Retry;
using System.Net.Sockets;

namespace MelloSilveiraTools.Infrastructure.ResiliencePipelines;

/// <summary>
/// Resilience pipeline for PostgreSQL repositories that provides a strategy to handle retriable exceptions.
/// </summary>
public class PostgresResiliencePipeline : DefaultResiliencePipeline
{
    /// <summary>
    /// Initialize a new instance of <see cref="PostgresResiliencePipeline"/>.
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="settings"></param>
    public PostgresResiliencePipeline(ILogger logger, ResiliencePipelineSettings settings)
        : base(logger, settings, new PredicateBuilder()
            .Handle<PostgresException>(ex => ex.IsTransient || ex.SqlState == PostgresErrorCodes.ProtocolViolation /* PgBouncer error */)
            .Handle<NpgsqlException>(ex => ex.IsTransient)
            .Handle<TimeoutException>()
            .Handle<SocketException>())
    { }

    /// <summary>
    /// Initializes a new instance of <see cref="PostgresResiliencePipeline"/>.
    /// </summary>
    /// <param name="logger">See reference at <see cref="ILogger"/>.</param>
    /// <param name="settings">See reference at <see cref="ResiliencePipelineSettings"/>.</param>
    /// <param name="shouldHandle">Predicate that determines whether the retry should be executed for a given outcome.</param>
    public PostgresResiliencePipeline(ILogger logger, ResiliencePipelineSettings settings, Func<RetryPredicateArguments<object>, ValueTask<bool>> shouldHandle) : base(logger, settings, shouldHandle) { }
}
