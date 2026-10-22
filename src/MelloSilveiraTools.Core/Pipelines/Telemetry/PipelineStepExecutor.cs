using MelloSilveiraTools.Core.Pipelines.Models;
using MelloSilveiraTools.Core.Pipelines.Notifiers;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace MelloSilveiraTools.Core.Pipelines.Telemetry;

/// <summary>
/// Encapsulates pipeline step execution with OpenTelemetry distributed tracing, structured logging,
/// retry orchestration, and real-time <see cref="IPipelineProgressNotifier"/> lifecycle notifications.
/// </summary>
/// <remarks>
/// Architectural Decisions:
/// <list type="bullet">
/// <item><description><b>Stack Trace Cleanliness:</b> Enforces <see cref="StackTraceHiddenAttribute"/> across the class so telemetry and retry wrapper frames do not clutter application exception stack traces.</description></item>
/// <item><description><b>Unified Execution Core:</b> Synchronous and asynchronous actions, transformations, conditional forks, and enumerable streams funnel through unified core routines (<c>ExecuteCoreAsync</c>, <c>SafeExecuteCoreAsync</c>, and <c>ExecuteStreamingAsync</c>) to eliminate duplicated telemetry, retry, and error-handling logic.</description></item>
/// <item><description><b>Dual Tracking Mode:</b> When invoked with a <see cref="PipelineStepTracker"/> (inside a TPL Dataflow DAG), lifecycle events and item counters are governed by the tracker; when invoked standalone by name, single-item <c>Started</c>/<c>Completed</c>/<c>Faulted</c> events are emitted directly.</description></item>
/// </list>
/// </remarks>
[StackTraceHidden]
public static class PipelineStepExecutor
{
    /// <summary>
    /// Wraps a synchronous action with OpenTelemetry tracing, structured logging, and progress notifications.
    /// </summary>
    public static Func<TIn, Task> HandleExecution<TIn>(
        ILogger logger,
        IPipelineProgressNotifier progressNotifier,
        TimeProvider timeProvider,
        string callbackName,
        Action<TIn> callback,
        CancellationToken cancellationToken = default) => input => ExecuteCoreAsync(logger, progressNotifier, timeProvider, input, callbackName, WrapSyncAction(callback), null, null, false, cancellationToken);

    internal static Func<TIn, Task> HandleExecution<TIn>(
        ILogger logger,
        IPipelineProgressNotifier progressNotifier,
        TimeProvider timeProvider,
        PipelineStepTracker stepTracker,
        Action<TIn> callback,
        CancellationToken cancellationToken = default) => input => ExecuteCoreAsync(logger, progressNotifier, timeProvider, input, stepTracker.TelemetryName, WrapSyncAction(callback), null, stepTracker, false, cancellationToken);

    /// <summary>
    /// Wraps a synchronous action, capturing exceptions into a <see cref="SafeResult{TIn}"/> envelope for centralized error routing.
    /// </summary>
    public static Func<TIn, Task<SafeResult<TIn>>> HandleSafeExecution<TIn>(
        ILogger logger,
        IPipelineProgressNotifier progressNotifier,
        TimeProvider timeProvider,
        string callbackName,
        Action<TIn> callback,
        CancellationToken cancellationToken = default) => input => SafeExecuteActionCoreAsync(logger, progressNotifier, timeProvider, input, callbackName, WrapSyncAction(callback), null, null, cancellationToken);

    internal static Func<TIn, Task<SafeResult<TIn>>> HandleSafeExecution<TIn>(
        ILogger logger,
        IPipelineProgressNotifier progressNotifier,
        TimeProvider timeProvider,
        PipelineStepTracker stepTracker,
        Action<TIn> callback,
        CancellationToken cancellationToken = default) => input => SafeExecuteActionCoreAsync(logger, progressNotifier, timeProvider, input, stepTracker.TelemetryName, WrapSyncAction(callback), null, stepTracker, cancellationToken);

    /// <summary>
    /// Wraps an asynchronous action with OpenTelemetry tracing, structured logging, progress notifications, and optional exponential backoff.
    /// </summary>
    public static Func<TIn, Task> HandleExecution<TIn>(
        ILogger logger,
        IPipelineProgressNotifier progressNotifier,
        TimeProvider timeProvider,
        string callbackName,
        Func<TIn, CancellationToken, Task> callback,
        RetryOptions? retryOptions = null,
        CancellationToken cancellationToken = default) => input => ExecuteCoreAsync(logger, progressNotifier, timeProvider, input, callbackName, WrapAsyncAction(callback), retryOptions, null, false, cancellationToken);

    internal static Func<TIn, Task> HandleExecution<TIn>(
        ILogger logger,
        IPipelineProgressNotifier progressNotifier,
        TimeProvider timeProvider,
        PipelineStepTracker stepTracker,
        Func<TIn, CancellationToken, Task> callback,
        RetryOptions? retryOptions = null,
        CancellationToken cancellationToken = default) => input => ExecuteCoreAsync(logger, progressNotifier, timeProvider, input, stepTracker.TelemetryName, WrapAsyncAction(callback), retryOptions, stepTracker, false, cancellationToken);

