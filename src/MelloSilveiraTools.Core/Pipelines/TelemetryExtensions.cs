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
        using Activity? activity = Telemetry.DefaultInstance.StartActivity(callbackName, ActivityKind.Internal);
        Execute(logger, activity, input, callbackName, callback, cancellationToken);
    };

    /// <summary>
    /// Wraps an asynchronous action with OpenTelemetry tracing and structured logging, returning a new execution delegate.
    /// </summary>
    public static Func<TIn, Task> HandleExecution<TIn>(ILogger logger, string callbackName, Func<TIn, CancellationToken, Task> callback, CancellationToken cancellationToken = default) => async (input) =>
    {
        using Activity? activity = Telemetry.DefaultInstance.StartActivity(callbackName, ActivityKind.Internal);
        await ExecuteAsync(logger, activity, input, callbackName, callback, cancellationToken).ConfigureAwait(false);
    };

    /// <summary>
    /// Wraps a synchronous mapping function with OpenTelemetry tracing and structured logging, returning a new execution delegate.
    /// </summary>
    public static Func<TIn, TOut> HandleExecution<TIn, TOut>(ILogger logger, string callbackName, Func<TIn, TOut> callback, CancellationToken cancellationToken = default) => (input) =>
    {
        using Activity? activity = Telemetry.DefaultInstance.StartActivity(callbackName, ActivityKind.Internal);
        return Execute(logger, activity, input, callbackName, callback, cancellationToken);
    };

    /// <summary>
    /// Wraps an asynchronous mapping function, incorporating tracing, logging, and an explicit error handler delegate.
    /// </summary>
    public static Func<TIn, Task<TOut?>> HandleExecution<TIn, TOut>(
        ILogger logger,
        string callbackName,
        Func<TIn, TOut> callback,
        Func<(TIn Input, Exception Exception), CancellationToken, Task> errorHandler,
        CancellationToken cancellationToken = default)
        => async (input) =>
        {
            using Activity? activity = Telemetry.DefaultInstance.StartActivity(callbackName, ActivityKind.Internal);
            return await ExecuteAsync(logger, activity, input, callbackName, callback, errorHandler, cancellationToken).ConfigureAwait(false);
        };

    /// <summary>
    /// Wraps an asynchronous mapping function with support for optional retry logic (exponential backoff) and error handling.
    /// </summary>
    public static Func<TIn, Task<TOut?>> HandleExecution<TIn, TOut>(ILogger logger,
        string callbackName,
        Func<TIn, CancellationToken, Task<TOut>> callback,
        Func<(TIn Input, Exception Exception), CancellationToken, Task>? errorHandler = null,
        RetryOptions? retryOptions = null,
        CancellationToken cancellationToken = default)
        => async (input) =>
        {
            using Activity? activity = Telemetry.DefaultInstance.StartActivity(callbackName, ActivityKind.Internal);
            return await ExecuteAsync(logger, activity, input, callbackName, callback, errorHandler, retryOptions, cancellationToken).ConfigureAwait(false);
        };

    /// <summary>
    /// Wraps a conditional forking operation evaluating the input payload prior to execution.
    /// </summary>
    public static Func<TIn, Task<TOut?>> HandleExecution<TIn, TOut>(ILogger logger,
        string callbackName,
        string fallbackName,
        Func<TIn, CancellationToken, Task<TOut>> callback,
        Func<TIn, bool> fallbackCondition,
        Func<TIn, CancellationToken, Task<TOut>> fallback,
        Func<(TIn Input, Exception Exception), CancellationToken, Task>? errorHandler = null,
        RetryOptions? retryOptions = null,
        CancellationToken cancellationToken = default)
        => async (input) =>
        {
            using Activity? activity = Telemetry.DefaultInstance.StartActivity(callbackName, ActivityKind.Internal);
            return await ExecuteForkingAsync(logger, activity, input, callbackName, fallbackName, callback, fallbackCondition, fallback, errorHandler, retryOptions, cancellationToken).ConfigureAwait(false);
        };

    /// <summary>
    /// Wraps a conditional forking operation evaluating the output payload after the primary execution completes.
    /// </summary>
    public static Func<TIn, Task<TOut?>> HandleExecution<TIn, TOut>(ILogger logger,
        string callbackName,
        string fallbackName,
        Func<TIn, CancellationToken, Task<TOut>> callback,
        Func<TOut, bool> fallbackCondition,
        Func<TIn, CancellationToken, Task<TOut>> fallback,
        Func<(TIn Input, Exception Exception), CancellationToken, Task>? errorHandler = null,
        RetryOptions? retryOptions = null,
        CancellationToken cancellationToken = default)
        => async (input) =>
        {
            using Activity? activity = Telemetry.DefaultInstance.StartActivity(callbackName, ActivityKind.Internal);
            return await ExecuteForkingAsync(logger, activity, input, callbackName, fallbackName, callback, fallbackCondition, fallback, errorHandler, retryOptions, cancellationToken).ConfigureAwait(false);
        };

    /// <summary>
    /// Executes a synchronous action, recording its duration, lifecycle events, and tracking any unhandled exceptions.
    /// </summary>
    public static void Execute<TIn>(ILogger logger, Activity? activity, TIn input, string callbackName, Action<TIn> callback, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

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
    }

    /// <summary>
    /// Executes an asynchronous action, recording its duration, lifecycle events, and tracking any unhandled exceptions.
    /// </summary>
    public static async Task ExecuteAsync<TIn>(ILogger logger, Activity? activity, TIn input, string callbackName, Func<TIn, CancellationToken, Task> callback, CancellationToken cancellationToken = default)
    {
        DateTimeOffset startTime = StartTelemetry(logger, activity, callbackName);

        try
        {
            await callback(input, cancellationToken).ConfigureAwait(false);
            LogAndTrackStepCompletion(logger, activity, startTime, callbackName);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogAndTrackStepFailure(logger, activity, startTime, callbackName, ex);
            throw;
        }
    }

    /// <summary>
    /// Executes a synchronous mapping function, recording its duration and returning the mapped result.
    /// </summary>
    public static TOut Execute<TIn, TOut>(ILogger logger, Activity? activity, TIn input, string callbackName, Func<TIn, TOut> callback, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        DateTimeOffset startTime = StartTelemetry(logger, activity, callbackName);

        try
        {
            TOut? result = callback(input);
            LogAndTrackStepCompletion(logger, activity, startTime, callbackName);
            return result;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogAndTrackStepFailure(logger, activity, startTime, callbackName, ex);
            throw;
        }
    }

    /// <summary>
    /// Executes a synchronous mapping function asynchronously to route any exceptions to the provided error handler.
    /// </summary>
    public static async Task<TOut?> ExecuteAsync<TIn, TOut>(
        ILogger logger,
        Activity? activity,
        TIn input,
        string callbackName,
        Func<TIn, TOut> callback,
        Func<(TIn Input, Exception Exception), CancellationToken, Task> errorHandler,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        DateTimeOffset startTime = StartTelemetry(logger, activity, callbackName);

        try
        {
            TOut? result = callback(input);
            LogAndTrackStepCompletion(logger, activity, startTime, callbackName);
            return result;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogAndTrackStepFailure(logger, activity, startTime, callbackName, ex);
            await ExecuteAsync(logger, activity, (input, ex), $"{callbackName}.ErrorHandler", errorHandler, cancellationToken);
            return default;
        }
    }

    /// <summary>
    /// Executes an async func with OpenTelemetry tracing, standard logging, and optional exponential backoff retry logic.
    /// </summary>
    public static async Task<TOut?> ExecuteAsync<TIn, TOut>(
        ILogger logger,
        Activity? activity,
        TIn input,
        string callbackName,
        Func<TIn, CancellationToken, Task<TOut>> callback,
        Func<(TIn Input, Exception Exception), CancellationToken, Task>? errorHandler = null,
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
                TOut? result = await callback(input, cancellationToken).ConfigureAwait(false);
                LogAndTrackStepCompletion(logger, activity, startTime, callbackName);
                return result;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                attempt++;
                if (retryOptions == null || attempt >= retryOptions.Value.MaxAttempts)
                {
                    LogAndTrackStepFailure(logger, activity, startTime, callbackName, ex);

                    if (errorHandler is null)
                        throw;

                    await errorHandler((input, ex), cancellationToken).ConfigureAwait(false);
                    return default;
                }

                logger.LogWarning(ex, "Attempt {Attempt} failed for '{Name}'. Retrying in {Delay}ms...", attempt, callbackName, delayMs);
                await Task.Delay(delayMs, cancellationToken);
                delayMs = (int)(delayMs * retryOptions.Value.BackoffFactor);
            }
        }
    }

    /// <summary>
    /// Evaluates a condition against the input prior to execution. If met, diverts to the fallback delegate instead of the primary function.
    /// </summary>
    public static async Task<TOut?> ExecuteForkingAsync<TIn, TOut>(
        ILogger logger,
        Activity? activity,
        TIn input,
        string callbackName,
        string fallbackName,
        Func<TIn, CancellationToken, Task<TOut>> callback,
        Func<TIn, bool> fallbackCondition,
        Func<TIn, CancellationToken, Task<TOut>> fallback,
        Func<(TIn Input, Exception Exception), CancellationToken, Task>? errorHandler = null,
        RetryOptions? retryOptions = null,
        CancellationToken cancellationToken = default)
    {
        if (fallbackCondition(input))
        {
            logger.LogInformation("Fallback condition met for input prior to '{CallbackName}'. Rerouting to '{FallbackName}'.", callbackName, fallbackName);
            return await ExecuteAsync(logger, activity, input, fallbackName, fallback, errorHandler, retryOptions, cancellationToken).ConfigureAwait(false);
        }

        return await ExecuteAsync(logger, activity, input, callbackName, callback, errorHandler, retryOptions, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Executes the primary function, then evaluates a condition against its output. If met, discards the output and executes the fallback delegate.
    /// </summary>
    public static async Task<TOut?> ExecuteForkingAsync<TIn, TOut>(
        ILogger logger,
        Activity? activity,
        TIn input,
        string callbackName,
        string fallbackName,
        Func<TIn, CancellationToken, Task<TOut>> callback,
        Func<TOut, bool> fallbackCondition,
        Func<TIn, CancellationToken, Task<TOut>> fallback,
        Func<(TIn Input, Exception Exception), CancellationToken, Task>? errorHandler = null,
        RetryOptions? retryOptions = null,
        CancellationToken cancellationToken = default)
    {
        DateTimeOffset startTime = StartTelemetry(logger, activity, callbackName);

        try
        {
            TOut? result = await callback(input, cancellationToken).ConfigureAwait(false);
            if (fallbackCondition(result))
            {
                LogAndTrackStepFailure(logger, activity, startTime, callbackName, new Exception($"Fallback condition met for output of '{callbackName}'. Rerouting to '{fallbackName}'."));
                return await ExecuteAsync(logger, activity, input, fallbackName, fallback, errorHandler, retryOptions, cancellationToken).ConfigureAwait(false);
            }

            LogAndTrackStepCompletion(logger, activity, startTime, callbackName);
            return result;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogAndTrackStepFailure(logger, activity, startTime, callbackName, ex);
            if (errorHandler is null)
                throw;

            await ExecuteAsync(logger, activity, (input, ex), $"{callbackName}.ErrorHandler", errorHandler, cancellationToken);
            return default;
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
}