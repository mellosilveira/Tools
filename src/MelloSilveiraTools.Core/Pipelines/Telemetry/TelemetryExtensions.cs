using MelloSilveiraTools.Core.Pipelines.Models;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace MelloSilveiraTools.Core.Pipelines.Telemetry;

[StackTraceHidden]
public static class TelemetryExtensions
{
    /// <summary>
    /// Wraps a synchronous action with OpenTelemetry tracing and structured logging.
    /// </summary>
    /// <typeparam name="TIn">Input message type.</typeparam>
    /// <param name="logger">Structured logger instance.</param>
    /// <param name="timeProvider">Time provider instance.</param>
    /// <param name="callbackName">Logical callback name for telemetry.</param>
    /// <param name="callback">Synchronous delegate executed for each input.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Asynchronous wrapped delegate.</returns>
    public static Func<TIn, Task> HandleExecution<TIn>(
        ILogger logger,
        TimeProvider timeProvider,
        string callbackName,
        Action<TIn> callback,
        CancellationToken cancellationToken = default)
        => input => ExecuteCoreAsync(logger, timeProvider, input, callbackName, WrapSyncAction(callback), cancellationToken);

    /// <summary>
    /// Wraps a synchronous action, capturing exceptions into a <see cref="SafeResult{TIn}"/> envelope for centralized error routing.
    /// </summary>
    /// <typeparam name="TIn">Input message type.</typeparam>
    /// <param name="logger">Structured logger instance.</param>
    /// <param name="timeProvider">Time provider instance.</param>
    /// <param name="callbackName">Logical callback name for telemetry.</param>
    /// <param name="callback">Synchronous delegate executed for each input.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Safe execution delegate returning <see cref="SafeResult{TIn}"/>.</returns>
    public static Func<TIn, Task<SafeResult<TIn>>> HandleSafeExecution<TIn>(
        ILogger logger,
        TimeProvider timeProvider,
        string callbackName,
        Action<TIn> callback,
        CancellationToken cancellationToken = default)
        => input => SafeExecuteActionCoreAsync(logger, timeProvider, input, callbackName, WrapSyncAction(callback), cancellationToken);

    /// <summary>
    /// Wraps an asynchronous action with OpenTelemetry tracing and structured logging.
    /// </summary>
    /// <typeparam name="TIn">Input message type.</typeparam>
    /// <param name="logger">Structured logger instance.</param>
    /// <param name="timeProvider">Time provider instance.</param>
    /// <param name="callbackName">Logical callback name for telemetry.</param>
    /// <param name="callback">Asynchronous delegate executed for each input.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Asynchronous wrapped delegate.</returns>
    public static Func<TIn, Task> HandleExecution<TIn>(
        ILogger logger,
        TimeProvider timeProvider,
        string callbackName,
        Func<TIn, CancellationToken, Task> callback,
        CancellationToken cancellationToken = default)
        => input => ExecuteCoreAsync(logger, timeProvider, input, callbackName, WrapAsyncAction(callback), cancellationToken);

    /// <summary>
    /// Wraps an asynchronous action, combining execution with a <see cref="SafeResult{TIn}"/> envelope for terminal failures.
    /// </summary>
    /// <typeparam name="TIn">Input message type.</typeparam>
    /// <param name="logger">Structured logger instance.</param>
    /// <param name="timeProvider">Time provider instance.</param>
    /// <param name="callbackName">Logical callback name for telemetry.</param>
    /// <param name="callback">Asynchronous delegate executed for each input.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Safe execution delegate returning <see cref="SafeResult{TIn}"/>.</returns>
    public static Func<TIn, Task<SafeResult<TIn>>> HandleSafeExecution<TIn>(
        ILogger logger,
        TimeProvider timeProvider,
        string callbackName,
        Func<TIn, CancellationToken, Task> callback,
        CancellationToken cancellationToken = default)
        => input => SafeExecuteActionCoreAsync(logger, timeProvider, input, callbackName, WrapAsyncAction(callback), cancellationToken);

