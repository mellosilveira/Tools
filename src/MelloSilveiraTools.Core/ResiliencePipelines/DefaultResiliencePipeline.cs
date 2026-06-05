using Microsoft.Extensions.Logging;
using Polly;
using Polly.RateLimiting;
using Polly.Retry;
using System.Runtime.CompilerServices;
using System.Threading.RateLimiting;

namespace MelloSilveiraTools.Core.ResiliencePipelines;

/// <summary>
/// Base resilience pipeline to automatically retry and log each attempt.
/// </summary>
public class DefaultResiliencePipeline
{
    private static readonly ResiliencePropertyKey<string> CallerFileNamePropertyKey = new("CallerFileName");
    private static readonly ResiliencePropertyKey<string> CallerMemberNamePropertyKey = new("CallerMemberName");

    private readonly ResiliencePipeline _pipeline;

    /// <summary>
    /// Initializes a new instance of <see cref="DefaultResiliencePipeline"/>.
    /// </summary>
    /// <param name="logger">See reference at <see cref="ILogger"/>.</param>
    /// <param name="settings">See reference at <see cref="ResiliencePipelineSettings"/>.</param>
    /// <param name="shouldHandle">Predicate that determines whether the retry should be executed for a given outcome.</param>
    public DefaultResiliencePipeline(ILogger<DefaultResiliencePipeline> logger, ResiliencePipelineSettings settings, Func<RetryPredicateArguments<object>, ValueTask<bool>> shouldHandle)
    {
        _pipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                ShouldHandle = shouldHandle,
                BackoffType = settings.BackoffType,
                Delay = TimeSpan.FromMilliseconds(settings.DelayInMilliseconds),
                MaxRetryAttempts = settings.MaxRetryAttempts,
                UseJitter = settings.UseJitter,
                OnRetry = args =>
                {
                    ResilienceProperties contextProperties = args.Context.Properties;
                    string? className = contextProperties.GetValue(CallerFileNamePropertyKey!, null);
                    string? methodName = contextProperties.GetValue(CallerMemberNamePropertyKey!, null);

                    // The attempt number from resilience pipeline arguments is zero-based.
                    int attempt = args.AttemptNumber + 1;

                    logger.LogWarning(
                        args.Outcome.Exception,
                        "Attempt {Attempt} on {MethodName} of {ClassName}. Duration: {Duration}, RetryDelay: {RetryDelay}, Result: {@Result}",
                        attempt,
                        methodName,
                        className,
                        args.Duration,
                        args.RetryDelay,
                        args.Outcome.Result);

                    return default;
                }
            })
            // TODO: Estudar como implementar circuit breaker e rate limit nas próximas versões.
            //.AddCircuitBreaker(new Polly.CircuitBreaker.CircuitBreakerStrategyOptions { })
            //.AddConcurrencyLimiter(new ConcurrencyLimiterOptions { })
            //.AddRateLimiter(new RateLimiterStrategyOptions { })
            .Build();
    }

    /// <summary>
    /// Encapsulates a function with a strategy that covers retriable scenarios.
    /// </summary>
    /// <typeparam name="T">Type returned by callback function.</typeparam>
    /// <param name="callback">Function which will be encapsulated by the retry strategy.</param>
    /// <param name="callerMemberName">Member name of the caller which will be logged when a retry is performed.</param>
    /// <param name="callerFilePath">Path of the caller which will be used to get the class name to be logged when a retry is performed.</param>
    /// <returns>The instance of <see cref="ValueTask"/> that represents the asynchronous execution.</returns>
    public async ValueTask<T> ExecuteAsync<T>(Func<ResilienceContext, ValueTask<T>> callback, [CallerMemberName] string callerMemberName = "", [CallerFilePath] string callerFilePath = "")
    {
        ResilienceContext context = ResilienceContextPool.Shared.Get();

        try
        {
            // It is necessary to store the caller member name and caller file name to correctly log on retry.
            context.Properties.Set(CallerMemberNamePropertyKey, callerMemberName);
            context.Properties.Set(CallerFileNamePropertyKey, Path.GetFileNameWithoutExtension(callerFilePath));

            // ATENTION: use async/await to correctly perform finally block.
            return await _pipeline.ExecuteAsync(callback, context).ConfigureAwait(false);
        }
        finally
        {
            ResilienceContextPool.Shared.Return(context);
        }
    }

    /// <summary>
    /// Encapsulates a function with a strategy that covers retriable scenarios.
    /// </summary>
    /// <typeparam name="T">Type returned by callback function.</typeparam>
    /// <param name="callback">Function which will be encapsulated by the retry strategy.</param>
    /// <param name="fallback">Function invoked if the pipeline execution fails. It receives the caught exception and must provide a safe alternative value of type <typeparamref name="T"/> to be returned to the caller.</param>
    /// <param name="callerMemberName">Member name of the caller which will be logged when a retry is performed.</param>
    /// <param name="callerFilePath">Path of the caller which will be used to get the class name to be logged when a retry is performed.</param>
    /// <returns>The instance of <see cref="ValueTask"/> that represents the asynchronous execution.</returns>
    public async ValueTask<T> ExecuteAsync<T>(Func<ResilienceContext, ValueTask<T>> callback, Func<Exception, T> fallback, [CallerMemberName] string callerMemberName = "", [CallerFilePath] string callerFilePath = "")
    {
        ResilienceContext context = ResilienceContextPool.Shared.Get();

        try
        {
            // It is necessary to store the caller member name and caller file name to correctly log on retry.
            context.Properties.Set(CallerMemberNamePropertyKey, callerMemberName);
            context.Properties.Set(CallerFileNamePropertyKey, Path.GetFileNameWithoutExtension(callerFilePath));

            // ATENTION: use async/await to correctly perform finally block.
            return await _pipeline.ExecuteAsync(callback, context).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return fallback(ex);
        }
        finally
        {
            ResilienceContextPool.Shared.Return(context);
        }
    }

    /// <summary>
    /// Encapsulates a function with a strategy that covers retriable scenarios.
    /// </summary>
    /// <typeparam name="T">Type returned by callback function.</typeparam>
    /// <param name="callback">Function which will be encapsulated by the retry strategy.</param>
    /// <param name="fallback">Function invoked if the pipeline execution fails. It receives the caught exception and must provide a safe alternative value of type <typeparamref name="T"/> to be returned to the caller.</param>
    /// <param name="callerMemberName">Member name of the caller which will be logged when a retry is performed.</param>
    /// <param name="callerFilePath">Path of the caller which will be used to get the class name to be logged when a retry is performed.</param>
    /// <returns>The instance of <see cref="ValueTask"/> that represents the asynchronous execution.</returns>
    public async ValueTask<T> ExecuteAsync<T>(Func<ResilienceContext, ValueTask<T>> callback, Func<Exception, ValueTask<T>> fallback, [CallerMemberName] string callerMemberName = "", [CallerFilePath] string callerFilePath = "")
    {
        ResilienceContext context = ResilienceContextPool.Shared.Get();

        try
        {
            // It is necessary to store the caller member name and caller file name to correctly log on retry.
            context.Properties.Set(CallerMemberNamePropertyKey, callerMemberName);
            context.Properties.Set(CallerFileNamePropertyKey, Path.GetFileNameWithoutExtension(callerFilePath));

            // ATENTION: use async/await to correctly perform finally block.
            return await _pipeline.ExecuteAsync(callback, context).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return await fallback(ex).ConfigureAwait(false);
        }
        finally
        {
            ResilienceContextPool.Shared.Return(context);
        }
    }

    /// <summary>
    /// Encapsulates a function with a strategy that covers retriable scenarios.
    /// </summary>
    /// <param name="callback">Function which will be encapsulated by the retry strategy.</param>
    /// <param name="callerMemberName">Member name of the caller which will be logged when a retry is performed.</param>
    /// <param name="callerFilePath">Path of the caller which will be used to get the class name to be logged when a retry is performed.</param>
    /// <returns>The instance of <see cref="ValueTask"/> that represents the asynchronous execution.</returns>
    public async ValueTask ExecuteAsync(Func<ResilienceContext, ValueTask> callback, [CallerMemberName] string callerMemberName = "", [CallerFilePath] string callerFilePath = "")
    {
        ResilienceContext context = ResilienceContextPool.Shared.Get();

        try
        {
            // It is necessary to store the caller member name and caller file name to correctly log on retry.
            context.Properties.Set(CallerMemberNamePropertyKey, callerMemberName);
            context.Properties.Set(CallerFileNamePropertyKey, Path.GetFileNameWithoutExtension(callerFilePath));

            // ATENTION: use async/await to correctly perform finally block.
            await _pipeline.ExecuteAsync(callback, context).ConfigureAwait(false);
        }
        finally
        {
            ResilienceContextPool.Shared.Return(context);
        }
    }

    /// <summary>
    /// Encapsulates a function with a strategy that covers retriable scenarios.
    /// </summary>
    /// <param name="callback">Function which will be encapsulated by the retry strategy.</param>
    /// <param name="fallback">Function invoked if the pipeline execution fails. It receives the caught exception and must provide a safe alternative value of type <typeparamref name="T"/> to be returned to the caller.</param>
    /// <param name="callerMemberName">Member name of the caller which will be logged when a retry is performed.</param>
    /// <param name="callerFilePath">Path of the caller which will be used to get the class name to be logged when a retry is performed.</param>
    /// <returns>The instance of <see cref="ValueTask"/> that represents the asynchronous execution.</returns>
    public async ValueTask ExecuteAsync(Func<ResilienceContext, ValueTask> callback, Action<Exception> fallback, [CallerMemberName] string callerMemberName = "", [CallerFilePath] string callerFilePath = "")
    {
        ResilienceContext context = ResilienceContextPool.Shared.Get();

        try
        {
            // It is necessary to store the caller member name and caller file name to correctly log on retry.
            context.Properties.Set(CallerMemberNamePropertyKey, callerMemberName);
            context.Properties.Set(CallerFileNamePropertyKey, Path.GetFileNameWithoutExtension(callerFilePath));

            // ATENTION: use async/await to correctly perform finally block.
            await _pipeline.ExecuteAsync(callback, context).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            fallback(ex);
        }
        finally
        {
            ResilienceContextPool.Shared.Return(context);
        }
    }

    /// <summary>
    /// Encapsulates a function with a strategy that covers retriable scenarios.
    /// </summary>
    /// <param name="callback">Function which will be encapsulated by the retry strategy.</param>
    /// <param name="fallback">Function invoked if the pipeline execution fails. It receives the caught exception and must provide a safe alternative value of type <typeparamref name="T"/> to be returned to the caller.</param>
    /// <param name="callerMemberName">Member name of the caller which will be logged when a retry is performed.</param>
    /// <param name="callerFilePath">Path of the caller which will be used to get the class name to be logged when a retry is performed.</param>
    /// <returns>The instance of <see cref="ValueTask"/> that represents the asynchronous execution.</returns>
    public async ValueTask ExecuteAsync(Func<ResilienceContext, ValueTask> callback, Func<Exception, ValueTask> fallback, [CallerMemberName] string callerMemberName = "", [CallerFilePath] string callerFilePath = "")
    {
        ResilienceContext context = ResilienceContextPool.Shared.Get();

        try
        {
            // It is necessary to store the caller member name and caller file name to correctly log on retry.
            context.Properties.Set(CallerMemberNamePropertyKey, callerMemberName);
            context.Properties.Set(CallerFileNamePropertyKey, Path.GetFileNameWithoutExtension(callerFilePath));

            // ATENTION: use async/await to correctly perform finally block.
            await _pipeline.ExecuteAsync(callback, context).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            await fallback(ex).ConfigureAwait(false);
        }
        finally
        {
            ResilienceContextPool.Shared.Return(context);
        }
    }
}