    /// <summary>
    /// Wraps an asynchronous action, combining exponential backoff with a <see cref="SafeResult{TIn}"/> envelope for terminal failures.
    /// </summary>
    public static Func<TIn, Task<SafeResult<TIn>>> HandleSafeExecution<TIn>(
        ILogger logger,
        IPipelineProgressNotifier progressNotifier,
        TimeProvider timeProvider,
        string callbackName,
        Func<TIn, CancellationToken, Task> callback,
        RetryOptions? retryOptions = null,
        CancellationToken cancellationToken = default) => input => SafeExecuteActionCoreAsync(logger, progressNotifier, timeProvider, input, callbackName, WrapAsyncAction(callback), retryOptions, null, cancellationToken);

    internal static Func<TIn, Task<SafeResult<TIn>>> HandleSafeExecution<TIn>(
        ILogger logger,
        IPipelineProgressNotifier progressNotifier,
        TimeProvider timeProvider,
        PipelineStepTracker stepTracker,
        Func<TIn, CancellationToken, Task> callback,
        RetryOptions? retryOptions = null,
        CancellationToken cancellationToken = default) => input => SafeExecuteActionCoreAsync(logger, progressNotifier, timeProvider, input, stepTracker.TelemetryName, WrapAsyncAction(callback), retryOptions, stepTracker, cancellationToken);

    /// <summary>
    /// Wraps a synchronous mapping function with OpenTelemetry tracing, structured logging, and progress notifications, returning the mapped output.
    /// </summary>
    public static Func<TIn, Task<TOut>> HandleExecution<TIn, TOut>(
        ILogger logger,
        IPipelineProgressNotifier progressNotifier,
        TimeProvider timeProvider,
        string callbackName,
        Func<TIn, TOut> callback,
        CancellationToken cancellationToken = default) => input => ExecuteCoreAsync(logger, progressNotifier, timeProvider, input, callbackName, WrapSyncFunc(callback), null, null, true, cancellationToken);

    internal static Func<TIn, Task<TOut>> HandleExecution<TIn, TOut>(
        ILogger logger,
        IPipelineProgressNotifier progressNotifier,
        TimeProvider timeProvider,
        PipelineStepTracker stepTracker,
        Func<TIn, TOut> callback,
        CancellationToken cancellationToken = default) => input => ExecuteCoreAsync(logger, progressNotifier, timeProvider, input, stepTracker.TelemetryName, WrapSyncFunc(callback), null, stepTracker, true, cancellationToken);

    /// <summary>
    /// Wraps a synchronous mapping function, intercepting failures into a <see cref="SafeResult{TIn, TOut}"/> to permit DLQ offloading.
    /// </summary>
    public static Func<TIn, Task<SafeResult<TIn, TOut>>> HandleSafeExecution<TIn, TOut>(
        ILogger logger,
        IPipelineProgressNotifier progressNotifier,
        TimeProvider timeProvider,
        string callbackName,
        Func<TIn, TOut> callback,
        CancellationToken cancellationToken = default) => input => SafeExecuteCoreAsync(logger, progressNotifier, timeProvider, input, callbackName, WrapSyncFunc(callback), null, null, true, cancellationToken);

    internal static Func<TIn, Task<SafeResult<TIn, TOut>>> HandleSafeExecution<TIn, TOut>(
        ILogger logger,
        IPipelineProgressNotifier progressNotifier,
        TimeProvider timeProvider,
        PipelineStepTracker stepTracker,
        Func<TIn, TOut> callback,
        CancellationToken cancellationToken = default) => input => SafeExecuteCoreAsync(logger, progressNotifier, timeProvider, input, stepTracker.TelemetryName, WrapSyncFunc(callback), null, stepTracker, true, cancellationToken);

    /// <summary>
    /// Wraps an asynchronous mapping function with support for optional retry logic (exponential backoff) and telemetry tracking.
    /// </summary>
    public static Func<TIn, Task<TOut>> HandleExecution<TIn, TOut>(
        ILogger logger,
        IPipelineProgressNotifier progressNotifier,
        TimeProvider timeProvider,
        string callbackName,
        Func<TIn, CancellationToken, Task<TOut>> callback,
        RetryOptions? retryOptions = null,
        CancellationToken cancellationToken = default) => input => ExecuteCoreAsync(logger, progressNotifier, timeProvider, input, callbackName, callback, retryOptions, null, true, cancellationToken);

    internal static Func<TIn, Task<TOut>> HandleExecution<TIn, TOut>(
        ILogger logger,
        IPipelineProgressNotifier progressNotifier,
        TimeProvider timeProvider,
        PipelineStepTracker stepTracker,
        Func<TIn, CancellationToken, Task<TOut>> callback,
        RetryOptions? retryOptions = null,
        CancellationToken cancellationToken = default) => input => ExecuteCoreAsync(logger, progressNotifier, timeProvider, input, stepTracker.TelemetryName, callback, retryOptions, stepTracker, true, cancellationToken);