    /// <summary>
    /// Wraps a synchronous mapping function with OpenTelemetry tracing and structured logging, returning the mapped output.
    /// </summary>
    /// <typeparam name="TIn">Input message type.</typeparam>
    /// <typeparam name="TOut">Output message type.</typeparam>
    /// <param name="logger">Structured logger instance.</param>
    /// <param name="timeProvider">Time provider instance.</param>
    /// <param name="callbackName">Logical callback name for telemetry.</param>
    /// <param name="callback">Synchronous transform function.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Asynchronous transform delegate.</returns>
    public static Func<TIn, Task<TOut>> HandleExecution<TIn, TOut>(
        ILogger logger,
        TimeProvider timeProvider,
        string callbackName,
        Func<TIn, TOut> callback,
        CancellationToken cancellationToken = default)
        => input => ExecuteCoreAsync(logger, timeProvider, input, callbackName, WrapSyncFunc(callback), cancellationToken);

    /// <summary>
    /// Wraps a synchronous mapping function, intercepting failures into a <see cref="SafeResult{TIn, TOut}"/> to permit DLQ offloading.
    /// </summary>
    /// <typeparam name="TIn">Input message type.</typeparam>
    /// <typeparam name="TOut">Output message type.</typeparam>
    /// <param name="logger">Structured logger instance.</param>
    /// <param name="timeProvider">Time provider instance.</param>
    /// <param name="callbackName">Logical callback name for telemetry.</param>
    /// <param name="callback">Synchronous transform function.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Safe execution delegate returning <see cref="SafeResult{TIn, TOut}"/>.</returns>
    public static Func<TIn, Task<SafeResult<TIn, TOut>>> HandleSafeExecution<TIn, TOut>(
        ILogger logger,
        TimeProvider timeProvider,
        string callbackName,
        Func<TIn, TOut> callback,
        CancellationToken cancellationToken = default)
        => input => SafeExecuteCoreAsync(logger, timeProvider, input, callbackName, WrapSyncFunc(callback), cancellationToken);

    /// <summary>
    /// Wraps an asynchronous mapping function with support for telemetry tracking and structured logging.
    /// </summary>
    /// <typeparam name="TIn">Input message type.</typeparam>
    /// <typeparam name="TOut">Output message type.</typeparam>
    /// <param name="logger">Structured logger instance.</param>
    /// <param name="timeProvider">Time provider instance.</param>
    /// <param name="callbackName">Logical callback name for telemetry.</param>
    /// <param name="callback">Asynchronous transform function.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Asynchronous transform delegate.</returns>
    public static Func<TIn, Task<TOut>> HandleExecution<TIn, TOut>(
        ILogger logger,
        TimeProvider timeProvider,
        string callbackName,
        Func<TIn, CancellationToken, Task<TOut>> callback,
        CancellationToken cancellationToken = default)
        => input => ExecuteCoreAsync(logger, timeProvider, input, callbackName, callback, cancellationToken);

    /// <summary>
    /// Wraps an asynchronous mapping function, catching failures into a bifurcated <see cref="SafeResult{TIn, TOut}"/>.
    /// </summary>
    /// <typeparam name="TIn">Input message type.</typeparam>
    /// <typeparam name="TOut">Output message type.</typeparam>
    /// <param name="logger">Structured logger instance.</param>
    /// <param name="timeProvider">Time provider instance.</param>
    /// <param name="callbackName">Logical callback name for telemetry.</param>
    /// <param name="callback">Asynchronous transform function.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Safe execution delegate returning <see cref="SafeResult{TIn, TOut}"/>.</returns>
    public static Func<TIn, Task<SafeResult<TIn, TOut>>> HandleSafeExecution<TIn, TOut>(
        ILogger logger,
        TimeProvider timeProvider,
        string callbackName,
        Func<TIn, CancellationToken, Task<TOut>> callback,
        CancellationToken cancellationToken = default)
        => input => SafeExecuteCoreAsync(logger, timeProvider, input, callbackName, callback, cancellationToken);

