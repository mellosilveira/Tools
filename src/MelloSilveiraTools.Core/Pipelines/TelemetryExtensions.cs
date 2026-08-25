using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace MelloSilveiraTools.Core.Pipelines;

/// <summary>
/// Provides structured logging wrappers, ensuring consistent tracking of execution lifecycles, durations, and failures.
/// </summary>
public static class TelemetryExtensions
{
    public static Action<TIn> HandleExecution<TIn>(ILogger logger, string name, Action<TIn> callback, CancellationToken cancellationToken = default) => [StackTraceHidden] (input) =>
    {
        using Activity? activity = Telemetry.DefaultInstance.StartActivity(name, ActivityKind.Internal);
        Execute(logger, activity, input, name, callback, cancellationToken);
    };

    public static Func<TIn, Task> HandleExecution<TIn>(ILogger logger, string name, Func<TIn, Task> callback, CancellationToken cancellationToken = default) => [StackTraceHidden] async (input) =>
    {
        using Activity? activity = Telemetry.DefaultInstance.StartActivity(name, ActivityKind.Internal);
        await ExecuteAsync(logger, activity, input, name, callback, cancellationToken).Con;
    };

    public static Func<TIn, Task> HandleExecution<TIn>(ILogger logger, string name, Func<TIn, CancellationToken, Task> callback, CancellationToken cancellationToken = default) => [StackTraceHidden] async (input) =>
    {
        using Activity? activity = Telemetry.DefaultInstance.StartActivity(name, ActivityKind.Internal);
        await ExecuteAsync(logger, activity, input, name, callback, cancellationToken).ConfigureAwait(false);
    };

    public static Func<TIn, Task<TOut?>> HandleStepExecution<TIn, TOut>(
        ILogger logger,
        string stepName,
        string deadLetterQueueName,
        Func<TIn, CancellationToken, Task<TOut>> stepFunc,
        Func<FailedPayload<object>, CancellationToken, Task>? deadLetterQueueFunc,
        CancellationToken cancellationToken = default)
        => [StackTraceHidden] async (input) =>
        {
            string name = $"Pipeline.Step.{stepName}";
            using Activity? activity = Telemetry.DefaultInstance.StartActivity(name, ActivityKind.Internal);

            if (deadLetterQueueFunc is null)
                return await ExecuteAsync(logger, activity, input, name, stepFunc, cancellationToken);

            string fallbackName = $"Pipeline.Step.{deadLetterQueueName}";
            async Task FallbackAsync(FailedPayload<TIn> failedPayload, CancellationToken ct) => await deadLetterQueueFunc(failedPayload, ct).ConfigureAwait(false);
            return await ExecuteAsync(logger, activity, input, name, fallbackName, stepFunc, FallbackAsync, cancellationToken).ConfigureAwait(false);
        };

    public static Func<TIn, Task<(TOut? Result, FailedPayload<TIn>? Failure, bool IsSuccess)>> HandleStepExecution<TIn, TOut>(
        ILogger logger,
        string stepName,
        string recoveryStepName,
        Func<TIn, CancellationToken, Task<TOut>> primaryStepFunc,
        Func<FailedPayload<TIn>, CancellationToken, Task> recoveryStepFunc,
        CancellationToken cancellationToken = default)
        => [StackTraceHidden] async (input) =>
        {
            string name = $"Pipeline.Step.{stepName}";
            string fallbackName = $"Pipeline.Step.{recoveryStepName}";

            using Activity? activity = Telemetry.DefaultInstance.StartActivity(name, ActivityKind.Internal);

            return await ExecuteAsync<TIn, (TOut? Result, FailedPayload<TIn>? Failure, bool IsSuccess)>(
                logger, activity, input, name, fallbackName,
                async (input, ct) =>
                {
                    TOut output = await primaryStepFunc(input, ct).ConfigureAwait(false);
                    return (output, default, true);
                },
                async (failedPayload, ct) =>
                {
                    await recoveryStepFunc(failedPayload, ct).ConfigureAwait(false);
                    return (default, failedPayload, false);
                },
                cancellationToken
            ).ConfigureAwait(false);
        };