    /// <summary>
    /// Wraps an asynchronous mapping function, catching ultimate backoff failures into a bifurcated <see cref="SafeResult{TIn, TOut}"/>.
    /// </summary>
    public static Func<TIn, Task<SafeResult<TIn, TOut>>> HandleSafeExecution<TIn, TOut>(
        ILogger logger,
        IPipelineProgressNotifier progressNotifier,
        TimeProvider timeProvider,
        string callbackName,
        Func<TIn, CancellationToken, Task<TOut>> callback,
        RetryOptions? retryOptions = null,
        CancellationToken cancellationToken = default) => input => SafeExecuteCoreAsync(logger, progressNotifier, timeProvider, input, callbackName, callback, retryOptions, null, true, cancellationToken);

    internal static Func<TIn, Task<SafeResult<TIn, TOut>>> HandleSafeExecution<TIn, TOut>(
        ILogger logger,
        IPipelineProgressNotifier progressNotifier,
        TimeProvider timeProvider,
        PipelineStepTracker stepTracker,
        Func<TIn, CancellationToken, Task<TOut>> callback,
        RetryOptions? retryOptions = null,
        CancellationToken cancellationToken = default) => input => SafeExecuteCoreAsync(logger, progressNotifier, timeProvider, input, stepTracker.TelemetryName, callback, retryOptions, stepTracker, true, cancellationToken);

    /// <summary>
    /// Wraps a synchronous conditional forking operation evaluating the output payload after the primary execution completes.
    /// </summary>
    public static Func<TIn, Task<TOut>> HandleExecution<TIn, TOut>(
        ILogger logger,
        IPipelineProgressNotifier progressNotifier,
        TimeProvider timeProvider,
        string callbackName,
        string fallbackName,
        Func<TIn, TOut> callback,
        Func<TOut, bool> fallbackCondition,
        Func<TIn, TOut> fallback,
        CancellationToken cancellationToken = default) => HandleExecution(logger, progressNotifier, timeProvider, callbackName, fallbackName, WrapSyncFunc(callback), fallbackCondition, WrapSyncFunc(fallback), null, cancellationToken);

    internal static Func<TIn, Task<TOut>> HandleExecution<TIn, TOut>(
        ILogger logger,
        IPipelineProgressNotifier progressNotifier,
        TimeProvider timeProvider,
        PipelineStepTracker primaryTracker,
        PipelineStepTracker fallbackTracker,
        Func<TIn, TOut> callback,
        Func<TOut, bool> fallbackCondition,
        Func<TIn, TOut> fallback,
        CancellationToken cancellationToken = default) => HandleExecution(logger, progressNotifier, timeProvider, primaryTracker, fallbackTracker, WrapSyncFunc(callback), fallbackCondition, WrapSyncFunc(fallback), null, cancellationToken);

    /// <summary>
    /// Wraps a synchronous post-execution conditional forking operation, evaluating against the <see cref="SafeResult{TIn, TOut}"/> state.
    /// </summary>
    public static Func<TIn, Task<SafeResult<TIn, TOut>>> HandleSafeExecution<TIn, TOut>(
        ILogger logger,
        IPipelineProgressNotifier progressNotifier,
        TimeProvider timeProvider,
        string callbackName,
        string fallbackName,
        Func<TIn, TOut> callback,
        Func<SafeResult<TIn, TOut>, bool> fallbackCondition,
        Func<TIn, TOut> fallback,
        CancellationToken cancellationToken = default) => HandleSafeExecution(logger, progressNotifier, timeProvider, callbackName, fallbackName, WrapSyncFunc(callback), fallbackCondition, WrapSyncFunc(fallback), null, cancellationToken);

    internal static Func<TIn, Task<SafeResult<TIn, TOut>>> HandleSafeExecution<TIn, TOut>(
        ILogger logger,
        IPipelineProgressNotifier progressNotifier,
        TimeProvider timeProvider,
        PipelineStepTracker primaryTracker,
        PipelineStepTracker fallbackTracker,
        Func<TIn, TOut> callback,
        Func<SafeResult<TIn, TOut>, bool> fallbackCondition,
        Func<TIn, TOut> fallback,
        CancellationToken cancellationToken = default) => HandleSafeExecution(logger, progressNotifier, timeProvider, primaryTracker, fallbackTracker, WrapSyncFunc(callback), fallbackCondition, WrapSyncFunc(fallback), null, cancellationToken);