    /// <summary>
    /// Wraps a synchronous conditional forking operation evaluating the output payload after the primary execution completes.
    /// </summary>
    /// <typeparam name="TIn">Input message type.</typeparam>
    /// <typeparam name="TOut">Output message type.</typeparam>
    /// <param name="logger">Structured logger instance.</param>
    /// <param name="timeProvider">Time provider instance.</param>
    /// <param name="callbackName">Logical callback name for primary step.</param>
    /// <param name="fallbackName">Logical callback name for fallback step.</param>
    /// <param name="callback">Primary transform function.</param>
    /// <param name="fallbackCondition">Predicate determining if fallback is needed.</param>
    /// <param name="fallback">Fallback transform function.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Asynchronous conditional fork delegate.</returns>
    public static Func<TIn, Task<TOut>> HandleExecution<TIn, TOut>(
        ILogger logger,
        TimeProvider timeProvider,
        string callbackName,
        string fallbackName,
        Func<TIn, TOut> callback,
        Func<TOut, bool> fallbackCondition,
        Func<TIn, TOut> fallback,
        CancellationToken cancellationToken = default)
        => HandleExecution(logger, timeProvider, callbackName, fallbackName, WrapSyncFunc(callback), fallbackCondition, WrapSyncFunc(fallback), cancellationToken);

    /// <summary>
    /// Wraps a synchronous post-execution conditional forking operation, evaluating against the <see cref="SafeResult{TIn, TOut}"/> state.
    /// </summary>
    /// <typeparam name="TIn">Input message type.</typeparam>
    /// <typeparam name="TOut">Output message type.</typeparam>
    /// <param name="logger">Structured logger instance.</param>
    /// <param name="timeProvider">Time provider instance.</param>
    /// <param name="callbackName">Logical callback name for primary step.</param>
    /// <param name="fallbackName">Logical callback name for fallback step.</param>
    /// <param name="callback">Primary transform function.</param>
    /// <param name="fallbackCondition">Predicate determining if fallback is needed.</param>
    /// <param name="fallback">Fallback transform function.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Safe execution delegate returning <see cref="SafeResult{TIn, TOut}"/>.</returns>
    public static Func<TIn, Task<SafeResult<TIn, TOut>>> HandleSafeExecution<TIn, TOut>(
        ILogger logger,
        TimeProvider timeProvider,
        string callbackName,
        string fallbackName,
        Func<TIn, TOut> callback,
        Func<SafeResult<TIn, TOut>, bool> fallbackCondition,
        Func<TIn, TOut> fallback,
        CancellationToken cancellationToken = default)
        => HandleSafeExecution(logger, timeProvider, callbackName, fallbackName, WrapSyncFunc(callback), fallbackCondition, WrapSyncFunc(fallback), cancellationToken);

    /// <summary>
    /// Wraps an asynchronous conditional forking operation evaluating the output payload after the primary execution completes.
    /// </summary>
    /// <typeparam name="TIn">Input message type.</typeparam>
    /// <typeparam name="TOut">Output message type.</typeparam>
    /// <param name="logger">Structured logger instance.</param>
    /// <param name="timeProvider">Time provider instance.</param>
    /// <param name="callbackName">Logical callback name for primary step.</param>
    /// <param name="fallbackName">Logical callback name for fallback step.</param>
    /// <param name="callback">Primary asynchronous transform function.</param>
    /// <param name="fallbackCondition">Predicate determining if fallback is needed.</param>
    /// <param name="fallback">Fallback asynchronous transform function.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Asynchronous conditional fork delegate.</returns>
    public static Func<TIn, Task<TOut>> HandleExecution<TIn, TOut>(
        ILogger logger,
        TimeProvider timeProvider,
        string callbackName,
        string fallbackName,
        Func<TIn, CancellationToken, Task<TOut>> callback,
        Func<TOut, bool> fallbackCondition,
        Func<TIn, CancellationToken, Task<TOut>> fallback,
        CancellationToken cancellationToken = default) => async input =>
    {
        TOut output = await ExecuteCoreAsync(logger, timeProvider, input, callbackName, callback, cancellationToken).ConfigureAwait(false);
        if (fallbackCondition(output))
        {
            LogFallbackConditionMet(logger, callbackName, fallbackName);
            return await ExecuteCoreAsync(logger, timeProvider, input, fallbackName, fallback, cancellationToken).ConfigureAwait(false);
        }

        return output;
    };

