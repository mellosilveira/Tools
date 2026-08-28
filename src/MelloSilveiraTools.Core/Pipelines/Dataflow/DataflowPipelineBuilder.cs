using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Threading.Tasks.Dataflow;

namespace MelloSilveiraTools.Core.Pipelines.Dataflow;

/// <summary>
/// A strongly-typed fluent builder for orchestrating TPL Dataflow topologies.
/// Maintains internal block references to guarantee type invariance during pipeline construction.
/// </summary>
internal class DataflowPipelineBuilder<THead, TTail>(
    ILogger logger,
    ITargetBlock<THead> headBlock,
    ISourceBlock<TTail> tailBlock,
    ITargetBlock<FailedPayload<object?>>? deadLetterQueueBlock,
    CancellationToken pipelineCancellationToken)
    : IDataflowPipelineBuilder<THead, TTail>
{
    private const string DeadLetterQueueTelemetryName = "Pipeline.DeadLetterQueue";

    /// <inheritdoc/>
    public IDataflowPipelineBuilder<THead, TTail> WithDeadLetterQueue(ITargetBlock<FailedPayload<object?>> deadLetterQueueSink)
    {
        ArgumentNullException.ThrowIfNull(deadLetterQueueSink);
        return new DataflowPipelineBuilder<THead, TTail>(logger, headBlock, tailBlock, deadLetterQueueSink, pipelineCancellationToken);
    }

    /// <inheritdoc/>
    public IDataflowPipelineBuilder<THead, TTail> WithDeadLetterQueue(Action<FailedPayload<object?>> errorHandler, PipelineStepOptions options = default)
    {
        ActionBlock<FailedPayload<object?>> actionBlock = new(
            TelemetryExtensions.HandleExecution(logger, DeadLetterQueueTelemetryName, errorHandler, pipelineCancellationToken),
            options.ToDataflowOptions(pipelineCancellationToken));

        return WithDeadLetterQueue(actionBlock);
    }

    /// <inheritdoc/>
    public IDataflowPipelineBuilder<THead, TTail> WithDeadLetterQueue(Func<FailedPayload<object?>, CancellationToken, Task> errorHandlerAsync, PipelineStepOptions options = default)
    {
        ActionBlock<FailedPayload<object?>> actionBlock = new(
            TelemetryExtensions.HandleExecutionAsync(logger, DeadLetterQueueTelemetryName, errorHandlerAsync, pipelineCancellationToken),
            options.ToDataflowOptions(pipelineCancellationToken));

        return WithDeadLetterQueue(actionBlock);
    }

    /// <inheritdoc/>
    public IDataflowPipelineBuilder<THead, TTail> WithLoggingErrors(PipelineStepOptions options = default)
    {
        ActionBlock<FailedPayload<object?>> actionBlock = new(
            failedPayload => logger.LogError("Failed to execute step. Failed payload: {FailedPayload}", failedPayload),
            options.ToDataflowOptions(pipelineCancellationToken));

        return WithDeadLetterQueue(actionBlock);
    }

    /// <inheritdoc/>
    public IDataflowPipelineBuilder<THead, TNextOut> AddDataMapping<TNextOut>(string stepName, Func<TTail, TNextOut> mapFunc, PipelineStepOptions options = default)
    {
        TransformBlock<TTail, TNextOut?> nextBlock = deadLetterQueueBlock is null
            ? new(
                TelemetryExtensions.HandleExecution(logger, GetTelemetryName(stepName), mapFunc, pipelineCancellationToken), 
                options.ToDataflowOptions(pipelineCancellationToken))
            : new(
                TelemetryExtensions.HandleExecution(logger, GetTelemetryName(stepName), mapFunc, GetDeadLetterQueueSender(logger, deadLetterQueueBlock, stepName), pipelineCancellationToken),
                options.ToDataflowOptions(pipelineCancellationToken));

        return new DataflowPipelineBuilder<THead, TNextOut>(logger, headBlock, nextBlock!, deadLetterQueueBlock, pipelineCancellationToken);
    }

    /// <inheritdoc/>
    public IDataflowPipelineBuilder<THead, TNextOut> AddStep<TNextOut>(string stepName, Func<TTail, CancellationToken, Task<TNextOut>> stepFunc, PipelineStepOptions options = default)
    {
        TransformBlock<TTail, TNextOut?> nextBlock = new(
            deadLetterQueueBlock is null
                ? TelemetryExtensions.HandleExecution(logger, stepName, stepFunc, pipelineCancellationToken)
                : TelemetryExtensions.HandleExecution(logger, stepName, stepFunc, "DeadLetterQueue", deadLetterQueueBlock.SendAsync, pipelineCancellationToken),
            options.ToDataflowOptions(pipelineCancellationToken));
        tailBlock.LinkTo(nextBlock, ignoreNulls: true);

        return new DataflowPipelineBuilder<THead, TNextOut>(logger, headBlock, nextBlock!, deadLetterQueueBlock, pipelineCancellationToken);
    }





    /// <inheritdoc/>
    public IDataflowPipelineBuilder<THead, TNextOut> AddForkingStep<TNextOut>(
        string primaryStepName,
        string recoveryStepName,
        Func<TTail, CancellationToken, Task<TNextOut>> primaryFunc,
        Func<FailedPayload<TTail>, CancellationToken, Task> recoveryFunc,
        PipelineStepOptions options = default)
    {
        TransformBlock<TTail, (TNextOut? Result, FailedPayload<TTail>? Failure, bool IsSuccess)> splitBlock = new(
            TelemetryExtensions.HandleExecution(logger, primaryStepName, recoveryStepName, primaryFunc, recoveryFunc, pipelineCancellationToken),
            options.ToDataflowOptions(pipelineCancellationToken));

        TransformBlock<(TNextOut? Result, FailedPayload<TTail>? Failure, bool IsSuccess), TNextOut> successTarget = new(
            tuple => tuple.Result!,
            options.ToDataflowOptions(pipelineCancellationToken));

        ActionBlock<(TNextOut? Result, FailedPayload<TTail>? Failure, bool IsSuccess)> failureTarget = new(
            async tuple => await recoveryFunc(tuple.Failure!.Value, pipelineCancellationToken).ConfigureAwait(false),
            options.ToDataflowOptions(pipelineCancellationToken));

        tailBlock.LinkTo(splitBlock);
        splitBlock.LinkTo(successTarget, tuple => tuple.IsSuccess);
        splitBlock.LinkTo(failureTarget, tuple => !tuple.IsSuccess);

        return new DataflowPipelineBuilder<THead, TNextOut>(logger, headBlock, successTarget, null, pipelineCancellationToken);
    }

    /// <inheritdoc/>
    public IDataflowPipelineBuilder<THead, TNextOut> AddDataMapping<TNextOut>(string stepName, Func<TTail, CancellationToken, Task<TNextOut>> mapFunc, PipelineStepOptions options = default)
    {
        TransformBlock<TTail, TNextOut?> nextBlock = new(
            TelemetryExtensions.HandleExecution(logger, GetTelemetryName(stepName), mapFunc, pipelineCancellationToken),
            options.ToDataflowOptions(pipelineCancellationToken));

        tailBlock.LinkTo(nextBlock, ignoreNulls: true);

        return new DataflowPipelineBuilder<THead, TNextOut>(logger, headBlock, nextBlock!, deadLetterQueueBlock, pipelineCancellationToken);
    }

    /// <inheritdoc/>
    public IDataflowPipeline<THead> BuildTerminal(string stepName, Action<TTail> terminalAction, PipelineStepOptions options = default)
    {
        ActionBlock<TTail> terminalBlock = new(TelemetryExtensions.HandleExecution(logger, GetTelemetryName(stepName), terminalAction, pipelineCancellationToken), options.ToDataflowOptions(pipelineCancellationToken));
        tailBlock.LinkTo(terminalBlock);

        return new DataflowPipeline<THead>(headBlock, terminalBlock.Completion, logger);
    }

    /// <inheritdoc/>
    public IDataflowPipeline<THead> BuildTerminal(string stepName, Func<TTail, Task> terminalAction, PipelineStepOptions options = default)
    {
        ActionBlock<TTail> terminalBlock = new(TelemetryExtensions.HandleExecution(logger, GetTelemetryName(stepName), terminalAction, pipelineCancellationToken), options.ToDataflowOptions(pipelineCancellationToken));
        tailBlock.LinkTo(terminalBlock);

        return new DataflowPipeline<THead>(headBlock, terminalBlock.Completion, logger);
    }



    private static Func<TIn, Task<(TOut? Result, FailedPayload<TIn>? Failure, bool IsSuccess)>> HandleExecution<TIn, TOut>(
        ILogger logger,
        string stepName,
        string recoveryStepName,
        Func<TIn, CancellationToken, Task<TOut>> primaryStepFunc,
        Func<TIn, CancellationToken, Task> recoveryStepFunc,
        CancellationToken cancellationToken = default)
        => [StackTraceHidden] async (input) =>
        {
            string callbackName = GetTelemetryName(stepName);
            using Activity? activity = Telemetry.DefaultInstance.StartActivity(callbackName, ActivityKind.Internal);

            return await TelemetryExtensions.ExecuteAsync<TIn, (TOut? Result, FailedPayload<TIn>? Failure, bool IsSuccess)>(
                logger, activity, input, callbackName,
                async (input, ct) =>
                {
                    TOut output = await primaryStepFunc(input, ct).ConfigureAwait(false);
                    return (output, default, true);
                },
                GetTelemetryName(recoveryStepName),
                async (failedPayload, ct) =>
                {
                    await recoveryStepFunc(failedPayload, ct).ConfigureAwait(false);
                    return (default, failedPayload, false);
                },
                cancellationToken
            ).ConfigureAwait(false);
        };

    private static Func<(TTail Input, Exception Exception), CancellationToken, Task> GetDeadLetterQueueSender(ILogger logger, ITargetBlock<FailedPayload<object?>> deadLetterQueueBlock, string stepName)
        => async (tuple, cancellationToken) =>
        {
            FailedPayload<object?> failedPayload = new(stepName, tuple.Input, tuple.Exception);
            if (await deadLetterQueueBlock.SendAsync(failedPayload, cancellationToken))
                logger.LogWarning("Failed to send failed payload to dead letter queue. Failed payload: {FailedPayload}", failedPayload);
        };

    private static string GetTelemetryName(string stepName) => $"Pipeline.Step.{stepName}";
}

/// <summary>
/// The concrete execution engine encapsulating the TPL source/target block linkages.
/// Restricted via file-scoped access and sealed to enable runtime devirtualization optimizations.
/// </summary>
file sealed class DataflowPipeline<TIn>(ITargetBlock<TIn> headBlock, Task completionTask, ILogger? logger) : IDataflowPipeline<TIn>
{
    /// <inheritdoc/>
    public Task<bool> SendAsync(TIn item, CancellationToken cancellationToken = default) => headBlock.SendAsync(item, cancellationToken);

    /// <inheritdoc/>
    public void Complete()
    {
        logger?.LogInformation("Pipeline completion invoked. Draining buffered messages and propagating completion state.");
        headBlock.Complete();
    }

    /// <inheritdoc/>
    public Task Completion => completionTask;

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        Complete();
        await Completion.ConfigureAwait(false);
    }
}
