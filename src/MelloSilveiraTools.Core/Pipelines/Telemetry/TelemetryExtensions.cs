using MelloSilveiraTools.Core.Pipelines.Models;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace MelloSilveiraTools.Core.Pipelines.Telemetry;

/// <summary>
/// Provides structured logging wrappers, ensuring consistent tracking of execution lifecycles, durations, and failures.
/// </summary>
/// <remarks>
/// Technical Decision: Enforces the <see cref="StackTraceHiddenAttribute"/> across all methods to prevent these telemetry 
/// and retry wrapper frames from cluttering application exception stack traces, keeping debugging focused on the actual business logic.
/// </remarks>
[StackTraceHidden]
public static class TelemetryExtensions
{
    /// <summary>
    /// Wraps a synchronous action with OpenTelemetry tracing and structured logging, returning a new execution delegate.
    /// </summary>
    /// <remarks>
    /// Technical Decision: Exceptions are allowed to bubble up naturally. This is intended for use in pipeline topologies without a configured Dead-Letter Queue (DLQ).
    /// Limitation: Does not support retries. Attempting to retry a synchronous action would require thread-blocking operations, risking ThreadPool starvation.
    /// </remarks>
    public static Action<TIn> HandleExecution<TIn>(ILogger logger, string callbackName, Action<TIn> callback, CancellationToken cancellationToken = default) => (input) =>
    {
        cancellationToken.ThrowIfCancellationRequested();

        using Activity? activity = TelemetryConstants.DefaultInstance.StartActivity(callbackName, ActivityKind.Internal);
        DateTimeOffset startTime = StartTelemetry(logger, activity, callbackName);

        try
        {
            callback(input);
            LogAndTrackStepCompletion(logger, activity, startTime, callbackName);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogAndTrackStepFailure(logger, activity, startTime, callbackName, ex);
            throw;
        }
    };

    /// <summary>
    /// Wraps a synchronous action, capturing exceptions into a <see cref="SafeResult{TIn}"/> envelope for centralized error routing.
    /// </summary>
    /// <remarks>
    /// Technical Decision: Dynamically replaces the native exception-throwing behavior to prevent block faults in TPL Dataflow, routing the failure to a DLQ branch instead.
    /// </remarks>
    public static Func<TIn, SafeResult<TIn>> HandleSafeExecution<TIn>(ILogger logger, string callbackName, Action<TIn> callback, CancellationToken cancellationToken = default) => (input) =>
    {
        cancellationToken.ThrowIfCancellationRequested();

        using Activity? activity = TelemetryConstants.DefaultInstance.StartActivity(callbackName, ActivityKind.Internal);
        DateTimeOffset startTime = StartTelemetry(logger, activity, callbackName);

        try
        {
            callback(input);
            LogAndTrackStepCompletion(logger, activity, startTime, callbackName);
            return SafeResult<TIn>.CreateSuccess();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogAndTrackStepFailure(logger, activity, startTime, callbackName, ex);
            return new(callbackName, input, ex);
        }
    };

    /// <summary>
    /// Wraps an asynchronous action with OpenTelemetry tracing, structured logging, and optional exponential backoff.
    /// </summary>
    /// <remarks>
    /// Technical Decision: Leverages asynchronous delays (<see cref="Task.Delay"/>) to perform backoff retries without blocking the ThreadPool.
    /// Limitation: Unrecoverable exceptions (post-retries) will fault the pipeline block since this method does not return a <see cref="SafeResult{TIn}"/>.
    /// </remarks>
    public static Func<TIn, Task> HandleExecution<TIn>(
        ILogger logger,
        string callbackName,
        Func<TIn, CancellationToken, Task> callback,
        RetryOptions? retryOptions = null,
        CancellationToken cancellationToken = default)
        => async (input) =>
        {
            using Activity? activity = TelemetryConstants.DefaultInstance.StartActivity(callbackName, ActivityKind.Internal);
            DateTimeOffset startTime = StartTelemetry(logger, activity, callbackName);

            int attempt = 0;
            int delayMs = retryOptions?.InitialDelayMs ?? 0;

            while (true)
            {
                try
                {
                    await callback(input, cancellationToken).ConfigureAwait(false);
                    LogAndTrackStepCompletion(logger, activity, startTime, callbackName);
                    return; // Explicitly break out of loop on success
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    (bool shouldRetry, attempt, delayMs) = await HandleRetryAsync(logger, activity, callbackName, ex, attempt, delayMs, startTime, retryOptions, cancellationToken).ConfigureAwait(false);
                    if (!shouldRetry)
                        throw;
                }
            }
        };