    /// <summary>
    /// Wraps an asynchronous post-execution conditional forking operation, evaluating against the <see cref="SafeResult{TIn, TOut}"/> state.
    /// </summary>
    /// <typeparam name="TIn">Input message type.</typeparam>
    /// <typeparam name="TOut">Output message type.</typeparam>
    /// <param name="logger">Structured logger instance.</param>
    /// <param name="timeProvider">Time provider instance.</param>
    /// <param name="callbackName">Logical callback name for primary step.</param>
    /// <param name="fallbackName">Logical callback name for fallback step.</param>
    /// <param name="callback">Primary asynchronous transform function.</param>
    /// <param name="fallbackCondition">Predicate determining if fallback is needed.</param>
    /// <param name="fallback">Fallback asynchronous transform function.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Safe execution delegate returning <see cref="SafeResult{TIn, TOut}"/>.</returns>
    public static Func<TIn, Task<SafeResult<TIn, TOut>>> HandleSafeExecution<TIn, TOut>(
        ILogger logger,
        TimeProvider timeProvider,
        string callbackName,
        string fallbackName,
        Func<TIn, CancellationToken, Task<TOut>> callback,
        Func<SafeResult<TIn, TOut>, bool> fallbackCondition,
        Func<TIn, CancellationToken, Task<TOut>> fallback,
        CancellationToken cancellationToken = default) => async input =>
    {
        SafeResult<TIn, TOut> safeResult = await SafeExecuteCoreAsync(logger, timeProvider, input, callbackName, callback, cancellationToken).ConfigureAwait(false);
        if (fallbackCondition(safeResult))
        {
            LogFallbackConditionMet(logger, callbackName, fallbackName);
            return await SafeExecuteCoreAsync(logger, timeProvider, input, fallbackName, fallback, cancellationToken).ConfigureAwait(false);
        }

        return safeResult;
    };

    /// <summary>
    /// Wraps a streaming function returning an <see cref="IAsyncEnumerable{TOut}"/> with OpenTelemetry tracing and structured logging.
    /// </summary>
    /// <typeparam name="TIn">Input message type.</typeparam>
    /// <typeparam name="TOut">Output message type.</typeparam>
    /// <param name="logger">Structured logger instance.</param>
    /// <param name="timeProvider">Time provider instance.</param>
    /// <param name="callbackName">Logical callback name for telemetry.</param>
    /// <param name="callback">Asynchronous enumerable generator.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Streaming execution delegate.</returns>
    public static Func<TIn, IAsyncEnumerable<TOut>> HandleExecution<TIn, TOut>(
        ILogger logger,
        TimeProvider timeProvider,
        string callbackName,
        Func<TIn, CancellationToken, IAsyncEnumerable<TOut>> callback,
        CancellationToken cancellationToken = default)
        => input => ExecuteStreamingAsync(logger, timeProvider, input, callbackName, callback, cancellationToken);

    /// <summary>
    /// Wraps a streaming function returning an <see cref="IAsyncEnumerable{TOut}"/> with OpenTelemetry tracing, structured logging, and safe DLQ routing.
    /// </summary>
    /// <typeparam name="TIn">Input message type.</typeparam>
    /// <typeparam name="TOut">Output message type.</typeparam>
    /// <param name="logger">Structured logger instance.</param>
    /// <param name="timeProvider">Time provider instance.</param>
    /// <param name="callbackName">Logical callback name for telemetry.</param>
    /// <param name="callback">Asynchronous enumerable generator.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Safe streaming execution delegate returning <see cref="SafeResult{TIn, TOut}"/>.</returns>
    public static Func<TIn, IAsyncEnumerable<SafeResult<TIn, TOut>>> HandleSafeExecution<TIn, TOut>(
        ILogger logger,
        TimeProvider timeProvider,
        string callbackName,
        Func<TIn, CancellationToken, IAsyncEnumerable<TOut>> callback,
        CancellationToken cancellationToken = default)
        => input => ExecuteSafeStreamingAsync(logger, timeProvider, input, callbackName, callback, cancellationToken);

