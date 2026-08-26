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
        await ExecuteAsync(logger, activity, input, name, callback, cancellationToken).ConfigureAwait(false);
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
            string callbackName = $"Pipeline.Step.{stepName}";
            using Activity? activity = Telemetry.DefaultInstance.StartActivity(callbackName, ActivityKind.Internal);

            if (deadLetterQueueFunc is null)
                return await InternalExecuteAsync(logger, activity, input, callbackName, stepFunc, cancellationToken);

            string fallbackName = $"Pipeline.Step.{deadLetterQueueName}";
            async Task FallbackAsync(FailedPayload<TIn> failedPayload, CancellationToken ct) => await deadLetterQueueFunc(failedPayload, ct).ConfigureAwait(false);
            return await ExecuteAsync(logger, activity, input, callbackName, stepFunc, fallbackName, FallbackAsync, cancellationToken).ConfigureAwait(false);
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
            string callbackName = $"Pipeline.Step.{stepName}";
            string fallbackName = $"Pipeline.Step.{recoveryStepName}";

            using Activity? activity = Telemetry.DefaultInstance.StartActivity(callbackName, ActivityKind.Internal);

            return await ExecuteAsync<TIn, (TOut? Result, FailedPayload<TIn>? Failure, bool IsSuccess)>(
                logger, activity, input, callbackName,
                async (input, ct) =>
                {
                    TOut output = await primaryStepFunc(input, ct).ConfigureAwait(false);
                    return (output, default, true);
                },
                fallbackName,
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
        string callbackName,
        Func<TIn, CancellationToken, Task<TOut>> callback,
        string? fallbackName = null,
        Func<FailedPayload<TIn>, CancellationToken, Task<TOut>>? fallback = null,
        CancellationToken cancellationToken = default)
    {
        DateTimeOffset startTime = StartTelemetry(logger, activity, callbackName, input);

        try
        {
            TOut? output = await callback(input, cancellationToken).ConfigureAwait(false);
            LogAndTrackStepCompletion(logger, activity, startTime, callbackName, input, output);
            return output;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogAndTrackStepFailure(logger, activity, startTime, callbackName, input, ex);


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
        DateTimeOffset startTime = StartTelemetry(logger, activity, callbackName, input);

        try
        {
            TOut? output = await callback(input, cancellationToken).ConfigureAwait(false);
            LogAndTrackStepCompletion(logger, activity, startTime, callbackName, input, output);
            return output;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogAndTrackStepFailure(logger, activity, startTime, callbackName, input, ex);

            if (fallback is null)
                throw;

            FailedPayload<TIn> failedPayload = new(input, ex, callbackName);
            fallbackName ??= $"{callbackName}.Fallback";
            await ExecuteAsync(logger, activity, failedPayload, fallbackName, fallback, cancellationToken: cancellationToken).ConfigureAwait(false);
            return default;
        }
    }

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
        DateTimeOffset startTime = StartTelemetry(logger, activity, callbackName, input);

        try
        {
            LogAndTrackStepCompletion(logger, activity, startTime, callbackName, input);
            await callback(input, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogAndTrackStepFailure(logger, activity, startTime, callbackName, input, ex);

            if (fallback is null)
                throw;

            FailedPayload<TIn> failedPayload = new(input, ex, callbackName);
            fallbackName ??= $"{callbackName}.Fallback";
            await ExecuteAsync(logger, activity, failedPayload, fallbackName, fallback, cancellationToken: cancellationToken).ConfigureAwait(false);
        }
    }

    public static TOut Execute<TIn, TOut>(
        ILogger logger,
        Activity? activity,
        TIn input,
        string callbackName,
        Func<TIn, TOut> callback,
        string? fallbackName = null,
        Func<FailedPayload<TIn>, TOut>? fallback = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        DateTimeOffset startTime = StartTelemetry(logger, activity, callbackName, input);

        try
        {
            TOut? result = callback(input);
            LogAndTrackStepCompletion(logger, activity, startTime, callbackName, input, result);
            return result;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogAndTrackStepFailure(logger, activity, startTime, callbackName, input, ex);

            if (fallback is null)
                throw;

            FailedPayload<TIn> failedPayload = new(input, ex, callbackName);
            fallbackName ??= $"{callbackName}.Fallback";
            return Execute<FailedPayload<TIn>, TOut>(logger, activity, failedPayload, fallbackName, fallback, cancellationToken: cancellationToken);
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

        DateTimeOffset startTime = StartTelemetry(logger, activity, callbackName, input);

        try
        {
            TOut? result = callback(input);
            LogAndTrackStepCompletion(logger, activity, startTime, callbackName, input, result);
            return result;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogAndTrackStepFailure(logger, activity, startTime, callbackName, input, ex);

            if (fallback is null)
                throw;

            FailedPayload<TIn> failedPayload = new(input, ex, callbackName);
            fallbackName ??= $"{callbackName}.Fallback";
            Execute(logger, activity, failedPayload, fallbackName, fallback, cancellationToken: cancellationToken);
            return default;
        }
    }

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

        DateTimeOffset startTime = StartTelemetry(logger, activity, callbackName, input);

        try
        {
            LogAndTrackStepCompletion(logger, activity, startTime, callbackName, input);
            callback(input);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogAndTrackStepFailure(logger, activity, startTime, callbackName, input, ex);

            if (fallback is null)
                throw;

            FailedPayload<TIn> failedPayload = new(input, ex, callbackName);
            fallbackName ??= $"{callbackName}.Fallback";
            Execute(logger, activity, failedPayload, fallbackName, fallback, cancellationToken: cancellationToken);
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