using MelloSilveiraTools.Core.Pipelines.Dataflow;
using MelloSilveiraTools.Core.Pipelines.Fluent;
using Microsoft.Extensions.Logging;

namespace MelloSilveiraTools.Core.Pipelines;

/// <summary>
/// Encapsulates extension methods for pipeline builders.
/// Abstracts underlying mechanics by providing strongly-typed fluid configuration APIs.
/// </summary>
public static class PipelineExtensions
{
    extension<TInitial, TCurrentOut>(IFluentPipelineBuilder<TInitial, TCurrentOut> builder)
    {
        /// <summary>
        /// Appends an asynchronous execution step to the fluent pipeline topology[cite: 2].
        /// </summary>
        public IFluentPipelineBuilder<TInitial, TNextOut> AddStep<TNextOut>(IPipelineStep<TCurrentOut, TNextOut> step)
        {
            ArgumentNullException.ThrowIfNull(step);
            return builder.AddStep(step.Name, step.ExecuteAsync);
        }

        /// <summary>
        /// Injects a synchronous data transformation projection into the pipeline execution graph[cite: 2].
        /// </summary>
        public IFluentPipelineBuilder<TInitial, TNextOut> AddDataMapping<TNextOut>(Func<TCurrentOut, TNextOut> mapper)
        {
            ArgumentNullException.ThrowIfNull(mapper);
            return builder.AddStep("DataMapping", (input, _) => Task.FromResult(mapper(input)));
        }
    }

    extension<THead, TCurrentOut>(IDataflowPipelineBuilder<THead, TCurrentOut> builder)
    {
        /// <summary>
        /// Injects a pre-configured <see cref="IPipelineStep{TIn, TOut}"/> instance into the continuous Dataflow execution graph.
        /// </summary>
        public IDataflowPipelineBuilder<THead, TNextOut> AddStep<TNextOut>(IPipelineStep<TCurrentOut, TNextOut> step, PipelineStepOptions options = default)
        {
            ArgumentNullException.ThrowIfNull(step);
            return builder.AddStep(step.Name, step.ExecuteAsync, options);
        }
    }

    /// <summary>
    /// Forks the pipeline execution based on success or failure using a lightweight ValueTuple envelope.
    /// Successful items proceed to the next stage, while failed items are routed to a recovery step and terminated.
    /// </summary>
    public static IDataflowPipelineBuilder<THead, TNextOut> AddForkingStep<THead, TCurrentOut, TNextOut>(
        this IDataflowPipelineBuilder<THead, TCurrentOut> builder,
        IPipelineStep<TCurrentOut, TNextOut> primaryStep,
        IPipelineStep<FailedPayload<TCurrentOut>, TNextOut> recoveryStep,
        PipelineStepOptions options = default)
    {
        ArgumentNullException.ThrowIfNull(primaryStep);
        ArgumentNullException.ThrowIfNull(recoveryStep);
        return builder.AddForkingStep(primaryStep.Name, recoveryStep.Name, primaryStep.ExecuteAsync, recoveryStep.ExecuteAsync, options);
    }
}

/// <summary>
/// Provides structured logging wrappers for pipeline steps, ensuring consistent tracking of execution lifecycles, durations, and failures.
/// </summary>
internal static class PipelineLoggingExtensions
{
    public static async Task<TOut> HandleExecutionAsync<TIn, TOut>(
        ILogger logger,
        string stepName, 
        TIn input, 
        Func<TIn, CancellationToken, Task<TOut>> stepFunc,
        Func<FailedPayload<TIn>, CancellationToken, Task<TOut>>? fallback = null,
        CancellationToken cancellationToken = default)
    {
        // Capture exact start timestamp.
        DateTimeOffset startTime = LogStepStart(logger, stepName, input);

        try
        {
            TOut? result = await stepFunc(input, cancellationToken).ConfigureAwait(false);
            LogStepCompletion(logger, stepName, input, result, startTime);
            return result;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogStepFailure(logger, stepName, input, ex, startTime);

            if (fallback is null)
                throw;

            FailedPayload<TIn> failedPayload = new(input, ex, stepName);
            return await fallback(failedPayload, cancellationToken).ConfigureAwait(false);
        }
    }

    public static TOut HandleExecution<TIn, TOut>(
        ILogger logger,
        string stepName,
        TIn input,
        Func<TIn, TOut> stepFunc,
        Func<FailedPayload<TIn>, TOut>? fallback = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Capture exact start timestamp.
        DateTimeOffset startTime = LogStepStart(logger, stepName, input);

        try
        {
            TOut? result = stepFunc(input);
            LogStepCompletion(logger, stepName, input, result, startTime);
            return result;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogStepFailure(logger, stepName, input, ex, startTime);

            if (fallback is null)
                throw;

            FailedPayload<TIn> failedPayload = new(input, ex, stepName);
            return fallback(failedPayload);
        }
    }

    private static DateTimeOffset LogStepStart<TInput>(ILogger logger, string stepName, TInput input)
    {
        var startTime = DateTimeOffset.UtcNow;
        logger.LogInformation("{StartTime:O} - Starting pipeline step '{StepName}' with input payload: {@Input}", startTime, stepName, input);
        return startTime;
    }

    private static void LogStepCompletion<TInput, TOutput>(ILogger logger, string stepName, TInput input, TOutput output, DateTimeOffset startTime)
    {
        var endTime = DateTimeOffset.UtcNow;
        TimeSpan duration = endTime - startTime;
        logger.LogInformation("{EndTime:O} - Duration: {Duration} - Successfully completed pipeline step '{StepName}'. Input payload: {@Input}, Output payload: {@Output}", endTime, duration, stepName, input, output);
    }

    private static void LogStepFailure<TInput>(ILogger logger, string stepName, TInput input, Exception ex, DateTimeOffset startTime)
    {
        var endTime = DateTimeOffset.UtcNow;
        TimeSpan duration = endTime - startTime;
        logger.LogError(ex, "{EndTime:O} - Duration: {Duration} - Pipeline step '{StepName}' faulted while processing payload: {@Input}", endTime, duration, stepName, input);
    }
}