    /// <summary>
    /// Wraps an enumerable transformation function returning an <see cref="IEnumerable{TOut}"/> with OpenTelemetry tracing and structured logging.
    /// </summary>
    /// <typeparam name="TIn">Input message type.</typeparam>
    /// <typeparam name="TOut">Output message type.</typeparam>
    /// <param name="logger">Structured logger instance.</param>
    /// <param name="timeProvider">Time provider instance.</param>
    /// <param name="callbackName">Logical callback name for telemetry.</param>
    /// <param name="callback">Synchronous enumerable transform function.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Streaming execution delegate.</returns>
    public static Func<TIn, IAsyncEnumerable<TOut>> HandleExecution<TIn, TOut>(
        ILogger logger,
        TimeProvider timeProvider,
        string callbackName,
        Func<TIn, CancellationToken, IEnumerable<TOut>> callback,
        CancellationToken cancellationToken = default)
        => input => ExecuteStreamingAsync(logger, timeProvider, input, callbackName, WrapSyncEnumerable(callback), cancellationToken);

    /// <summary>
    /// Wraps an enumerable transformation function returning an <see cref="IEnumerable{TOut}"/> with OpenTelemetry tracing, structured logging, and safe DLQ routing.
    /// </summary>
    /// <typeparam name="TIn">Input message type.</typeparam>
    /// <typeparam name="TOut">Output message type.</typeparam>
    /// <param name="logger">Structured logger instance.</param>
    /// <param name="timeProvider">Time provider instance.</param>
    /// <param name="callbackName">Logical callback name for telemetry.</param>
    /// <param name="callback">Synchronous enumerable transform function.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Safe streaming execution delegate returning <see cref="SafeResult{TIn, TOut}"/>.</returns>
    public static Func<TIn, IAsyncEnumerable<SafeResult<TIn, TOut>>> HandleSafeExecution<TIn, TOut>(
        ILogger logger,
        TimeProvider timeProvider,
        string callbackName,
        Func<TIn, CancellationToken, IEnumerable<TOut>> callback,
        CancellationToken cancellationToken = default)
        => input => ExecuteSafeStreamingAsync(logger, timeProvider, input, callbackName, WrapSyncEnumerable(callback), cancellationToken);

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
        TimeProvider timeProvider,
        TIn input,
        string callbackName,
        Func<TIn, CancellationToken, Task<TOut>> callback,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using Activity? activity = TelemetryConstants.DefaultInstance.StartActivity(callbackName, ActivityKind.Internal);
        DateTimeOffset startTime = StartTelemetry(logger, timeProvider, activity, callbackName);