    public static async Task<TOut> ExecuteAsync<TIn, TOut>(
        ILogger logger,
        Activity? activity,
        TIn input,
        string name,
        Func<TIn, CancellationToken, Task<TOut>> callback,
        CancellationToken cancellationToken = default)
    {
        DateTimeOffset startTime = StartTelemetry(logger, activity, name, input);

        try
        {
            TOut? output = await callback(input, cancellationToken).ConfigureAwait(false);
            LogAndTrackStepCompletion(logger, activity, startTime, name, input, output);
            return output;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogAndTrackStepFailure(logger, activity, startTime, name, input, ex);
            throw;
        }
    }

    public static async Task<TOut> ExecuteAsync<TIn, TOut>(
        ILogger logger,
        Activity? activity,
        TIn input,
        string name,
        string fallbackName,
        Func<TIn, CancellationToken, Task<TOut>> callback,
        Func<FailedPayload<TIn>, CancellationToken, Task<TOut>> fallback,
        CancellationToken cancellationToken = default)
    {
        DateTimeOffset startTime = StartTelemetry(logger, activity, name, input);

        try
        {
            TOut? output = await callback(input, cancellationToken).ConfigureAwait(false);
            LogAndTrackStepCompletion(logger, activity, startTime, name, input, output);
            return output;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogAndTrackStepFailure(logger, activity, startTime, name, input, ex);

            FailedPayload<TIn> failedPayload = new(input, ex, name);
            return await ExecuteAsync(logger, activity, failedPayload, fallbackName, fallback, cancellationToken).ConfigureAwait(false);
        }
    }

    public static async Task<TOut?> ExecuteAsync<TIn, TOut>(
        ILogger logger,
        Activity? activity,
        TIn input,
        string name,
        string fallbackName,
        Func<TIn, CancellationToken, Task<TOut>> callback,
        Func<FailedPayload<TIn>, CancellationToken, Task> fallback,
        CancellationToken cancellationToken = default)
    {
        DateTimeOffset startTime = StartTelemetry(logger, activity, name, input);

        try
        {
            TOut? output = await callback(input, cancellationToken).ConfigureAwait(false);
            LogAndTrackStepCompletion(logger, activity, startTime, name, input, output);
            return output;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogAndTrackStepFailure(logger, activity, startTime, name, input, ex);

            FailedPayload<TIn> failedPayload = new(input, ex, name);
            await ExecuteAsync(logger, activity, failedPayload, fallbackName, fallback, cancellationToken).ConfigureAwait(false);
            return default;
        }
    }