    /// <summary>
    /// Wraps an asynchronous action, combining exponential backoff with a <see cref="SafeResult{TIn}"/> envelope for terminal failures.
    /// </summary>
    /// <remarks>
    /// Technical Decision: Provides the highest degree of fault tolerance for terminal pipeline steps (sinks), attempting to heal transient issues first, and gracefully failing over to a DLQ if exhaustion occurs.
    /// </remarks>
    public static Func<TIn, Task<SafeResult<TIn>>> HandleSafeExecution<TIn>(
        ILogger logger,
        string callbackName,
        Func<TIn, CancellationToken, Task> callback,
        RetryOptions? retryOptions = null,
        CancellationToken cancellationToken = default) => async (input) =>
        {
            using Activity? activity = TelemetryConstants.DefaultInstance.StartActivity(callbackName, ActivityKind.Internal);
            DateTimeOffset startTime = StartTelemetry(logger, activity, callbackName);

            int attempt = 0;
            int delayMs = retryOptions?.InitialDelayMs ?? 0;

            while (true)
            {
                try
                {
                    await callback(input, cancellationToken).ConfigureAwait(false);
                    LogAndTrackStepCompletion(logger, activity, startTime, callbackName);
                    return SafeResult<TIn>.CreateSuccess();
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    (bool shouldRetry, attempt, delayMs) = await HandleRetryAsync(logger, activity, callbackName, ex, attempt, delayMs, startTime, retryOptions, cancellationToken).ConfigureAwait(false);
                    if (!shouldRetry)
                        return new(callbackName, input, ex);
                }
            }
        };

    /// <summary>
    /// Wraps a synchronous mapping function with OpenTelemetry tracing and structured logging, returning the mapped output.
    /// </summary>
    /// <remarks>
    /// Technical Decision: Elides async overhead entirely. Designed for pure CPU-bound mapping logic (e.g., entity to DTO translation).
    /// </remarks>
    public static Func<TIn, TOut> HandleExecution<TIn, TOut>(ILogger logger, string callbackName, Func<TIn, TOut> callback, CancellationToken cancellationToken = default) => (input) =>
    {
        cancellationToken.ThrowIfCancellationRequested();

        using Activity? activity = TelemetryConstants.DefaultInstance.StartActivity(callbackName, ActivityKind.Internal);
        DateTimeOffset startTime = StartTelemetry(logger, activity, callbackName);

        try
        {
            TOut output = callback(input);
            LogAndTrackStepCompletion(logger, activity, startTime, callbackName);
            return output;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogAndTrackStepFailure(logger, activity, startTime, callbackName, ex);
            throw;
        }
    };

    /// <summary>
    /// Wraps a synchronous mapping function, intercepting failures into a <see cref="SafeResult{TIn, TOut}"/> to permit DLQ offloading.
    /// </summary>
    public static Func<TIn, SafeResult<TIn, TOut>> HandleSafeExecution<TIn, TOut>(ILogger logger, string callbackName, Func<TIn, TOut> callback, CancellationToken cancellationToken = default) => (input) =>
    {
        cancellationToken.ThrowIfCancellationRequested();

        using Activity? activity = TelemetryConstants.DefaultInstance.StartActivity(callbackName, ActivityKind.Internal);
        DateTimeOffset startTime = StartTelemetry(logger, activity, callbackName);

        try
        {
            TOut output = callback(input);
            LogAndTrackStepCompletion(logger, activity, startTime, callbackName);
            return output;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogAndTrackStepFailure(logger, activity, startTime, callbackName, ex);
            return new(callbackName, input, ex);
        }
    };

    /// <summary>
    /// Wraps an asynchronous mapping function with support for optional retry logic (exponential backoff) and telemetry tracking.
    /// </summary>
    public static Func<TIn, Task<TOut>> HandleExecution<TIn, TOut>(ILogger logger,
        string callbackName,
        Func<TIn, CancellationToken, Task<TOut>> callback,
        RetryOptions? retryOptions = null,
        CancellationToken cancellationToken = default)
        => async (input) =>
        {
            using Activity? activity = TelemetryConstants.DefaultInstance.StartActivity(callbackName, ActivityKind.Internal);
            return await ExecuteAsync(logger, activity, input, callbackName, callback, retryOptions, cancellationToken).ConfigureAwait(false);
        };