        try
        {
            TOut output = await callback(input, cancellationToken).ConfigureAwait(false);
            LogAndTrackStepCompletion(logger, timeProvider, activity, startTime, callbackName);
            return output;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogAndTrackStepFailure(logger, timeProvider, activity, startTime, callbackName, ex);
            throw;
        }
    }

    private static async Task<SafeResult<TIn>> SafeExecuteActionCoreAsync<TIn>(
        ILogger logger,
        TimeProvider timeProvider,
        TIn input,
        string callbackName,
        Func<TIn, CancellationToken, Task<bool>> callback,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await ExecuteCoreAsync(logger, timeProvider, input, callbackName, callback, cancellationToken).ConfigureAwait(false);
            return SafeResult<TIn>.CreateSuccess();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return new(callbackName, input, ex);
        }
    }

    private static async Task<SafeResult<TIn, TOut>> SafeExecuteCoreAsync<TIn, TOut>(
        ILogger logger,
        TimeProvider timeProvider,
        TIn input,
        string callbackName,
        Func<TIn, CancellationToken, Task<TOut>> callback,
        CancellationToken cancellationToken = default)
    {
        try
        {
            TOut output = await ExecuteCoreAsync(logger, timeProvider, input, callbackName, callback, cancellationToken).ConfigureAwait(false);
            return new(output);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return new(callbackName, input, ex);
        }
    }

    private static async IAsyncEnumerable<TOut> ExecuteStreamingAsync<TIn, TOut>(
        ILogger logger,
        TimeProvider timeProvider,
        TIn input,
        string callbackName,
        Func<TIn, CancellationToken, IAsyncEnumerable<TOut>> callback,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using Activity? activity = TelemetryConstants.DefaultInstance.StartActivity(callbackName, ActivityKind.Internal);
        DateTimeOffset startTime = StartTelemetry(logger, timeProvider, activity, callbackName);
        int itemCount = 0;

        await using (IAsyncEnumerator<TOut> enumerator = callback(input, cancellationToken).GetAsyncEnumerator(cancellationToken))
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
                    LogAndTrackStepFailure(logger, timeProvider, activity, startTime, callbackName, ex);
                    throw;
                }

                if (!hasNext)
                    break;

                itemCount++;
                yield return enumerator.Current;
            }
        }

        LogAndTrackStepCompletion(logger, timeProvider, activity, startTime, callbackName, $"Items emitted: {itemCount}");
    }

    private static async IAsyncEnumerable<SafeResult<TIn, TOut>> ExecuteSafeStreamingAsync<TIn, TOut>(
        ILogger logger,
        TimeProvider timeProvider,
        TIn input,
        string callbackName,
        Func<TIn, CancellationToken, IAsyncEnumerable<TOut>> callback,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using Activity? activity = TelemetryConstants.DefaultInstance.StartActivity(callbackName, ActivityKind.Internal);
        DateTimeOffset startTime = StartTelemetry(logger, timeProvider, activity, callbackName);
        int itemCount = 0;

        await using (IAsyncEnumerator<TOut> enumerator = callback(input, cancellationToken).GetAsyncEnumerator(cancellationToken))
        {
            while (true)
            {
                bool hasNext;
                Exception? exception = null;
                try
                {
                    hasNext = await enumerator.MoveNextAsync().ConfigureAwait(false);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    LogAndTrackStepFailure(logger, timeProvider, activity, startTime, callbackName, ex);
                    exception = ex;
                    hasNext = false;
                }

                if (exception is not null)
                {
                    yield return (callbackName, input, exception);
                    yield break;
                }

                if (!hasNext)
                    break;

                itemCount++;
                yield return enumerator.Current;
            }
        }

        LogAndTrackStepCompletion(logger, timeProvider, activity, startTime, callbackName, $"Items emitted: {itemCount}");
    }

    private static DateTimeOffset StartTelemetry(
        ILogger logger,
        TimeProvider timeProvider,
        Activity? activity,
        string name)
    {
        activity?.SetTag("execution.name", name);

        DateTimeOffset startTime = timeProvider.GetUtcNow();
        logger.LogInformation("{StartTime:O} - Starting '{Name}'.", startTime, name);
        return startTime;
    }

    private static void LogAndTrackStepCompletion(
        ILogger logger,
        TimeProvider timeProvider,
        Activity? activity,
        DateTimeOffset startTime,
        string name,
        string? details = null)
    {
        activity?.SetStatus(ActivityStatusCode.Ok);

        DateTimeOffset endTime = timeProvider.GetUtcNow();
        TimeSpan duration = endTime - startTime;

        if (details is not null)
            logger.LogInformation("{EndTime:O} - Duration: {Duration} - Successfully completed '{Name}'. Details: {Details}", endTime, duration, name, details);
        else
            logger.LogInformation("{EndTime:O} - Duration: {Duration} - Successfully completed '{Name}'.", endTime, duration, name);
    }

    private static void LogAndTrackStepFailure(
        ILogger logger,
        TimeProvider timeProvider,
        Activity? activity,
        DateTimeOffset startTime,
        string name,
        Exception ex)
    {
        activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
        activity?.AddException(ex);

        DateTimeOffset endTime = timeProvider.GetUtcNow();
        TimeSpan duration = endTime - startTime;
        logger.LogError(ex, "{EndTime:O} - Duration: {Duration} - Failed '{Name}'.", endTime, duration, name);
    }

    private static void LogFallbackConditionMet(ILogger logger, string callbackName, string fallbackName) => logger.LogInformation("Fallback condition met for '{CallbackName}'. Rerouting to '{FallbackName}'.", callbackName, fallbackName);
}
