using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace MelloSilveiraTools.Core.Pipelines;

/// <summary>
/// Provides structured logging wrappers, ensuring consistent tracking of execution lifecycles, durations, and failures.
/// </summary>
public static class TelemetryExtensions
{
    public static Action<TIn> HandleExecution<TIn>(ILogger logger, string callbackName, Action<TIn> callback, CancellationToken cancellationToken = default) 
        => [StackTraceHidden] (input) =>
        {
            using Activity? activity = Telemetry.DefaultInstance.StartActivity(callbackName, ActivityKind.Internal);
            Execute(logger, activity, input, callbackName, callback, cancellationToken);
        };

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

    public static Func<TIn, Task> HandleExecution<TIn>(ILogger logger, string callbackName, Func<TIn, CancellationToken, Task> callback, CancellationToken cancellationToken = default) 
        => [StackTraceHidden] async (input) =>
        {
            using Activity? activity = Telemetry.DefaultInstance.StartActivity(callbackName, ActivityKind.Internal);
            await ExecuteAsync(logger, activity, input, callbackName, callback, cancellationToken).ConfigureAwait(false);
        };

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

    public static Func<TIn, Task<TOut?>> HandleExecution<TIn, TOut>(ILogger logger,
        string callbackName,
        Func<TIn, TOut> callback,
        Func<(TIn Input, Exception Exception), CancellationToken, Task> errorHandler,
        CancellationToken cancellationToken = default)
        => [StackTraceHidden] async (input) =>
        {
            using Activity? activity = Telemetry.DefaultInstance.StartActivity(callbackName, ActivityKind.Internal);
            return await ExecuteAsync(logger, activity, input, callbackName, callback, errorHandler, cancellationToken).ConfigureAwait(false);
        };

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