    /// <summary>
    /// Wraps an asynchronous conditional forking operation evaluating the output payload after the primary execution completes.
    /// </summary>
    public static Func<TIn, Task<TOut>> HandleExecution<TIn, TOut>(
        ILogger logger,
        IPipelineProgressNotifier progressNotifier,
        TimeProvider timeProvider,
        string callbackName,
        string fallbackName,
        Func<TIn, CancellationToken, Task<TOut>> callback,
        Func<TOut, bool> fallbackCondition,
        Func<TIn, CancellationToken, Task<TOut>> fallback,
        RetryOptions? retryOptions = null,
        CancellationToken cancellationToken = default) => async input =>
        {
            TOut output = await ExecuteCoreAsync(logger, progressNotifier, timeProvider, input, callbackName, callback, retryOptions, null, false, cancellationToken).ConfigureAwait(false);
            if (fallbackCondition(output))
            {
                LogFallbackConditionMet(logger, callbackName, fallbackName);
                return await ExecuteCoreAsync(logger, progressNotifier, timeProvider, input, fallbackName, fallback, retryOptions, null, false, cancellationToken).ConfigureAwait(false);
            }

            return output;
        };

    internal static Func<TIn, Task<TOut>> HandleExecution<TIn, TOut>(
        ILogger logger,
        IPipelineProgressNotifier progressNotifier,
        TimeProvider timeProvider,
        PipelineStepTracker primaryTracker,
        PipelineStepTracker fallbackTracker,
        Func<TIn, CancellationToken, Task<TOut>> callback,
        Func<TOut, bool> fallbackCondition,
        Func<TIn, CancellationToken, Task<TOut>> fallback,
        RetryOptions? retryOptions = null,
        CancellationToken cancellationToken = default) => async input =>
        {
            TOut output = await ExecuteCoreAsync(logger, progressNotifier, timeProvider, input, primaryTracker.TelemetryName, callback, retryOptions, primaryTracker, false, cancellationToken).ConfigureAwait(false);
            if (fallbackCondition(output))
            {
                LogFallbackConditionMet(logger, primaryTracker.TelemetryName, fallbackTracker.TelemetryName);
                await fallbackTracker.OnItemReceivedAsync(cancellationToken).ConfigureAwait(false);
                return await ExecuteCoreAsync(logger, progressNotifier, timeProvider, input, fallbackTracker.TelemetryName, fallback, retryOptions, fallbackTracker, true, cancellationToken).ConfigureAwait(false);
            }

            await primaryTracker.OnItemEmittedAsync(cancellationToken).ConfigureAwait(false);
            return output;
        };

    /// <summary>
    /// Wraps an asynchronous post-execution conditional forking operation, evaluating against the <see cref="SafeResult{TIn, TOut}"/> state.
    /// </summary>
    public static Func<TIn, Task<SafeResult<TIn, TOut>>> HandleSafeExecution<TIn, TOut>(
        ILogger logger,
        IPipelineProgressNotifier progressNotifier,
        TimeProvider timeProvider,
        string callbackName,
        string fallbackName,
        Func<TIn, CancellationToken, Task<TOut>> callback,
        Func<SafeResult<TIn, TOut>, bool> fallbackCondition,
        Func<TIn, CancellationToken, Task<TOut>> fallback,
        RetryOptions? retryOptions = null,
        CancellationToken cancellationToken = default) => async input =>
        {
            SafeResult<TIn, TOut> safeResult = await SafeExecuteCoreAsync(logger, progressNotifier, timeProvider, input, callbackName, callback, retryOptions, null, false, cancellationToken).ConfigureAwait(false);
            if (fallbackCondition(safeResult))
            {
                LogFallbackConditionMet(logger, callbackName, fallbackName);
                return await SafeExecuteCoreAsync(logger, progressNotifier, timeProvider, input, fallbackName, fallback, retryOptions, null, false, cancellationToken).ConfigureAwait(false);
            }

            return safeResult;
        };

    internal static Func<TIn, Task<SafeResult<TIn, TOut>>> HandleSafeExecution<TIn, TOut>(
        ILogger logger,
        IPipelineProgressNotifier progressNotifier,
        TimeProvider timeProvider,
        PipelineStepTracker primaryTracker,
        PipelineStepTracker fallbackTracker,
        Func<TIn, CancellationToken, Task<TOut>> callback,
        Func<SafeResult<TIn, TOut>, bool> fallbackCondition,
        Func<TIn, CancellationToken, Task<TOut>> fallback,
        RetryOptions? retryOptions = null,
        CancellationToken cancellationToken = default) => async input =>
        {
            SafeResult<TIn, TOut> safeResult = await SafeExecuteCoreAsync(logger, progressNotifier, timeProvider, input, primaryTracker.TelemetryName, callback, retryOptions, primaryTracker, false, cancellationToken).ConfigureAwait(false);
            if (fallbackCondition(safeResult))
            {
                LogFallbackConditionMet(logger, primaryTracker.TelemetryName, fallbackTracker.TelemetryName);
                await fallbackTracker.OnItemReceivedAsync(cancellationToken).ConfigureAwait(false);
                return await SafeExecuteCoreAsync(logger, progressNotifier, timeProvider, input, fallbackTracker.TelemetryName, fallback, retryOptions, fallbackTracker, true, cancellationToken).ConfigureAwait(false);
            }

            if (safeResult.Success)
                await primaryTracker.OnItemEmittedAsync(cancellationToken).ConfigureAwait(false);

            return safeResult;
        };

