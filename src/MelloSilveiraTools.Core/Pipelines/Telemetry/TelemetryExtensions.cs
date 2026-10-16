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
    public static Action<TIn> HandleExecution<TIn>(
        ILogger logger,
        string callbackName,
        Action<TIn> callback,
        CancellationToken cancellationToken = default,
        IProgress<PipelineProgressEvent>? progress = null) => (input) =>
    {
        cancellationToken.ThrowIfCancellationRequested();

        using Activity? activity = TelemetryConstants.DefaultInstance.StartActivity(callbackName, ActivityKind.Internal);
        DateTimeOffset startTime = StartTelemetry(logger, activity, callbackName, progress);

        try
        {
            callback(input);
            LogAndTrackStepCompletion(logger, activity, startTime, callbackName, progress);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogAndTrackStepFailure(logger, activity, startTime, callbackName, ex, progress);
            throw;
        }
    };

    /// <summary>
    /// Wraps a synchronous action, capturing exceptions into a <see cref="SafeResult{TIn}"/> envelope for centralized error routing.
    /// </summary>
    /// <remarks>
    /// Technical Decision: Dynamically replaces the native exception-throwing behavior to prevent block faults in TPL Dataflow, routing the failure to a DLQ branch instead.
    /// </remarks>
    public static Func<TIn, SafeResult<TIn>> HandleSafeExecution<TIn>(
        ILogger logger,
        string callbackName,
        Action<TIn> callback,
        CancellationToken cancellationToken = default,
        IProgress<PipelineProgressEvent>? progress = null) => (input) =>
    {
        cancellationToken.ThrowIfCancellationRequested();

        using Activity? activity = TelemetryConstants.DefaultInstance.StartActivity(callbackName, ActivityKind.Internal);
        DateTimeOffset startTime = StartTelemetry(logger, activity, callbackName, progress);

        try
        {
            callback(input);
            LogAndTrackStepCompletion(logger, activity, startTime, callbackName, progress);
            return SafeResult<TIn>.CreateSuccess();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogAndTrackStepFailure(logger, activity, startTime, callbackName, ex, progress);
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
        CancellationToken cancellationToken = default,
        IProgress<PipelineProgressEvent>? progress = null)
        => async (input) =>
        {
            using Activity? activity = TelemetryConstants.DefaultInstance.StartActivity(callbackName, ActivityKind.Internal);
            DateTimeOffset startTime = StartTelemetry(logger, activity, callbackName, progress);

            int attempt = 0;
            int delayMs = retryOptions?.InitialDelayMs ?? 0;

            while (true)
            {
                try
                {
                    await callback(input, cancellationToken).ConfigureAwait(false);
                    LogAndTrackStepCompletion(logger, activity, startTime, callbackName, progress);
                    return; // Explicitly break out of loop on success
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    (bool shouldRetry, attempt, delayMs) = await HandleRetryAsync(logger, activity, callbackName, ex, attempt, delayMs, startTime, retryOptions, cancellationToken, progress).ConfigureAwait(false);
                    if (!shouldRetry)
                    {
                        throw;
                    }
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
        CancellationToken cancellationToken = default,
        IProgress<PipelineProgressEvent>? progress = null) => async (input) =>
        {
            using Activity? activity = TelemetryConstants.DefaultInstance.StartActivity(callbackName, ActivityKind.Internal);
            DateTimeOffset startTime = StartTelemetry(logger, activity, callbackName, progress);

            int attempt = 0;
            int delayMs = retryOptions?.InitialDelayMs ?? 0;

            while (true)
            {
                try
                {
                    await callback(input, cancellationToken).ConfigureAwait(false);
                    LogAndTrackStepCompletion(logger, activity, startTime, callbackName, progress);
                    return SafeResult<TIn>.CreateSuccess();
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    (bool shouldRetry, attempt, delayMs) = await HandleRetryAsync(logger, activity, callbackName, ex, attempt, delayMs, startTime, retryOptions, cancellationToken, progress).ConfigureAwait(false);
                    if (!shouldRetry)
                    {
                        return new(callbackName, input, ex);
                    }
                }
            }
        };

    /// <summary>
    /// Wraps a synchronous mapping function with OpenTelemetry tracing and structured logging, returning the mapped output.
    /// </summary>
    /// <remarks>
    /// Technical Decision: Elides async overhead entirely. Designed for pure CPU-bound mapping logic (e.g., entity to DTO translation).
    /// </remarks>
    public static Func<TIn, TOut> HandleExecution<TIn, TOut>(
        ILogger logger,
        string callbackName,
        Func<TIn, TOut> callback,
        CancellationToken cancellationToken = default,
        IProgress<PipelineProgressEvent>? progress = null) => (input) =>
    {
        using Activity? activity = TelemetryConstants.DefaultInstance.StartActivity(callbackName, ActivityKind.Internal);
        return Execute(logger, activity, input, callbackName, callback, cancellationToken, progress);
    };

    /// <summary>
    /// Wraps a synchronous mapping function, intercepting failures into a <see cref="SafeResult{TIn, TOut}"/> to permit DLQ offloading.
    /// </summary>
    public static Func<TIn, SafeResult<TIn, TOut>> HandleSafeExecution<TIn, TOut>(
        ILogger logger,
        string callbackName,
        Func<TIn, TOut> callback,
        CancellationToken cancellationToken = default,
        IProgress<PipelineProgressEvent>? progress = null) => (input) =>
    {
        using Activity? activity = TelemetryConstants.DefaultInstance.StartActivity(callbackName, ActivityKind.Internal);
        return SafeExecute(logger, activity, input, callbackName, callback, cancellationToken, progress);
    };

    /// <summary>
    /// Wraps an asynchronous mapping function with support for optional retry logic (exponential backoff) and telemetry tracking.
    /// </summary>
    public static Func<TIn, Task<TOut>> HandleExecution<TIn, TOut>(
        ILogger logger,
        string callbackName,
        Func<TIn, CancellationToken, Task<TOut>> callback,
        RetryOptions? retryOptions = null,
        CancellationToken cancellationToken = default,
        IProgress<PipelineProgressEvent>? progress = null)
        => async (input) =>
        {
            using Activity? activity = TelemetryConstants.DefaultInstance.StartActivity(callbackName, ActivityKind.Internal);
            return await ExecuteAsync(logger, activity, input, callbackName, callback, retryOptions, cancellationToken, progress).ConfigureAwait(false);
        };

    /// <summary>
    /// Wraps an asynchronous mapping function, catching ultimate backoff failures into a bifurcated <see cref="SafeResult{TIn, TOut}"/>.
    /// </summary>
    public static Func<TIn, Task<SafeResult<TIn, TOut>>> HandleSafeExecution<TIn, TOut>(
        ILogger logger,
        string callbackName,
        Func<TIn, CancellationToken, Task<TOut>> callback,
        RetryOptions? retryOptions = null,
        CancellationToken cancellationToken = default,
        IProgress<PipelineProgressEvent>? progress = null)
        => async (input) =>
        {
            using Activity? activity = TelemetryConstants.DefaultInstance.StartActivity(callbackName, ActivityKind.Internal);
            return await SafeExecuteAsync(logger, activity, input, callbackName, callback, retryOptions, cancellationToken, progress).ConfigureAwait(false);
        };

    /// <summary>
    /// Wraps a conditional forking operation evaluating the input payload prior to execution.
    /// </summary>
    /// <remarks>
    /// Technical Decision: Pre-execution evaluation prevents unnecessary I/O allocation on the primary branch if the short-circuit condition is met.
    /// Limitation: The condition must be synchronous. If asynchronous validation is required before branching, this pattern must be adapted.
    /// </remarks>
    public static Func<TIn, Task<TOut>> HandleExecution<TIn, TOut>(
        ILogger logger,
        string callbackName,
        string fallbackName,
        Func<TIn, CancellationToken, Task<TOut>> callback,
        Func<TIn, bool> fallbackCondition,
        Func<TIn, CancellationToken, Task<TOut>> fallback,
        RetryOptions? retryOptions = null,
        CancellationToken cancellationToken = default,
        IProgress<PipelineProgressEvent>? progress = null)
        => async (input) =>
        {
            using Activity? activity = TelemetryConstants.DefaultInstance.StartActivity(callbackName, ActivityKind.Internal);

            if (fallbackCondition(input))
            {
                LogFallbackConditionMet(logger, callbackName, fallbackName);
                return await ExecuteAsync(logger, activity, input, fallbackName, fallback, retryOptions, cancellationToken, progress).ConfigureAwait(false);
            }

            return await ExecuteAsync(logger, activity, input, callbackName, callback, retryOptions, cancellationToken, progress).ConfigureAwait(false);
        };

    /// <summary>
    /// Wraps a conditional forking operation evaluating the input payload, utilizing a <see cref="SafeResult{TIn, TOut}"/> to catch execution failures on either branch.
    /// </summary>
    public static Func<TIn, Task<SafeResult<TIn, TOut>>> HandleSafeExecution<TIn, TOut>(
        ILogger logger,
        string callbackName,
        string fallbackName,
        Func<TIn, CancellationToken, Task<TOut>> callback,
        Func<TIn, bool> fallbackCondition,
        Func<TIn, CancellationToken, Task<TOut>> fallback,
        RetryOptions? retryOptions = null,
        CancellationToken cancellationToken = default,
        IProgress<PipelineProgressEvent>? progress = null)
        => async (input) =>
        {
            using Activity? activity = TelemetryConstants.DefaultInstance.StartActivity(callbackName, ActivityKind.Internal);

            if (fallbackCondition(input))
            {
                LogFallbackConditionMet(logger, callbackName, fallbackName);
                return await SafeExecuteAsync(logger, activity, input, fallbackName, fallback, retryOptions, cancellationToken, progress).ConfigureAwait(false);
            }

            return await SafeExecuteAsync(logger, activity, input, callbackName, callback, retryOptions, cancellationToken, progress).ConfigureAwait(false);
        };

    /// <summary>
    /// Wraps a conditional forking operation evaluating the output payload after the primary execution completes.
    /// </summary>
    /// <remarks>
    /// Limitation: If the primary execution applies external side effects (like updating a database) before returning its result, triggering this fallback will *not* roll back those side effects automatically.
    /// </remarks>
    public static Func<TIn, Task<TOut>> HandleExecution<TIn, TOut>(
        ILogger logger,
        string callbackName,
        string fallbackName,
        Func<TIn, CancellationToken, Task<TOut>> callback,
        Func<TOut, bool> fallbackCondition,
        Func<TIn, CancellationToken, Task<TOut>> fallback,
        RetryOptions? retryOptions = null,
        CancellationToken cancellationToken = default,
        IProgress<PipelineProgressEvent>? progress = null)
        => async (input) =>
        {
            using Activity? activity = TelemetryConstants.DefaultInstance.StartActivity(callbackName, ActivityKind.Internal);

            TOut output = await ExecuteAsync(logger, activity, input, fallbackName, fallback, retryOptions, cancellationToken, progress).ConfigureAwait(false);
            if (fallbackCondition(output))
            {
                LogFallbackConditionMet(logger, callbackName, fallbackName);
                return await ExecuteAsync(logger, activity, input, callbackName, callback, retryOptions, cancellationToken, progress).ConfigureAwait(false);
            }

            return output;
        };

    /// <summary>
    /// Wraps a post-execution conditional forking operation, evaluating against the <see cref="SafeResult{TIn, TOut}"/> state.
    /// </summary>
    /// <remarks>
    /// Technical Decision: Passing the entire <c>SafeResult</c> to the <paramref name="fallbackCondition"/> allows the condition to evaluate both successfully mapped outputs and handled faults to determine routing logic.
    /// </remarks>
    public static Func<TIn, SafeResult<TIn, TOut>> HandleSafeExecution<TIn, TOut>(
        ILogger logger,
        string callbackName,
        string fallbackName,
        Func<TIn, TOut> callback,
        Func<SafeResult<TIn, TOut>, bool> fallbackCondition,
        Func<TIn, TOut> fallback,
        CancellationToken cancellationToken = default,
        IProgress<PipelineProgressEvent>? progress = null)
        => (input) =>
        {
            using Activity? activity = TelemetryConstants.DefaultInstance.StartActivity(callbackName, ActivityKind.Internal);

            SafeResult<TIn, TOut> safeResult = SafeExecute(logger, activity, input, fallbackName, fallback, cancellationToken, progress);
            if (fallbackCondition(safeResult))
            {
                LogFallbackConditionMet(logger, callbackName, fallbackName);
                return SafeExecute(logger, activity, input, callbackName, callback, cancellationToken, progress);
            }

            return safeResult;
        };

    /// <summary>
    /// Wraps a synchronous conditional forking operation evaluating the output payload after the primary execution completes.
    /// </summary>
    public static Func<TIn, TOut> HandleExecution<TIn, TOut>(
        ILogger logger,
        string callbackName,
        string fallbackName,
        Func<TIn, TOut> callback,
        Func<TOut, bool> fallbackCondition,
        Func<TIn, TOut> fallback,
        CancellationToken cancellationToken = default,
        IProgress<PipelineProgressEvent>? progress = null)
        => (input) =>
        {
            using Activity? activity = TelemetryConstants.DefaultInstance.StartActivity(callbackName, ActivityKind.Internal);

            TOut output = Execute(logger, activity, input, fallbackName, fallback, cancellationToken, progress);
            if (fallbackCondition(output))
            {
                LogFallbackConditionMet(logger, callbackName, fallbackName);
                return Execute(logger, activity, input, callbackName, callback, cancellationToken, progress);
            }

            return output;
        };

    /// <summary>
    /// Wraps a post-execution conditional forking operation, evaluating against the <see cref="SafeResult{TIn, TOut}"/> state.
    /// </summary>
    /// <remarks>
    /// Technical Decision: Passing the entire <c>SafeResult</c> to the <paramref name="fallbackCondition"/> allows the condition to evaluate both successfully mapped outputs and handled faults to determine routing logic.
    /// </remarks>
    public static Func<TIn, Task<SafeResult<TIn, TOut>>> HandleSafeExecution<TIn, TOut>(
        ILogger logger,
        string callbackName,
        string fallbackName,
        Func<TIn, CancellationToken, Task<TOut>> callback,
        Func<SafeResult<TIn, TOut>, bool> fallbackCondition,
        Func<TIn, CancellationToken, Task<TOut>> fallback,
        RetryOptions? retryOptions = null,
        CancellationToken cancellationToken = default,
        IProgress<PipelineProgressEvent>? progress = null)
        => async (input) =>
        {
            using Activity? activity = TelemetryConstants.DefaultInstance.StartActivity(callbackName, ActivityKind.Internal);

            SafeResult<TIn, TOut> safeResult = await SafeExecuteAsync(logger, activity, input, fallbackName, fallback, retryOptions, cancellationToken, progress).ConfigureAwait(false);
            if (fallbackCondition(safeResult))
            {
                LogFallbackConditionMet(logger, callbackName, fallbackName);
                return await SafeExecuteAsync(logger, activity, input, callbackName, callback, retryOptions, cancellationToken, progress).ConfigureAwait(false);
            }

            return safeResult;
        };

    /// <summary>
    /// Wraps a streaming function returning an <see cref="IAsyncEnumerable{TOut}"/> with OpenTelemetry tracing and structured logging.
    /// </summary>
    public static Func<TIn, IAsyncEnumerable<TOut>> HandleExecution<TIn, TOut>(
        ILogger logger,
        string callbackName,
        Func<TIn, CancellationToken, IAsyncEnumerable<TOut>> callback,
        CancellationToken cancellationToken = default,
        IProgress<PipelineProgressEvent>? progress = null)
        => (input) =>
        {
            using Activity? activity = TelemetryConstants.DefaultInstance.StartActivity(callbackName, ActivityKind.Internal);
            return ExecuteStreamingAsync(logger, activity, input, callbackName, callback, cancellationToken, progress);
        };

    /// <summary>
    /// Wraps a streaming function returning an <see cref="IAsyncEnumerable{TOut}"/> with OpenTelemetry tracing and structured logging.
    /// </summary>
    public static Func<TIn, IAsyncEnumerable<SafeResult<TIn, TOut>>> HandleSafeExecution<TIn, TOut>(
        ILogger logger,
        string callbackName,
        Func<TIn, CancellationToken, IAsyncEnumerable<TOut>> callback,
        CancellationToken cancellationToken = default,
        IProgress<PipelineProgressEvent>? progress = null)
        => (input) =>
        {
            using Activity? activity = TelemetryConstants.DefaultInstance.StartActivity(callbackName, ActivityKind.Internal);
            return ExecuteSafeStreamingAsync(logger, activity, input, callbackName, callback, cancellationToken, progress);
        };

    /// <summary>
    /// Wraps an enumerable transformation function returning an <see cref="IEnumerable{TOut}"/> with OpenTelemetry tracing and structured logging.
    /// </summary>
    public static Func<TIn, IEnumerable<TOut>> HandleExecution<TIn, TOut>(
        ILogger logger,
        string callbackName,
        Func<TIn, CancellationToken, IEnumerable<TOut>> callback,
        CancellationToken cancellationToken = default,
        IProgress<PipelineProgressEvent>? progress = null)
        => (input) =>
        {
            using Activity? activity = TelemetryConstants.DefaultInstance.StartActivity(callbackName, ActivityKind.Internal);
            return ExecuteStreaming(logger, activity, input, callbackName, callback, cancellationToken, progress);
        };

    /// <summary>
    /// Wraps an enumerable transformation function returning an <see cref="IEnumerable{TOut}"/> with OpenTelemetry tracing, structured logging, and safe DLQ routing.
    /// </summary>
    public static Func<TIn, IEnumerable<SafeResult<TIn, TOut>>> HandleSafeExecution<TIn, TOut>(
        ILogger logger,
        string callbackName,
        Func<TIn, CancellationToken, IEnumerable<TOut>> callback,
        CancellationToken cancellationToken = default,
        IProgress<PipelineProgressEvent>? progress = null)
        => (input) =>
        {
            using Activity? activity = TelemetryConstants.DefaultInstance.StartActivity(callbackName, ActivityKind.Internal);
            return ExecuteSafeStreaming(logger, activity, input, callbackName, callback, cancellationToken, progress);
        };

    private static SafeResult<TIn, TOut> SafeExecute<TIn, TOut>(
        ILogger logger,
        Activity? activity,
        TIn input,
        string callbackName,
        Func<TIn, TOut> callback,
        CancellationToken cancellationToken = default,
        IProgress<PipelineProgressEvent>? progress = null)
    {
        cancellationToken.ThrowIfCancellationRequested();

        DateTimeOffset startTime = StartTelemetry(logger, activity, callbackName, progress);

        try
        {
            TOut output = callback(input);
            LogAndTrackStepCompletion(logger, activity, startTime, callbackName, progress);
            return output;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogAndTrackStepFailure(logger, activity, startTime, callbackName, ex, progress);
            return new(callbackName, input, ex);
        }
    }

    private static TOut Execute<TIn, TOut>(
        ILogger logger,
        Activity? activity,
        TIn input,
        string callbackName,
        Func<TIn, TOut> callback,
        CancellationToken cancellationToken = default,
        IProgress<PipelineProgressEvent>? progress = null)
    {
        cancellationToken.ThrowIfCancellationRequested();

        DateTimeOffset startTime = StartTelemetry(logger, activity, callbackName, progress);

        try
        {
            TOut output = callback(input);
            LogAndTrackStepCompletion(logger, activity, startTime, callbackName, progress);
            return output;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogAndTrackStepFailure(logger, activity, startTime, callbackName, ex, progress);
            throw;
        }
    }

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
        CancellationToken cancellationToken = default,
        IProgress<PipelineProgressEvent>? progress = null)
    {
        DateTimeOffset startTime = StartTelemetry(logger, activity, callbackName, progress);

        int attempt = 0;
        int delayMs = retryOptions?.InitialDelayMs ?? 0;

        while (true)
        {
            try
            {
                TOut output = await callback(input, cancellationToken).ConfigureAwait(false);
                LogAndTrackStepCompletion(logger, activity, startTime, callbackName, progress);
                return output;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                (bool shouldRetry, attempt, delayMs) = await HandleRetryAsync(logger, activity, callbackName, ex, attempt, delayMs, startTime, retryOptions, cancellationToken, progress).ConfigureAwait(false);
                if (!shouldRetry)
                {
                    throw;
                }
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
        CancellationToken cancellationToken = default,
        IProgress<PipelineProgressEvent>? progress = null)
    {
        DateTimeOffset startTime = StartTelemetry(logger, activity, callbackName, progress);

        int attempt = 0;
        int delayMs = retryOptions?.InitialDelayMs ?? 0;

        while (true)
        {
            try
            {
                TOut output = await callback(input, cancellationToken).ConfigureAwait(false);
                LogAndTrackStepCompletion(logger, activity, startTime, callbackName, progress);
                return output;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                attempt++;
                if (retryOptions == null || attempt >= retryOptions.Value.MaxAttempts)
                {
                    LogAndTrackStepFailure(logger, activity, startTime, callbackName, ex, progress);
                    return new(callbackName, input, ex);
                }

                logger.LogWarning(ex, "Attempt {Attempt} failed for '{Name}'. Retrying in {Delay}ms...", attempt, callbackName, delayMs);
                await Task.Delay(delayMs, cancellationToken).ConfigureAwait(false);
                delayMs = (int)(delayMs * retryOptions.Value.BackoffFactor);
            }
        }
    }

    private static async IAsyncEnumerable<TOut> ExecuteStreamingAsync<TIn, TOut>(
        ILogger logger,
        Activity? activity,
        TIn input,
        string callbackName,
        Func<TIn, CancellationToken, IAsyncEnumerable<TOut>> callback,
        [EnumeratorCancellation] CancellationToken cancellationToken = default,
        IProgress<PipelineProgressEvent>? progress = null)
    {
        cancellationToken.ThrowIfCancellationRequested();

        DateTimeOffset startTime = StartTelemetry(logger, activity, callbackName, progress);
        int itemCount = 0;

        IAsyncEnumerator<TOut> enumerator = callback(input, cancellationToken).GetAsyncEnumerator(cancellationToken);
        await using (enumerator.ConfigureAwait(false))
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
                    LogAndTrackStepFailure(logger, activity, startTime, callbackName, ex, progress);
                    throw;
                }

                if (!hasNext)
                {
                    break;
                }

                itemCount++;
                if (itemCount == 1 || itemCount % 500 == 0)
                {
                    progress?.Report(new PipelineProgressEvent(
                        callbackName,
                        $"Streaming '{callbackName}'.",
                        PipelineStepState.Running,
                        Detail: $"Items emitted: {itemCount}",
                        Timestamp: DateTimeOffset.UtcNow));
                }

                yield return enumerator.Current;
            }
        }

        LogAndTrackStepCompletion(logger, activity, startTime, callbackName, progress, $"Items emitted: {itemCount}");
    }

    private static async IAsyncEnumerable<SafeResult<TIn, TOut>> ExecuteSafeStreamingAsync<TIn, TOut>(
        ILogger logger,
        Activity? activity,
        TIn input,
        string callbackName,
        Func<TIn, CancellationToken, IAsyncEnumerable<TOut>> callback,
        [EnumeratorCancellation] CancellationToken cancellationToken = default,
        IProgress<PipelineProgressEvent>? progress = null)
    {
        cancellationToken.ThrowIfCancellationRequested();

        DateTimeOffset startTime = StartTelemetry(logger, activity, callbackName, progress);
        int itemCount = 0;

        IAsyncEnumerator<TOut> enumerator = callback(input, cancellationToken).GetAsyncEnumerator(cancellationToken);
        await using (enumerator.ConfigureAwait(false))
        {
            while (true)
            {
                bool hasNext = false;
                Exception? exception = null;
                try
                {
                    hasNext = await enumerator.MoveNextAsync().ConfigureAwait(false);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    LogAndTrackStepFailure(logger, activity, startTime, callbackName, ex, progress);
                    exception = ex;
                }

                if (exception is not null)
                {
                    yield return (callbackName, input, exception);
                    yield break;
                }

                if (!hasNext)
                {
                    break;
                }

                itemCount++;
                if (itemCount == 1 || itemCount % 500 == 0)
                {
                    progress?.Report(new PipelineProgressEvent(
                        callbackName,
                        $"Streaming '{callbackName}'.",
                        PipelineStepState.Running,
                        Detail: $"Items emitted: {itemCount}",
                        Timestamp: DateTimeOffset.UtcNow));
                }

                yield return enumerator.Current;
            }
        }

        LogAndTrackStepCompletion(logger, activity, startTime, callbackName, progress, $"Items emitted: {itemCount}");
    }

    private static IEnumerable<TOut> ExecuteStreaming<TIn, TOut>(
        ILogger logger,
        Activity? activity,
        TIn input,
        string callbackName,
        Func<TIn, CancellationToken, IEnumerable<TOut>> callback,
        CancellationToken cancellationToken = default,
        IProgress<PipelineProgressEvent>? progress = null)
    {
        cancellationToken.ThrowIfCancellationRequested();

        DateTimeOffset startTime = StartTelemetry(logger, activity, callbackName, progress);
        int itemCount = 0;

        IEnumerator<TOut> enumerator;
        try
        {
            enumerator = callback(input, cancellationToken).GetEnumerator();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogAndTrackStepFailure(logger, activity, startTime, callbackName, ex, progress);
            throw;
        }

        using (enumerator)
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                bool hasNext;
                try
                {
                    hasNext = enumerator.MoveNext();
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    LogAndTrackStepFailure(logger, activity, startTime, callbackName, ex, progress);
                    throw;
                }

                if (!hasNext)
                {
                    break;
                }

                itemCount++;
                if (itemCount == 1 || itemCount % 500 == 0)
                {
                    progress?.Report(new PipelineProgressEvent(
                        callbackName,
                        $"Streaming '{callbackName}'.",
                        PipelineStepState.Running,
                        Detail: $"Items emitted: {itemCount}",
                        Timestamp: DateTimeOffset.UtcNow));
                }

                yield return enumerator.Current;
            }
        }

        LogAndTrackStepCompletion(logger, activity, startTime, callbackName, progress, $"Items emitted: {itemCount}");
    }

    private static IEnumerable<SafeResult<TIn, TOut>> ExecuteSafeStreaming<TIn, TOut>(
        ILogger logger,
        Activity? activity,
        TIn input,
        string callbackName,
        Func<TIn, CancellationToken, IEnumerable<TOut>> callback,
        CancellationToken cancellationToken = default,
        IProgress<PipelineProgressEvent>? progress = null)
    {
        cancellationToken.ThrowIfCancellationRequested();

        DateTimeOffset startTime = StartTelemetry(logger, activity, callbackName, progress);
        int itemCount = 0;

        IEnumerator<TOut>? enumerator = null;
        Exception? initException = null;
        try
        {
            enumerator = callback(input, cancellationToken).GetEnumerator();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogAndTrackStepFailure(logger, activity, startTime, callbackName, ex, progress);
            initException = ex;
        }

        if (initException is not null)
        {
            yield return (callbackName, input, initException);
            yield break;
        }

        using (enumerator!)
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                bool hasNext = false;
                Exception? exception = null;
                try
                {
                    hasNext = enumerator.MoveNext();
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    LogAndTrackStepFailure(logger, activity, startTime, callbackName, ex, progress);
                    exception = ex;
                }

                if (exception is not null)
                {
                    yield return (callbackName, input, exception);
                    yield break;
                }

                if (!hasNext)
                {
                    break;
                }

                itemCount++;
                if (itemCount == 1 || itemCount % 500 == 0)
                {
                    progress?.Report(new PipelineProgressEvent(
                        callbackName,
                        $"Streaming '{callbackName}'.",
                        PipelineStepState.Running,
                        Detail: $"Items emitted: {itemCount}",
                        Timestamp: DateTimeOffset.UtcNow));
                }

                yield return enumerator.Current;
            }
        }

        LogAndTrackStepCompletion(logger, activity, startTime, callbackName, progress, $"Items emitted: {itemCount}");
    }

    /// <summary>
    /// Initializes telemetry tags and logs the start of a pipeline execution step.
    /// </summary>
    private static DateTimeOffset StartTelemetry(
        ILogger logger,
        Activity? activity,
        string name,
        IProgress<PipelineProgressEvent>? progress = null)
    {
        activity?.SetTag("execution.name", name);

        DateTimeOffset startTime = DateTimeOffset.UtcNow;
        logger.LogInformation("{StartTime:O} - Starting '{Name}'.", startTime, name);
        progress?.Report(new PipelineProgressEvent(
            name,
            $"Starting '{name}'.",
            PipelineStepState.Started,
            Timestamp: startTime));
        return startTime;
    }

    /// <summary>
    /// Finalizes a successful execution step, updating the span status to Ok and logging the total duration.
    /// </summary>
    private static void LogAndTrackStepCompletion(
        ILogger logger,
        Activity? activity,
        DateTimeOffset startTime,
        string name,
        IProgress<PipelineProgressEvent>? progress = null,
        string? detail = null)
    {
        activity?.SetStatus(ActivityStatusCode.Ok);

        DateTimeOffset endTime = DateTimeOffset.UtcNow;
        TimeSpan duration = endTime - startTime;
        logger.LogInformation("{EndTime:O} - Duration: {Duration} - Successfully completed '{Name}'.", endTime, duration, name);
        progress?.Report(new PipelineProgressEvent(
            name,
            $"Successfully completed '{name}'.",
            PipelineStepState.Completed,
            Detail: detail ?? $"Duration: {duration}",
            Timestamp: endTime));
    }

    /// <summary>
    /// Finalizes a failed execution step, attaching the exception to the span, updating status to Error, and logging the fault.
    /// </summary>
    private static void LogAndTrackStepFailure(
        ILogger logger,
        Activity? activity,
        DateTimeOffset startTime,
        string name,
        Exception ex,
        IProgress<PipelineProgressEvent>? progress = null)
    {
        activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
        // Attach the exception and stack trace to the telemetry span using native API.
        activity?.AddException(ex);

        DateTimeOffset endTime = DateTimeOffset.UtcNow;
        TimeSpan duration = endTime - startTime;
        logger.LogError(ex, "{EndTime:O} - Duration: {Duration} - Failed '{Name}'.", endTime, duration, name);
        progress?.Report(new PipelineProgressEvent(
            name,
            $"Failed '{name}'.",
            PipelineStepState.Faulted,
            Detail: ex.Message,
            Timestamp: endTime));
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
        CancellationToken cancellationToken,
        IProgress<PipelineProgressEvent>? progress = null)
    {
        int currentAttempt = attempt + 1;
        if (retryOptions == null || currentAttempt >= retryOptions.Value.MaxAttempts)
        {
            LogAndTrackStepFailure(logger, activity, startTime, callbackName, exception, progress);
            return (false, currentAttempt, delayMs);
        }

        logger.LogWarning(exception, "Attempt {Attempt} failed for '{Name}'. Retrying in {Delay}ms...", currentAttempt, callbackName, delayMs);
        await Task.Delay(delayMs, cancellationToken).ConfigureAwait(false);

        int currentDelayMs = (int)(delayMs * retryOptions.Value.BackoffFactor);
        return (true, currentAttempt, currentDelayMs);
    }
}