    public static async Task<TOut?> ExecuteAsync<TIn, TOut>(
        ILogger logger,
        Activity? activity,
        TIn input,
        string callbackName,
        Func<TIn, CancellationToken, Task<TOut>> callback,
        Func<(TIn Input, Exception Exception), CancellationToken, Task> errorHandler,
        CancellationToken cancellationToken = default)
    {
        DateTimeOffset startTime = StartTelemetry(logger, activity, callbackName);

        try
        {
            TOut? result = await callback(input, cancellationToken).ConfigureAwait(false);
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















    public static Func<TIn, TOut> HandleExecution<TIn, TOut>(ILogger logger, string callbackName, Func<TIn, TOut> callback, CancellationToken cancellationToken = default) => [StackTraceHidden] (input) =>
    {
        using Activity? activity = Telemetry.DefaultInstance.StartActivity(callbackName, ActivityKind.Internal);
        return Execute(logger, activity, input, callbackName, callback, cancellationToken);
    };

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

    public static Func<TIn, Task<TOut?>> HandleExecution<TIn, TOut>(
        ILogger logger,
        string callbackName,
        Func<TIn, TOut> callback,
        Func<(TIn Input, Exception Exception), Task> errorHandler,
        CancellationToken cancellationToken = default)
        => [StackTraceHidden] async (input) =>
        {
            using Activity? activity = Telemetry.DefaultInstance.StartActivity(callbackName, ActivityKind.Internal);
            return await ExecuteAsync(logger, activity, input, callbackName, callback, errorHandler, cancellationToken).ConfigureAwait(false);
        };

    public static async Task<TOut?> ExecuteAsync<TIn, TOut>(
        ILogger logger,
        Activity? activity,
        TIn input,
        string callbackName,
        Func<TIn, TOut> callback,
        Func<(TIn Input, Exception Exception), Task> errorHandler,
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

    public static Func<TIn, TOut> HandleExecution<TIn, TOut>(
        ILogger logger,
        string callbackName,
        string fallbackName,
        Func<TIn, TOut> callback,
        Func<TOut, bool> fallbackCondition,
        Func<TIn, TOut> fallback,
        Func<(TIn Input, Exception Exception), TOut> errorHandler,
        CancellationToken cancellationToken = default)
        => [StackTraceHidden] (input) =>
        {
            using Activity? activity = Telemetry.DefaultInstance.StartActivity(callbackName, ActivityKind.Internal);
            return Execute(logger, activity, input, callbackName, fallbackName, callback, fallbackCondition, fallback, errorHandler, cancellationToken);
        };

    public static TOut Execute<TIn, TOut>(
        ILogger logger,
        Activity? activity,
        TIn input,
        string callbackName,
        string fallbackName,
        Func<TIn, TOut> callback,
        Func<TOut, bool> fallbackCondition,
        Func<TIn, TOut> fallback,
        Func<(TIn Input, Exception Exception), TOut> errorHandler,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        DateTimeOffset startTime = StartTelemetry(logger, activity, callbackName);

        try
        {
            TOut? result = callback(input);
            if (fallbackCondition(result))
            {
                LogAndTrackStepFailure(logger, activity, startTime, callbackName, new Exception($"Fallback condition met for '{callbackName}'."));
                return Execute(logger, activity, input, fallbackName, fallback, errorHandler, cancellationToken);
            }

            LogAndTrackStepCompletion(logger, activity, startTime, callbackName);
            return result;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogAndTrackStepFailure(logger, activity, startTime, callbackName, ex);
            return Execute(logger, activity, (input, ex), $"{callbackName}.ErrorHandler", errorHandler, cancellationToken);
        }
    }












    public static Action<TIn> HandleExecution<TIn>(
        ILogger logger,
        string callbackName,
        string fallbackName,
        Action<TIn> callback,
        Action<FailedPayload<TIn>> fallback,
        CancellationToken cancellationToken = default) => [StackTraceHidden] (input) =>
        {
            using Activity? activity = Telemetry.DefaultInstance.StartActivity(callbackName, ActivityKind.Internal);
            Execute(logger, activity, input, callbackName, callback, fallbackName, fallback, cancellationToken);
        };

    public static void Execute<TIn>(
        ILogger logger,
        Activity? activity,
        TIn input,
        string callbackName,
        Action<TIn> callback,
        string? fallbackName = null,
        Action<FailedPayload<TIn>>? fallback = null,
        CancellationToken cancellationToken = default)
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

            if (fallback is null)
                throw;

            FailedPayload<TIn> failedPayload = new(input, ex, callbackName);
            fallbackName ??= $"{callbackName}.Fallback";
            Execute(logger, activity, failedPayload, fallbackName, fallback, cancellationToken: cancellationToken);
        }
    }

    public static Func<TIn, Task> HandleExecution<TIn>(
        ILogger logger,
        string callbackName,
        Func<TIn, CancellationToken, Task> callback,
        string? fallbackName = null,
        Func<FailedPayload<TIn>, CancellationToken, Task>? fallback = null,
        CancellationToken cancellationToken = default) => [StackTraceHidden] async (input) =>
    {
        using Activity? activity = Telemetry.DefaultInstance.StartActivity(callbackName, ActivityKind.Internal);
        await ExecuteAsync(logger, activity, input, callbackName, callback, fallbackName, fallback, cancellationToken).ConfigureAwait(false);
    };

    public static async Task ExecuteAsync<TIn>(
        ILogger logger,
        Activity? activity,
        TIn input,
        string callbackName,
        Func<TIn, CancellationToken, Task> callback,
        string? fallbackName = null,
        Func<FailedPayload<TIn>, CancellationToken, Task>? fallback = null,
        CancellationToken cancellationToken = default)
    {
        DateTimeOffset startTime = StartTelemetry(logger, activity, callbackName);

        try
        {
            LogAndTrackStepCompletion(logger, activity, startTime, callbackName);
            await callback(input, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogAndTrackStepFailure(logger, activity, startTime, callbackName, ex);

            if (fallback is null)
                throw;

            FailedPayload<TIn> failedPayload = new(input, ex, callbackName);
            fallbackName ??= $"{callbackName}.Fallback";
            await ExecuteAsync(logger, activity, failedPayload, fallbackName, fallback, cancellationToken: cancellationToken).ConfigureAwait(false);
        }
    }

    public static Func<TIn, Task<TOut?>> HandleExecution<TIn, TOut>(
        ILogger logger,
        string callbackName,
        Func<TIn, CancellationToken, Task<TOut>> callback,
        string? fallbackName = null,
        Func<FailedPayload<TIn>, CancellationToken, Task>? fallback = null,
        CancellationToken cancellationToken = default)
        => [StackTraceHidden] async (input) =>
        {
            using Activity? activity = Telemetry.DefaultInstance.StartActivity(callbackName, ActivityKind.Internal);

            if (fallback is null)
                return await InternalExecuteAsync(logger, activity, input, callbackName, callback, cancellationToken);

            async Task FallbackAsync(FailedPayload<TIn> failedPayload, CancellationToken ct) => await fallback(failedPayload, ct).ConfigureAwait(false);
            return await ExecuteAsync(logger, activity, input, callbackName, callback, fallbackName, FallbackAsync, cancellationToken).ConfigureAwait(false);
        };

    public static async Task<TOut> ExecuteAsync<TIn, TOut>(
        ILogger logger,
        Activity? activity,
        TIn input,
        string callbackName,
        Func<TIn, CancellationToken, Task<TOut>> callback,
        string? fallbackName = null,
        Func<FailedPayload<TIn>, CancellationToken, Task<TOut>>? fallback = null,
        CancellationToken cancellationToken = default)
    {
        DateTimeOffset startTime = StartTelemetry(logger, activity, callbackName);

        try
        {
            TOut? output = await callback(input, cancellationToken).ConfigureAwait(false);
            LogAndTrackStepCompletion(logger, activity, startTime, callbackName);
            return output;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogAndTrackStepFailure(logger, activity, startTime, callbackName, ex);

            if (fallback is null)
                throw;

            FailedPayload<TIn> failedPayload = new(input, ex, callbackName);
            fallbackName ??= $"{callbackName}.Fallback";
            return await InternalExecuteAsync(logger, activity, failedPayload, fallbackName, fallback, cancellationToken: cancellationToken).ConfigureAwait(false);
        }
    }

    public static async Task<TOut?> ExecuteAsync<TIn, TOut>(
        ILogger logger,
        Activity? activity,
        TIn input,
        string callbackName,
        Func<TIn, CancellationToken, Task<TOut>> callback,
        string? fallbackName = null,
        Func<FailedPayload<TIn>, CancellationToken, Task>? fallback = null,
        CancellationToken cancellationToken = default)
    {
        DateTimeOffset startTime = StartTelemetry(logger, activity, callbackName);

        try
        {
            TOut? output = await callback(input, cancellationToken).ConfigureAwait(false);
            LogAndTrackStepCompletion(logger, activity, startTime, callbackName);
            return output;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogAndTrackStepFailure(logger, activity, startTime, callbackName, ex);

            if (fallback is null)
                throw;

            FailedPayload<TIn> failedPayload = new(input, ex, callbackName);
            fallbackName ??= $"{callbackName}.Fallback";
            await ExecuteAsync(logger, activity, failedPayload, fallbackName, fallback, cancellationToken: cancellationToken).ConfigureAwait(false);
            return default;
        }
    }

    public static TOut? Execute<TIn, TOut>(
        ILogger logger,
        Activity? activity,
        TIn input,
        string callbackName,
        Func<TIn, TOut> callback,
        string? fallbackName = null,
        Action<FailedPayload<TIn>>? fallback = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        DateTimeOffset startTime = StartTelemetry(logger, activity, callbackName);

        try
        {
            TOut? output = callback(input);
            LogAndTrackStepCompletion(logger, activity, startTime, callbackName);
            return output;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogAndTrackStepFailure(logger, activity, startTime, callbackName, ex);

            if (fallback is null)
                throw;

            FailedPayload<TIn> failedPayload = new(input, ex, callbackName);
            fallbackName ??= $"{callbackName}.Fallback";
            Execute(logger, activity, failedPayload, fallbackName, fallback, cancellationToken: cancellationToken);
            return default;
        }
    }

    private static async Task<TOut> InternalExecuteAsync<TIn, TOut>(
        ILogger logger,
        Activity? activity,
        TIn input,
        string name,
        Func<TIn, CancellationToken, Task<TOut>> callback,
        CancellationToken cancellationToken = default)
    {
        DateTimeOffset startTime = StartTelemetry(logger, activity, name);

        try
        {
            TOut? output = await callback(input, cancellationToken).ConfigureAwait(false);
            LogAndTrackStepCompletion(logger, activity, startTime, name);
            return output;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogAndTrackStepFailure(logger, activity, startTime, name, ex);
            throw;
        }
    }

    private static DateTimeOffset StartTelemetry(ILogger logger, Activity? activity, string name)
    {
        activity?.SetTag("execution.name", name);

        var startTime = DateTimeOffset.UtcNow;
        logger.LogInformation("{StartTime:O} - Starting '{Name}'.", startTime, name);
        return startTime;
    }

    private static void LogAndTrackStepCompletion(ILogger logger, Activity? activity, DateTimeOffset startTime, string name)
    {
        activity?.SetStatus(ActivityStatusCode.Ok);

        var endTime = DateTimeOffset.UtcNow;
        TimeSpan duration = endTime - startTime;
        logger.LogInformation("{EndTime:O} - Duration: {Duration} - Successfully completed '{Name}'.", endTime, duration, name);
    }

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