    public static async Task ExecuteAsync<TIn>(ILogger logger, Activity? activity, TIn input, string name, Func<TIn, CancellationToken, Task> callback, CancellationToken cancellationToken = default)
    {
        DateTimeOffset startTime = StartTelemetry(logger, activity, name, input);

        try
        {
            LogAndTrackStepCompletion(logger, activity, startTime, name, input);
            await callback(input, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogAndTrackStepFailure(logger, activity, startTime, name, input, ex);
            throw;
        }
    }

    public static async Task ExecuteAsync<TIn>(
        ILogger logger,
        Activity? activity,
        TIn input,
        string name,
        string fallbackName,
        Func<TIn, CancellationToken, Task> callback,
        Func<FailedPayload<TIn>, CancellationToken, Task> fallback,
        CancellationToken cancellationToken = default)
    {
        DateTimeOffset startTime = StartTelemetry(logger, activity, name, input);

        try
        {
            LogAndTrackStepCompletion(logger, activity, startTime, name, input);
            await callback(input, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogAndTrackStepFailure(logger, activity, startTime, name, input, ex);
            FailedPayload<TIn> failedPayload = new(input, ex, name);
            await ExecuteAsync(logger, activity, failedPayload, fallbackName, fallback, cancellationToken).ConfigureAwait(false);
        }
    }

    public static TOut Execute<TIn, TOut>(ILogger logger, Activity? activity, TIn input, string name, Func<TIn, TOut> callback, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        DateTimeOffset startTime = StartTelemetry(logger, activity, name, input);

        try
        {
            TOut? result = callback(input);
            LogAndTrackStepCompletion(logger, activity, startTime, name, input, result);
            return result;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogAndTrackStepFailure(logger, activity, startTime, name, input, ex);
            throw;
        }
    }

    public static TOut Execute<TIn, TOut>(
        ILogger logger,
        Activity? activity,
        TIn input,
        string name,
        string fallbackName,
        Func<TIn, TOut> callback,
        Func<FailedPayload<TIn>, TOut> fallback,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        DateTimeOffset startTime = StartTelemetry(logger, activity, name, input);

        try
        {
            TOut? result = callback(input);
            LogAndTrackStepCompletion(logger, activity, startTime, name, input, result);
            return result;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogAndTrackStepFailure(logger, activity, startTime, name, input, ex);

            FailedPayload<TIn> failedPayload = new(input, ex, name);
            return Execute(logger, activity, failedPayload, fallbackName, fallback, cancellationToken);
        }
    }

    public static void Execute<TIn>(ILogger logger, Activity? activity, TIn input, string name, Action<TIn> callback, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        DateTimeOffset startTime = StartTelemetry(logger, activity, name, input);

        try
        {
            LogAndTrackStepCompletion(logger, activity, startTime, name, input);
            callback(input);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogAndTrackStepFailure(logger, activity, startTime, name, input, ex);
            throw;
        }
    }

    public static void Execute<TIn>(
        ILogger logger,
        Activity? activity,
        TIn input,
        string name,
        string fallbackName,
        Action<TIn> callback,
        Action<FailedPayload<TIn>> fallback,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        DateTimeOffset startTime = StartTelemetry(logger, activity, name, input);

        try
        {
            LogAndTrackStepCompletion(logger, activity, startTime, name, input);
            callback(input);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogAndTrackStepFailure(logger, activity, startTime, name, input, ex);

            FailedPayload<TIn> failedPayload = new(input, ex, name);
            Execute(logger, activity, failedPayload, fallbackName, fallback, cancellationToken);
        }
    }

    private static DateTimeOffset StartTelemetry<TInput>(ILogger logger, Activity? activity, string name, TInput input)
    {
        activity?.SetTag("execution.name", name);

        var startTime = DateTimeOffset.UtcNow;
        logger.LogInformation("{StartTime:O} - Starting '{Name}' with input payload: {@Input}", startTime, name, input);
        return startTime;
    }

    private static void LogAndTrackStepCompletion<TInput>(ILogger logger, Activity? activity, DateTimeOffset startTime, string stepName, TInput input)
    {
        activity?.SetStatus(ActivityStatusCode.Ok);

        var endTime = DateTimeOffset.UtcNow;
        TimeSpan duration = endTime - startTime;
        logger.LogInformation("{EndTime:O} - Duration: {Duration} - Successfully completed '{Name}'. Input payload: {@Input}.", endTime, duration, stepName, input);
    }

    private static void LogAndTrackStepCompletion<TInput, TOutput>(ILogger logger, Activity? activity, DateTimeOffset startTime, string name, TInput input, TOutput output)
    {
        activity?.SetStatus(ActivityStatusCode.Ok);

        var endTime = DateTimeOffset.UtcNow;
        TimeSpan duration = endTime - startTime;
        logger.LogInformation("{EndTime:O} - Duration: {Duration} - Successfully completed '{Name}'. Input payload: {@Input}. Output payload: {@Output}", endTime, duration, name, input, output);
    }

    private static void LogAndTrackStepFailure<TInput>(ILogger logger, Activity? activity, DateTimeOffset startTime, string name, TInput input, Exception ex)
    {
        activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
        // Attach the exception and stack trace to the telemetry span using native API.
        activity?.AddException(ex);

        var endTime = DateTimeOffset.UtcNow;
        TimeSpan duration = endTime - startTime;
        logger.LogError(ex, "{EndTime:O} - Duration: {Duration} - Execution of '{Name}' faulted while processing payload: {@Input}", endTime, duration, name, input);
    }
}