    /// <summary>
    /// Wraps a streaming function returning an <see cref="IAsyncEnumerable{TOut}"/> with OpenTelemetry tracing, structured logging, and progress notifications.
    /// </summary>
    public static Func<TIn, IAsyncEnumerable<TOut>> HandleExecution<TIn, TOut>(
        ILogger logger,
        IPipelineProgressNotifier progressNotifier,
        TimeProvider timeProvider,
        string callbackName,
        Func<TIn, CancellationToken, IAsyncEnumerable<TOut>> callback,
        CancellationToken cancellationToken = default) => input => ExecuteStreamingAsync(logger, progressNotifier, timeProvider, input, callbackName, callback, null, cancellationToken);

    internal static Func<TIn, IAsyncEnumerable<TOut>> HandleExecution<TIn, TOut>(
        ILogger logger,
        IPipelineProgressNotifier progressNotifier,
        TimeProvider timeProvider,
        PipelineStepTracker stepTracker,
        Func<TIn, CancellationToken, IAsyncEnumerable<TOut>> callback,
        CancellationToken cancellationToken = default) => input => ExecuteStreamingAsync(logger, progressNotifier, timeProvider, input, stepTracker.TelemetryName, callback, stepTracker, cancellationToken);

    /// <summary>
    /// Wraps a streaming function returning an <see cref="IAsyncEnumerable{TOut}"/> with OpenTelemetry tracing, structured logging, progress notifications, and safe DLQ routing.
    /// </summary>
    public static Func<TIn, IAsyncEnumerable<SafeResult<TIn, TOut>>> HandleSafeExecution<TIn, TOut>(
        ILogger logger,
        IPipelineProgressNotifier progressNotifier,
        TimeProvider timeProvider,
        string callbackName,
        Func<TIn, CancellationToken, IAsyncEnumerable<TOut>> callback,
        CancellationToken cancellationToken = default) => input => ExecuteSafeStreamingAsync(logger, progressNotifier, timeProvider, input, callbackName, callback, null, cancellationToken);

    internal static Func<TIn, IAsyncEnumerable<SafeResult<TIn, TOut>>> HandleSafeExecution<TIn, TOut>(
        ILogger logger,
        IPipelineProgressNotifier progressNotifier,
        TimeProvider timeProvider,
        PipelineStepTracker stepTracker,
        Func<TIn, CancellationToken, IAsyncEnumerable<TOut>> callback,
        CancellationToken cancellationToken = default) => input => ExecuteSafeStreamingAsync(logger, progressNotifier, timeProvider, input, stepTracker.TelemetryName, callback, stepTracker, cancellationToken);

    /// <summary>
    /// Wraps an enumerable transformation function returning an <see cref="IEnumerable{TOut}"/> with OpenTelemetry tracing, structured logging, and progress notifications.
    /// </summary>
    public static Func<TIn, IAsyncEnumerable<TOut>> HandleExecution<TIn, TOut>(
        ILogger logger,
        IPipelineProgressNotifier progressNotifier,
        TimeProvider timeProvider,
        string callbackName,
        Func<TIn, CancellationToken, IEnumerable<TOut>> callback,
        CancellationToken cancellationToken = default) => input => ExecuteStreamingAsync(logger, progressNotifier, timeProvider, input, callbackName, WrapSyncEnumerable(callback), null, cancellationToken);

    internal static Func<TIn, IAsyncEnumerable<TOut>> HandleExecution<TIn, TOut>(
        ILogger logger,
        IPipelineProgressNotifier progressNotifier,
        TimeProvider timeProvider,
        PipelineStepTracker stepTracker,
        Func<TIn, CancellationToken, IEnumerable<TOut>> callback,
        CancellationToken cancellationToken = default) => input => ExecuteStreamingAsync(logger, progressNotifier, timeProvider, input, stepTracker.TelemetryName, WrapSyncEnumerable(callback), stepTracker, cancellationToken);

    /// <summary>
    /// Wraps an enumerable transformation function returning an <see cref="IEnumerable{TOut}"/> with OpenTelemetry tracing, structured logging, progress notifications, and safe DLQ routing.
    /// </summary>
    public static Func<TIn, IAsyncEnumerable<SafeResult<TIn, TOut>>> HandleSafeExecution<TIn, TOut>(
        ILogger logger,
        IPipelineProgressNotifier progressNotifier,
        TimeProvider timeProvider,
        string callbackName,
        Func<TIn, CancellationToken, IEnumerable<TOut>> callback,
        CancellationToken cancellationToken = default) => input => ExecuteSafeStreamingAsync(logger, progressNotifier, timeProvider, input, callbackName, WrapSyncEnumerable(callback), null, cancellationToken);