    /// <summary>
    /// Wraps an asynchronous mapping function, catching ultimate backoff failures into a bifurcated <see cref="SafeResult{TIn, TOut}"/>.
    /// </summary>
    public static Func<TIn, Task<SafeResult<TIn, TOut>>> HandleSafeExecution<TIn, TOut>(
        ILogger logger,
        string callbackName,
        Func<TIn, CancellationToken, Task<TOut>> callback,
        RetryOptions? retryOptions = null,
        CancellationToken cancellationToken = default)
        => async (input) =>
        {
            using Activity? activity = TelemetryConstants.DefaultInstance.StartActivity(callbackName, ActivityKind.Internal);
            return await SafeExecuteAsync(logger, activity, input, callbackName, callback, retryOptions, cancellationToken).ConfigureAwait(false);
        };

    /// <summary>
    /// Wraps a streaming function returning an <see cref="IAsyncEnumerable{TOut}"/> with OpenTelemetry tracing and structured logging.
    /// </summary>
    public static Func<TIn, IAsyncEnumerable<TOut>> HandleExecution<TIn, TOut>(
        ILogger logger,
        string callbackName,
        Func<TIn, CancellationToken, IAsyncEnumerable<TOut>> callback,
        CancellationToken cancellationToken = default)
        => (input) => ExecuteStreamingAsync(logger, input, callbackName, callback, cancellationToken);

