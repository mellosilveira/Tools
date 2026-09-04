using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace MelloSilveiraTools.Core.Pipelines;

/// <summary>
/// Provides structured logging wrappers, ensuring consistent tracking of execution lifecycles, durations, and failures.
/// </summary>
[StackTraceHidden]
public static class TelemetryExtensions
{
    /// <summary>
    /// Wraps a synchronous action with OpenTelemetry tracing and structured logging, returning a new execution delegate.
    /// </summary>
    public static Action<TIn> HandleExecution<TIn>(ILogger logger, string callbackName, Action<TIn> callback, CancellationToken cancellationToken = default) => (input) =>
    {
        cancellationToken.ThrowIfCancellationRequested();

        using Activity? activity = Telemetry.DefaultInstance.StartActivity(callbackName, ActivityKind.Internal);
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

    public static Func<TIn, SafeResult<TIn>> HandleSafeExecution<TIn>(ILogger logger, string callbackName, Action<TIn> callback, CancellationToken cancellationToken = default) => (input) =>
    {
        cancellationToken.ThrowIfCancellationRequested();

        using Activity? activity = Telemetry.DefaultInstance.StartActivity(callbackName, ActivityKind.Internal);
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
    /// Wraps an asynchronous action with OpenTelemetry tracing and structured logging, returning a new execution delegate.
    /// </summary>
    public static Func<TIn, Task> HandleExecution<TIn>(
        ILogger logger,
        string callbackName,
        Func<TIn, CancellationToken, Task> callback,
        RetryOptions? retryOptions = null,
        CancellationToken cancellationToken = default)
        => async (input) =>
        {
            using Activity? activity = Telemetry.DefaultInstance.StartActivity(callbackName, ActivityKind.Internal);
            DateTimeOffset startTime = StartTelemetry(logger, activity, callbackName);

            int attempt = 0;
            int delayMs = retryOptions?.InitialDelayMs ?? 0;

            while (true)
            {
                try
                {
                    await callback(input, cancellationToken).ConfigureAwait(false);
                    LogAndTrackStepCompletion(logger, activity, startTime, callbackName);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    (bool shouldRetry, attempt, delayMs) = await HandleRetryAsync(logger, activity, callbackName, ex, attempt, delayMs, startTime, retryOptions, cancellationToken).ConfigureAwait(false);
                    if (!shouldRetry)
                        throw;
                }
            }
        };

    public static Func<TIn, Task<SafeResult<TIn>>> HandleSafeExecution<TIn>(
        ILogger logger,
        string callbackName,
        Func<TIn, CancellationToken, Task> callback,
        RetryOptions? retryOptions = null,
        CancellationToken cancellationToken = default) => async (input) =>
        {
            using Activity? activity = Telemetry.DefaultInstance.StartActivity(callbackName, ActivityKind.Internal);
            DateTimeOffset startTime = StartTelemetry(logger, activity, callbackName);

            int attempt = 0;
            int delayMs = retryOptions?.InitialDelayMs ?? 0;

            while (true)
            {
                try
                {
                    await callback(input, cancellationToken).ConfigureAwait(false);
                    LogAndTrackStepCompletion(logger, activity, startTime, callbackName);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    (bool shouldRetry, int currentAttempt, int currentDelayMs) = await HandleRetryAsync(logger, activity, callbackName, ex, attempt, delayMs, startTime, retryOptions, cancellationToken).ConfigureAwait(false);
                    if (!shouldRetry)
                        return new(callbackName, input, ex);
                }
            }
        };

    /// <summary>
    /// Wraps a synchronous mapping function with OpenTelemetry tracing and structured logging, returning a new execution delegate.
    /// </summary>
    public static Func<TIn, TOut> HandleExecution<TIn, TOut>(ILogger logger, string callbackName, Func<TIn, TOut> callback, CancellationToken cancellationToken = default) => (input) =>
    {
        cancellationToken.ThrowIfCancellationRequested();

        using Activity? activity = Telemetry.DefaultInstance.StartActivity(callbackName, ActivityKind.Internal);
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

    public static Func<TIn, SafeResult<TIn, TOut>> HandleSafeExecution<TIn, TOut>(ILogger logger, string callbackName, Func<TIn, TOut> callback, CancellationToken cancellationToken = default) => (input) =>
    {
        cancellationToken.ThrowIfCancellationRequested();

        using Activity? activity = Telemetry.DefaultInstance.StartActivity(callbackName, ActivityKind.Internal);
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
    /// Wraps an asynchronous mapping function with support for optional retry logic (exponential backoff) and error handling.
    /// </summary>
    public static Func<TIn, Task<TOut>> HandleExecution<TIn, TOut>(ILogger logger,
        string callbackName,
        Func<TIn, CancellationToken, Task<TOut>> callback,
        RetryOptions? retryOptions = null,
        CancellationToken cancellationToken = default)
        => async (input) =>
        {
            using Activity? activity = Telemetry.DefaultInstance.StartActivity(callbackName, ActivityKind.Internal);
            return await ExecuteAsync(logger, activity, input, callbackName, callback, retryOptions, cancellationToken).ConfigureAwait(false);
        };

    public static Func<TIn, Task<SafeResult<TIn, TOut>>> HandleSafeExecution<TIn, TOut>(
        ILogger logger,
        string callbackName,
        Func<TIn, CancellationToken, Task<TOut>> callback,
        RetryOptions? retryOptions = null,
        CancellationToken cancellationToken = default)
        => async (input) =>
        {
            using Activity? activity = Telemetry.DefaultInstance.StartActivity(callbackName, ActivityKind.Internal);
            return await SafeExecuteAsync(logger, activity, input, callbackName, callback, retryOptions, cancellationToken).ConfigureAwait(false);
        };

    /// <summary>
    /// Wraps a conditional forking operation evaluating the input payload prior to execution.
    /// </summary>
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
            using Activity? activity = Telemetry.DefaultInstance.StartActivity(callbackName, ActivityKind.Internal);

            if (fallbackCondition(input))
            {
                LogFallbackConditionMet(logger, callbackName, fallbackName);
                return await ExecuteAsync(logger, activity, input, fallbackName, fallback, retryOptions, cancellationToken).ConfigureAwait(false);
            }

            return await ExecuteAsync(logger, activity, input, callbackName, callback, retryOptions, cancellationToken).ConfigureAwait(false);
        };

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
            using Activity? activity = Telemetry.DefaultInstance.StartActivity(callbackName, ActivityKind.Internal);

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
            using Activity? activity = Telemetry.DefaultInstance.StartActivity(callbackName, ActivityKind.Internal);

            TOut output = await ExecuteAsync(logger, activity, input, fallbackName, fallback, retryOptions, cancellationToken).ConfigureAwait(false);
            if (fallbackCondition(output))
            {
                LogFallbackConditionMet(logger, callbackName, fallbackName);
                return await ExecuteAsync(logger, activity, input, callbackName, callback, retryOptions, cancellationToken).ConfigureAwait(false);
            }

            return output;
        };

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
            using Activity? activity = Telemetry.DefaultInstance.StartActivity(callbackName, ActivityKind.Internal);

            SafeResult<TIn, TOut> safeResult = await SafeExecuteAsync(logger, activity, input, fallbackName, fallback, retryOptions, cancellationToken).ConfigureAwait(false);
            if (fallbackCondition(safeResult))
            {
                LogFallbackConditionMet(logger, callbackName, fallbackName);
                return await SafeExecuteAsync(logger, activity, input, callbackName, callback, retryOptions, cancellationToken).ConfigureAwait(false);
            }

            return safeResult;
        };

    /// <summary>
    /// Executes an async func with OpenTelemetry tracing, standard logging, and optional exponential backoff retry logic.
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

    private static void LogFallbackConditionMet(ILogger logger, string callbackName, string fallbackName)
    {
        logger.LogInformation("Fallback condition met for input prior to '{CallbackName}'. Rerouting to '{FallbackName}'.", callbackName, fallbackName);
    }

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