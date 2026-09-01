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
    private const string DataMappingTelemetryName = "Pipeline.DataMapping";

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
            TelemetryExtensions.HandleExecution(logger, DeadLetterQueueTelemetryName, errorHandlerAsync, pipelineCancellationToken),
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
    public IDataflowPipelineBuilder<THead, TNextOut> AddDataMapping<TNextOut>(Func<TTail, TNextOut> mapFunc, PipelineStepOptions options = default)
    {
        TransformBlock<TTail, TNextOut?> nextBlock = deadLetterQueueBlock is null
            ? new(
                TelemetryExtensions.HandleExecution(logger, DataMappingTelemetryName, mapFunc, pipelineCancellationToken),
                options.ToDataflowOptions(pipelineCancellationToken))
            : new(
                TelemetryExtensions.HandleExecution(logger, DataMappingTelemetryName, mapFunc, GetDeadLetterQueueSender(logger, deadLetterQueueBlock, DataMappingTelemetryName)!, pipelineCancellationToken),
                options.ToDataflowOptions(pipelineCancellationToken));

        tailBlock.LinkTo(nextBlock, ignoreNulls: true);
        return new DataflowPipelineBuilder<THead, TNextOut>(logger, headBlock, nextBlock!, deadLetterQueueBlock, pipelineCancellationToken);
    }

    /// <inheritdoc/>
    public IDataflowPipelineBuilder<THead, TNextOut> AddDataMapping<TNextOut>(Func<TTail, CancellationToken, Task<TNextOut>> mapFunc, PipelineStepOptions options = default)
    {
        TransformBlock<TTail, TNextOut?> nextBlock = new(
            TelemetryExtensions.HandleExecution(logger, DataMappingTelemetryName, mapFunc, GetDeadLetterQueueSender(logger, deadLetterQueueBlock, DataMappingTelemetryName), pipelineCancellationToken),
            options.ToDataflowOptions(pipelineCancellationToken));

        tailBlock.LinkTo(nextBlock, ignoreNulls: true);
        return new DataflowPipelineBuilder<THead, TNextOut>(logger, headBlock, nextBlock!, deadLetterQueueBlock, pipelineCancellationToken);
    }

    /// <inheritdoc/>
    public IDataflowPipelineBuilder<THead, TNextOut> AddStep<TNextOut>(string stepName, Func<TTail, CancellationToken, Task<TNextOut>> stepFunc, PipelineStepOptions options = default)
    {
        TransformBlock<TTail, TNextOut?> nextBlock = new(
            TelemetryExtensions.HandleExecution(logger, GetTelemetryName(stepName), stepFunc, GetDeadLetterQueueSender(logger, deadLetterQueueBlock, stepName), pipelineCancellationToken),
            options.ToDataflowOptions(pipelineCancellationToken));

        tailBlock.LinkTo(nextBlock, ignoreNulls: true);
        return new DataflowPipelineBuilder<THead, TNextOut>(logger, headBlock, nextBlock!, deadLetterQueueBlock, pipelineCancellationToken);
    }

    /// <inheritdoc/>
    public IDataflowPipelineBuilder<THead, TNextOut> AddForkingStep<TNextOut>(
        string stepName,
        string fallbackStepName,
        Func<TTail, CancellationToken, Task<TNextOut>> stepFunc,
        Func<TTail, bool> fallbackCondition,
        Func<TTail, CancellationToken, Task<TNextOut>> fallbackStep,
        PipelineStepOptions options = default)
    {
        TransformBlock<TTail, TNextOut?> nextBlock = new(
            TelemetryExtensions.HandleExecution(logger, GetTelemetryName(stepName), GetTelemetryName(fallbackStepName), stepFunc, fallbackCondition, fallbackStep, GetDeadLetterQueueSender(logger, deadLetterQueueBlock, stepName), pipelineCancellationToken),
            options.ToDataflowOptions(pipelineCancellationToken));

        tailBlock.LinkTo(nextBlock, ignoreNulls: true);
        return new DataflowPipelineBuilder<THead, TNextOut>(logger, headBlock, nextBlock!, deadLetterQueueBlock, pipelineCancellationToken);
    }

    /// <inheritdoc/>
    public IDataflowPipelineBuilder<THead, TNextOut> AddForkingStep<TNextOut>(
        string stepName,
        string fallbackStepName,
        Func<TTail, CancellationToken, Task<TNextOut>> stepFunc,
        Func<TNextOut, bool> fallbackCondition,
        Func<TTail, CancellationToken, Task<TNextOut>> fallbackStep,
        PipelineStepOptions options = default)
    {
        TransformBlock<TTail, TNextOut?> nextBlock = new(
            TelemetryExtensions.HandleExecution(logger, GetTelemetryName(stepName), GetTelemetryName(fallbackStepName), stepFunc, fallbackCondition, fallbackStep, GetDeadLetterQueueSender(logger, deadLetterQueueBlock, stepName), pipelineCancellationToken),
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

    private static Func<(TTail Input, Exception Exception), CancellationToken, Task>? GetDeadLetterQueueSender(ILogger logger, ITargetBlock<FailedPayload<object?>>? deadLetterQueueBlock, string stepName)
        => deadLetterQueueBlock is null ? null : async (tuple, cancellationToken) =>
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