    private static async IAsyncEnumerable<TOut> ExecuteStreamingAsync<TIn, TOut>(
        ILogger logger,
        TIn input,
        string callbackName,
        Func<TIn, CancellationToken, IAsyncEnumerable<TOut>> callback,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using Activity? activity = TelemetryConstants.DefaultInstance.StartActivity(callbackName, ActivityKind.Internal);
        DateTimeOffset startTime = StartTelemetry(logger, activity, callbackName);

        IAsyncEnumerator<TOut>? enumerator = null;
        try
        {
            IAsyncEnumerable<TOut> enumerable = callback(input, cancellationToken);
            enumerator = enumerable.GetAsyncEnumerator(cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogAndTrackStepFailure(logger, activity, startTime, callbackName, ex);
            throw;
        }

        try
        {
            while (true)
            {
                bool hasNext;
                try
                {
                    hasNext = await enumerator.MoveNextAsync().ConfigureAwait(false);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    LogAndTrackStepFailure(logger, activity, startTime, callbackName, ex);
                    throw;
                }

                if (!hasNext)
                {
                    break;
                }

                yield return enumerator.Current;
            }

            LogAndTrackStepCompletion(logger, activity, startTime, callbackName);
        }
        finally
        {
            if (enumerator is not null)
            {
                await enumerator.DisposeAsync().ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    /// Wraps a conditional forking operation evaluating the input payload prior to execution.
    /// </summary>
    /// <remarks>
    /// Technical Decision: Pre-execution evaluation prevents unnecessary I/O allocation on the primary branch if the short-circuit condition is met.
    /// Limitation: The condition must be synchronous. If asynchronous validation is required before branching, this pattern must be adapted.
    /// </remarks>
    public static Func<TIn, Task<TOut>> HandleExecution<TIn, TOut>(ILogger logger,
        string callbackName,
        string fallbackName,
        Func<TIn, CancellationToken, Task<TOut>> callback,
        Func<TIn, bool> fallbackCondition,
        Func<TIn, CancellationToken, Task<TOut>> fallback,
        RetryOptions? retryOptions = null,
        CancellationToken cancellationToken = default)
        => async (input) =>
        {
            using Activity? activity = TelemetryConstants.DefaultInstance.StartActivity(callbackName, ActivityKind.Internal);

            if (fallbackCondition(input))
            {
                LogFallbackConditionMet(logger, callbackName, fallbackName);
                return await ExecuteAsync(logger, activity, input, fallbackName, fallback, retryOptions, cancellationToken).ConfigureAwait(false);
            }

            return await ExecuteAsync(logger, activity, input, callbackName, callback, retryOptions, cancellationToken).ConfigureAwait(false);
        };

    /// <summary>
    /// Wraps a conditional forking operation evaluating the input payload, utilizing a <see cref="SafeResult{TIn, TOut}"/> to catch execution failures on either branch.
    /// </summary>
    public static Func<TIn, Task<SafeResult<TIn, TOut>>> HandleSafeExecution<TIn, TOut>(ILogger logger,
        string callbackName,
        string fallbackName,
        Func<TIn, CancellationToken, Task<TOut>> callback,
        Func<TIn, bool> fallbackCondition,
        Func<TIn, CancellationToken, Task<TOut>> fallback,
        RetryOptions? retryOptions = null,
        CancellationToken cancellationToken = default)
        => async (input) =>
        {
            using Activity? activity = TelemetryConstants.DefaultInstance.StartActivity(callbackName, ActivityKind.Internal);

            if (fallbackCondition(input))
            {
                LogFallbackConditionMet(logger, callbackName, fallbackName);
                return await SafeExecuteAsync(logger, activity, input, fallbackName, fallback, retryOptions, cancellationToken).ConfigureAwait(false);
            }

            return await SafeExecuteAsync(logger, activity, input, callbackName, callback, retryOptions, cancellationToken).ConfigureAwait(false);
        };

    /// <summary>
    /// Wraps a conditional forking operation evaluating the output payload after the primary execution completes.
    /// </summary>
    /// <remarks>
    /// Limitation: If the primary execution applies external side effects (like updating a database) before returning its result, triggering this fallback will *not* roll back those side effects automatically.
    /// </remarks>
    public static Func<TIn, Task<TOut>> HandleExecution<TIn, TOut>(ILogger logger,
        string callbackName,
        string fallbackName,
        Func<TIn, CancellationToken, Task<TOut>> callback,
        Func<TOut, bool> fallbackCondition,
        Func<TIn, CancellationToken, Task<TOut>> fallback,
        RetryOptions? retryOptions = null,
        CancellationToken cancellationToken = default)
        => async (input) =>
        {
            using Activity? activity = TelemetryConstants.DefaultInstance.StartActivity(callbackName, ActivityKind.Internal);

            TOut output = await ExecuteAsync(logger, activity, input, fallbackName, fallback, retryOptions, cancellationToken).ConfigureAwait(false);
            if (fallbackCondition(output))
            {
                LogFallbackConditionMet(logger, callbackName, fallbackName);
                return await ExecuteAsync(logger, activity, input, callbackName, callback, retryOptions, cancellationToken).ConfigureAwait(false);
            }

            return output;
        };

    /// <summary>
    /// Wraps a post-execution conditional forking operation, evaluating against the <see cref="SafeResult{TIn, TOut}"/> state.
    /// </summary>
    /// <remarks>
    /// Technical Decision: Passing the entire <c>SafeResult</c> to the <paramref name="fallbackCondition"/> allows the condition to evaluate both successfully mapped outputs and handled faults to determine routing logic.
    /// </remarks>
    public static Func<TIn, Task<SafeResult<TIn, TOut>>> HandleSafeExecution<TIn, TOut>(ILogger logger,
        string callbackName,
        string fallbackName,
        Func<TIn, CancellationToken, Task<TOut>> callback,
        Func<SafeResult<TIn, TOut>, bool> fallbackCondition,
        Func<TIn, CancellationToken, Task<TOut>> fallback,
        RetryOptions? retryOptions = null,
        CancellationToken cancellationToken = default)
        => async (input) =>
        {
            using Activity? activity = TelemetryConstants.DefaultInstance.StartActivity(callbackName, ActivityKind.Internal);

            SafeResult<TIn, TOut> safeResult = await SafeExecuteAsync(logger, activity, input, fallbackName, fallback, retryOptions, cancellationToken).ConfigureAwait(false);
            if (fallbackCondition(safeResult))
            {
                LogFallbackConditionMet(logger, callbackName, fallbackName);
                return await SafeExecuteAsync(logger, activity, input, callbackName, callback, retryOptions, cancellationToken).ConfigureAwait(false);
            }

            return safeResult;
        };

    /// <summary>
    /// Encapsulates the core asynchronous execution loop with exponential backoff logic.
    /// </summary>
    private static async Task<TOut> ExecuteAsync<TIn, TOut>(
        ILogger logger,
        Activity? activity,
        TIn input,
        string callbackName,
        Func<TIn, CancellationToken, Task<TOut>> callback,
        RetryOptions? retryOptions = null,
        CancellationToken cancellationToken = default)
    {
        DateTimeOffset startTime = StartTelemetry(logger, activity, callbackName);

        int attempt = 0;
        int delayMs = retryOptions?.InitialDelayMs ?? 0;

        while (true)
        {
            try
            {
                TOut output = await callback(input, cancellationToken).ConfigureAwait(false);
                LogAndTrackStepCompletion(logger, activity, startTime, callbackName);
                return output;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                (bool shouldRetry, attempt, delayMs) = await HandleRetryAsync(logger, activity, callbackName, ex, attempt, delayMs, startTime, retryOptions, cancellationToken).ConfigureAwait(false);
                if (!shouldRetry)
                    throw;
            }
        }
    }

    /// <summary>
    /// Encapsulates the core asynchronous execution loop, returning a <see cref="SafeResult{TIn, TOut}"/> upon failure exhaustion.
    /// </summary>
    private static async Task<SafeResult<TIn, TOut>> SafeExecuteAsync<TIn, TOut>(
        ILogger logger,
        Activity? activity,
        TIn input,
        string callbackName,
        Func<TIn, CancellationToken, Task<TOut>> callback,
        RetryOptions? retryOptions = null,
        CancellationToken cancellationToken = default)
    {
        DateTimeOffset startTime = StartTelemetry(logger, activity, callbackName);

        int attempt = 0;
        int delayMs = retryOptions?.InitialDelayMs ?? 0;

        while (true)
        {
            try
            {
                TOut output = await callback(input, cancellationToken).ConfigureAwait(false);
                LogAndTrackStepCompletion(logger, activity, startTime, callbackName);
                return output;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                attempt++;
                if (retryOptions == null || attempt >= retryOptions.Value.MaxAttempts)
                {
                    LogAndTrackStepFailure(logger, activity, startTime, callbackName, ex);
                    return new(callbackName, input, ex);
                }

                logger.LogWarning(ex, "Attempt {Attempt} failed for '{Name}'. Retrying in {Delay}ms...", attempt, callbackName, delayMs);
                await Task.Delay(delayMs, cancellationToken);
                delayMs = (int)(delayMs * retryOptions.Value.BackoffFactor);
            }
        }
    }

    /// <summary>
    /// Initializes telemetry tags and logs the start of a pipeline execution step.
    /// </summary>
    private static DateTimeOffset StartTelemetry(ILogger logger, Activity? activity, string name)
    {
        activity?.SetTag("execution.name", name);

        var startTime = DateTimeOffset.UtcNow;
        logger.LogInformation("{StartTime:O} - Starting '{Name}'.", startTime, name);
        return startTime;
    }

    /// <summary>
    /// Finalizes a successful execution step, updating the span status to Ok and logging the total duration.
    /// </summary>
    private static void LogAndTrackStepCompletion(ILogger logger, Activity? activity, DateTimeOffset startTime, string name)
    {
        activity?.SetStatus(ActivityStatusCode.Ok);

        var endTime = DateTimeOffset.UtcNow;
        TimeSpan duration = endTime - startTime;
        logger.LogInformation("{EndTime:O} - Duration: {Duration} - Successfully completed '{Name}'.", endTime, duration, name);
    }

    /// <summary>
    /// Finalizes a failed execution step, attaching the exception to the span, updating status to Error, and logging the fault.
    /// </summary>
    private static void LogAndTrackStepFailure(ILogger logger, Activity? activity, DateTimeOffset startTime, string name, Exception ex)
    {
        activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
        // Attach the exception and stack trace to the telemetry span using native API.
        activity?.AddException(ex);

        var endTime = DateTimeOffset.UtcNow;
        TimeSpan duration = endTime - startTime;
        logger.LogError(ex, "{EndTime:O} - Duration: {Duration} - Failed '{Name}'.", endTime, duration, name);
    }

    /// <summary>
    /// Standardized log event indicating that short-circuit or fallback conditions have been met.
    /// </summary>
    private static void LogFallbackConditionMet(ILogger logger, string callbackName, string fallbackName)
    {
        logger.LogInformation("Fallback condition met for input prior to '{CallbackName}'. Rerouting to '{FallbackName}'.", callbackName, fallbackName);
    }

    /// <summary>
    /// Evaluates backoff conditions, applies Task delays for valid retries, or returns a terminal state if limits are reached.
    /// </summary>
    /// <remarks>
    /// Technical Decision: Extracted into a distinct helper to keep the execution loops clean and testable, unifying the retry calculation logic across both Safe and Unsafe async patterns.
    /// </remarks>
    private static async Task<(bool ShouldRetry, int Attempt, int DelayMs)> HandleRetryAsync(
        ILogger logger,
        Activity? activity,
        string callbackName,
        Exception exception,
        int attempt,
        int delayMs,
        DateTimeOffset startTime,
        RetryOptions? retryOptions,
        CancellationToken cancellationToken)
    {
        int currentAttempt = attempt + 1;
        if (retryOptions == null || currentAttempt >= retryOptions.Value.MaxAttempts)
        {
            LogAndTrackStepFailure(logger, activity, startTime, callbackName, exception);
            return (false, currentAttempt, delayMs);
        }

        logger.LogWarning(exception, "Attempt {Attempt} failed for '{Name}'. Retrying in {Delay}ms...", currentAttempt, callbackName, delayMs);
        await Task.Delay(delayMs, cancellationToken);

        int currentDelayMs = (int)(delayMs * retryOptions.Value.BackoffFactor);
        return (true, currentAttempt, currentDelayMs);
    }
}