    internal static Func<TIn, IAsyncEnumerable<SafeResult<TIn, TOut>>> HandleSafeExecution<TIn, TOut>(
        ILogger logger,
        IPipelineProgressNotifier progressNotifier,
        TimeProvider timeProvider,
        PipelineStepTracker stepTracker,
        Func<TIn, CancellationToken, IEnumerable<TOut>> callback,
        CancellationToken cancellationToken = default) => input => ExecuteSafeStreamingAsync(logger, progressNotifier, timeProvider, input, stepTracker.TelemetryName, WrapSyncEnumerable(callback), stepTracker, cancellationToken);

    private static Func<TIn, CancellationToken, Task<bool>> WrapSyncAction<TIn>(Action<TIn> callback) => (input, _) =>
    {
        callback(input);
        return Task.FromResult(true);
    };

    private static Func<TIn, CancellationToken, Task<bool>> WrapAsyncAction<TIn>(Func<TIn, CancellationToken, Task> callback) => async (input, ct) =>
    {
        await callback(input, ct).ConfigureAwait(false);
        return true;
    };

    private static Func<TIn, CancellationToken, Task<TOut>> WrapSyncFunc<TIn, TOut>(Func<TIn, TOut> callback) => (input, _) => Task.FromResult(callback(input));

    private static Func<TIn, CancellationToken, IAsyncEnumerable<TOut>> WrapSyncEnumerable<TIn, TOut>(Func<TIn, CancellationToken, IEnumerable<TOut>> callback) => (input, ct) => IterateSyncEnumerableAsync(callback, input, ct);

    private static async IAsyncEnumerable<TOut> IterateSyncEnumerableAsync<TIn, TOut>(
        Func<TIn, CancellationToken, IEnumerable<TOut>> callback,
        TIn input,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (TOut item in callback(input, cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return item;
        }

        await Task.CompletedTask.ConfigureAwait(false);
    }

    private static async Task<TOut> ExecuteCoreAsync<TIn, TOut>(
        ILogger logger,
        IPipelineProgressNotifier progressNotifier,
        TimeProvider timeProvider,
        TIn input,
        string callbackName,
        Func<TIn, CancellationToken, Task<TOut>> callback,
        RetryOptions? retryOptions,
        PipelineStepTracker? stepTracker,
        bool emitToDownstream,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using Activity? activity = TelemetryConstants.DefaultInstance.StartActivity(callbackName, ActivityKind.Internal);
        DateTimeOffset startTime = await StartTelemetryAsync(logger, progressNotifier, timeProvider, activity, callbackName, stepTracker, cancellationToken).ConfigureAwait(false);

        int attempt = 0;
        int delayMs = retryOptions?.InitialDelayMs ?? 0;

        while (true)
        {
            try
            {
                TOut output = await callback(input, cancellationToken).ConfigureAwait(false);
                await LogAndTrackStepCompletionAsync(logger, progressNotifier, timeProvider, activity, startTime, callbackName, stepTracker, emitToDownstream, null, cancellationToken).ConfigureAwait(false);
                return output;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                (bool shouldRetry, attempt, delayMs) = await HandleRetryAsync(logger, progressNotifier, timeProvider, activity, callbackName, ex, attempt, delayMs, startTime, retryOptions, stepTracker, cancellationToken).ConfigureAwait(false);
                if (!shouldRetry)
                    throw;
            }
        }
    }

    private static async Task<SafeResult<TIn>> SafeExecuteActionCoreAsync<TIn>(
        ILogger logger,
        IPipelineProgressNotifier progressNotifier,
        TimeProvider timeProvider,
        TIn input,
        string callbackName,
        Func<TIn, CancellationToken, Task<bool>> callback,
        RetryOptions? retryOptions,
        PipelineStepTracker? stepTracker,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await ExecuteCoreAsync(logger, progressNotifier, timeProvider, input, callbackName, callback, retryOptions, stepTracker, false, cancellationToken).ConfigureAwait(false);
            return SafeResult<TIn>.CreateSuccess();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return new(callbackName, input, ex);
        }
    }

    private static async Task<SafeResult<TIn, TOut>> SafeExecuteCoreAsync<TIn, TOut>(
        ILogger logger,
        IPipelineProgressNotifier progressNotifier,
        TimeProvider timeProvider,
        TIn input,
        string callbackName,
        Func<TIn, CancellationToken, Task<TOut>> callback,
        RetryOptions? retryOptions,
        PipelineStepTracker? stepTracker,
        bool emitToDownstream,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await ExecuteCoreAsync(logger, progressNotifier, timeProvider, input, callbackName, callback, retryOptions, stepTracker, emitToDownstream, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return new(callbackName, input, ex);
        }
    }

    private static async IAsyncEnumerable<TOut> ExecuteStreamingAsync<TIn, TOut>(
        ILogger logger,
        IPipelineProgressNotifier progressNotifier,
        TimeProvider timeProvider,
        TIn input,
        string callbackName,
        Func<TIn, CancellationToken, IAsyncEnumerable<TOut>> callback,
        PipelineStepTracker? stepTracker,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using Activity? activity = TelemetryConstants.DefaultInstance.StartActivity(callbackName, ActivityKind.Internal);
        DateTimeOffset startTime = await StartTelemetryAsync(logger, progressNotifier, timeProvider, activity, callbackName, stepTracker, cancellationToken).ConfigureAwait(false);
        int itemCount = 0;

        IAsyncEnumerator<TOut> enumerator = callback(input, cancellationToken).GetAsyncEnumerator(cancellationToken);
        await using (enumerator.ConfigureAwait(false))
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                bool hasNext;
                try
                {
                    hasNext = await enumerator.MoveNextAsync().ConfigureAwait(false);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    await LogAndTrackStepFailureAsync(logger, progressNotifier, timeProvider, activity, startTime, callbackName, ex, stepTracker, cancellationToken).ConfigureAwait(false);
                    throw;
                }

                if (!hasNext)
                    break;

                itemCount++;
                await NotifyStreamingProgressIfNeededAsync(progressNotifier, timeProvider, callbackName, itemCount, stepTracker, cancellationToken).ConfigureAwait(false);
                if (stepTracker is not null)
                    await stepTracker.OnItemEmittedAsync(cancellationToken).ConfigureAwait(false);

                yield return enumerator.Current;
            }
        }

        await LogAndTrackStepCompletionAsync(logger, progressNotifier, timeProvider, activity, startTime, callbackName, stepTracker, false, $"Items emitted: {itemCount}", cancellationToken).ConfigureAwait(false);
    }

    private static async IAsyncEnumerable<SafeResult<TIn, TOut>> ExecuteSafeStreamingAsync<TIn, TOut>(
        ILogger logger,
        IPipelineProgressNotifier progressNotifier,
        TimeProvider timeProvider,
        TIn input,
        string callbackName,
        Func<TIn, CancellationToken, IAsyncEnumerable<TOut>> callback,
        PipelineStepTracker? stepTracker,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using Activity? activity = TelemetryConstants.DefaultInstance.StartActivity(callbackName, ActivityKind.Internal);
        DateTimeOffset startTime = await StartTelemetryAsync(logger, progressNotifier, timeProvider, activity, callbackName, stepTracker, cancellationToken).ConfigureAwait(false);
        int itemCount = 0;

        IAsyncEnumerator<TOut> enumerator = callback(input, cancellationToken).GetAsyncEnumerator(cancellationToken);
        await using (enumerator.ConfigureAwait(false))
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                bool hasNext = false;
                Exception? exception = null;
                try
                {
                    hasNext = await enumerator.MoveNextAsync().ConfigureAwait(false);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    await LogAndTrackStepFailureAsync(logger, progressNotifier, timeProvider, activity, startTime, callbackName, ex, stepTracker, cancellationToken).ConfigureAwait(false);
                    exception = ex;
                }

                if (exception is not null)
                {
                    yield return (callbackName, input, exception);
                    yield break;
                }

                if (!hasNext)
                    break;

                itemCount++;
                await NotifyStreamingProgressIfNeededAsync(progressNotifier, timeProvider, callbackName, itemCount, stepTracker, cancellationToken).ConfigureAwait(false);
                if (stepTracker is not null)
                    await stepTracker.OnItemEmittedAsync(cancellationToken).ConfigureAwait(false);

                yield return enumerator.Current;
            }
        }

        await LogAndTrackStepCompletionAsync(logger, progressNotifier, timeProvider, activity, startTime, callbackName, stepTracker, false, $"Items emitted: {itemCount}", cancellationToken).ConfigureAwait(false);
    }

    private static ValueTask NotifyStreamingProgressIfNeededAsync(
        IPipelineProgressNotifier progressNotifier,
        TimeProvider timeProvider,
        string callbackName,
        int itemCount,
        PipelineStepTracker? stepTracker,
        CancellationToken cancellationToken)
    {
        if (stepTracker is not null)
            return stepTracker.OnStreamingProgressAsync(itemCount, cancellationToken);

        if (itemCount != 1 && itemCount % 500 != 0)
            return ValueTask.CompletedTask;

        return progressNotifier.NotifyAsync(
            new PipelineProgressEvent
            {
                StepName = callbackName,
                Description = $"Streaming '{callbackName}'.",
                State = PipelineStepState.Running,
                Timestamp = timeProvider.GetUtcNow(),
                ReceivedItems = 1,
                ProcessedItems = itemCount,
                ProgressPercentage = 0.0,
                Details = $"Items emitted: {itemCount}"
            },
            cancellationToken);
    }

    private static async ValueTask<DateTimeOffset> StartTelemetryAsync(
        ILogger logger,
        IPipelineProgressNotifier progressNotifier,
        TimeProvider timeProvider,
        Activity? activity,
        string name,
        PipelineStepTracker? stepTracker,
        CancellationToken cancellationToken = default)
    {
        activity?.SetTag("execution.name", name);

        if (stepTracker is not null)
            return await stepTracker.OnItemStartedAsync(cancellationToken).ConfigureAwait(false);

        DateTimeOffset startTime = timeProvider.GetUtcNow();
        logger.LogInformation("{StartTime:O} - Starting '{Name}'.", startTime, name);
        await progressNotifier.NotifyAsync(
            new PipelineProgressEvent
            {
                StepName = name,
                Description = $"Starting '{name}'.",
                State = PipelineStepState.Started,
                Timestamp = startTime,
                ReceivedItems = 1,
                ProcessedItems = 0,
                ProgressPercentage = 0.0
            },
            cancellationToken).ConfigureAwait(false);
        return startTime;
    }

    private static async ValueTask LogAndTrackStepCompletionAsync(
        ILogger logger,
        IPipelineProgressNotifier progressNotifier,
        TimeProvider timeProvider,
        Activity? activity,
        DateTimeOffset startTime,
        string name,
        PipelineStepTracker? stepTracker,
        bool emitToDownstream,
        string? details = null,
        CancellationToken cancellationToken = default)
    {
        activity?.SetStatus(ActivityStatusCode.Ok);

        if (stepTracker is not null)
        {
            await stepTracker.OnItemProcessedAsync(details, cancellationToken).ConfigureAwait(false);
            if (emitToDownstream)
                await stepTracker.OnItemEmittedAsync(cancellationToken).ConfigureAwait(false);

            return;
        }

        DateTimeOffset endTime = timeProvider.GetUtcNow();
        TimeSpan duration = endTime - startTime;
        logger.LogInformation("{EndTime:O} - Duration: {Duration} - Successfully completed '{Name}'.", endTime, duration, name);
        await progressNotifier.NotifyAsync(
            new PipelineProgressEvent
            {
                StepName = name,
                Description = $"Successfully completed '{name}'.",
                State = PipelineStepState.Completed,
                Timestamp = endTime,
                ReceivedItems = 1,
                ProcessedItems = 1,
                ProgressPercentage = 100.0,
                Details = details ?? $"Duration: {duration}"
            },
            cancellationToken).ConfigureAwait(false);
    }

    private static async ValueTask LogAndTrackStepFailureAsync(
        ILogger logger,
        IPipelineProgressNotifier progressNotifier,
        TimeProvider timeProvider,
        Activity? activity,
        DateTimeOffset startTime,
        string name,
        Exception ex,
        PipelineStepTracker? stepTracker,
        CancellationToken cancellationToken = default)
    {
        activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
        activity?.AddException(ex);

        if (stepTracker is not null)
        {
            await stepTracker.OnStepFaultedAsync(startTime, ex, cancellationToken).ConfigureAwait(false);
            return;
        }

        DateTimeOffset endTime = timeProvider.GetUtcNow();
        TimeSpan duration = endTime - startTime;
        logger.LogError(ex, "{EndTime:O} - Duration: {Duration} - Failed '{Name}'.", endTime, duration, name);
        await progressNotifier.NotifyAsync(
            new PipelineProgressEvent
            {
                StepName = name,
                Description = $"Failed '{name}'.",
                State = PipelineStepState.Faulted,
                Timestamp = endTime,
                ReceivedItems = 1,
                ProcessedItems = 0,
                ProgressPercentage = 0.0,
                Details = ex.Message
            },
            cancellationToken).ConfigureAwait(false);
    }

    private static void LogFallbackConditionMet(ILogger logger, string callbackName, string fallbackName)
        => logger.LogInformation("Fallback condition met for '{CallbackName}'. Rerouting to '{FallbackName}'.", callbackName, fallbackName);

    private static async Task<(bool ShouldRetry, int Attempt, int DelayMs)> HandleRetryAsync(
        ILogger logger,
        IPipelineProgressNotifier progressNotifier,
        TimeProvider timeProvider,
        Activity? activity,
        string callbackName,
        Exception exception,
        int attempt,
        int delayMs,
        DateTimeOffset startTime,
        RetryOptions? retryOptions,
        PipelineStepTracker? stepTracker,
        CancellationToken cancellationToken = default)
    {
        int currentAttempt = attempt + 1;
        if (retryOptions == null || currentAttempt >= retryOptions.Value.MaxAttempts)
        {
            await LogAndTrackStepFailureAsync(logger, progressNotifier, timeProvider, activity, startTime, callbackName, exception, stepTracker, cancellationToken).ConfigureAwait(false);
            return (false, currentAttempt, delayMs);
        }

        logger.LogWarning(exception, "Attempt {Attempt} failed for '{Name}'. Retrying in {Delay}ms...", currentAttempt, callbackName, delayMs);
        await Task.Delay(TimeSpan.FromMilliseconds(delayMs), timeProvider, cancellationToken).ConfigureAwait(false);

        int currentDelayMs = (int)(delayMs * retryOptions.Value.BackoffFactor);
        return (true, currentAttempt, currentDelayMs);
    }
}
