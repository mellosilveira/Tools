using Microsoft.Extensions.Logging;
using System.Threading.Tasks.Dataflow;

namespace MelloSilveiraTools.Core.Pipelines.Dataflow;

/// <summary>
/// A strongly-typed fluent builder for orchestrating TPL Dataflow topologies.
/// </summary>
internal class DataflowPipelineBuilder<THead, TTail>(
    ILogger logger,
    ITargetBlock<THead> headBlock,
    ISourceBlock<TTail> tailBlock,
    ITargetBlock<FailedPayload<object?>>? deadLetterQueueBlock,
    RetryOptions? retryOptions,
    CancellationToken pipelineCancellationToken)
{
    private const string DeadLetterQueueTelemetryName = "Pipeline.DeadLetterQueue";

    public DataflowPipelineBuilder<THead, TTail> WithDeadLetterQueue(ITargetBlock<FailedPayload<object?>> deadLetterQueueSink, RetryOptions? retry = null)
    {
        ArgumentNullException.ThrowIfNull(deadLetterQueueSink);
        return new DataflowPipelineBuilder<THead, TTail>(logger, headBlock, tailBlock, deadLetterQueueSink, retry ?? retryOptions, pipelineCancellationToken);
    }

    public DataflowPipelineBuilder<THead, TNextOut> AddStep<TNextOut>(string stepName, Func<TTail, CancellationToken, Task<TNextOut>> stepFunc, PipelineStepOptions options = default)
    {
        TransformBlock<TTail, TNextOut?> nextBlock = new(
            TelemetryExtensions.HandleExecution(logger, GetTelemetryName(stepName), stepFunc, GetDeadLetterQueueSender(stepName), retryOptions, pipelineCancellationToken),
            options.ToDataflowOptions(pipelineCancellationToken));

        tailBlock.LinkTo(nextBlock, ignoreNulls: deadLetterQueueBlock is not null);
        return new DataflowPipelineBuilder<THead, TNextOut>(logger, headBlock, nextBlock!, deadLetterQueueBlock, retryOptions, pipelineCancellationToken);
    }

    public DataflowPipelineBuilder<THead, TNextOut> AddForkingStep<TNextOut>(
        string stepName,
        string fallbackStepName,
        Func<TTail, CancellationToken, Task<TNextOut>> stepFunc,
        Func<TTail, bool> fallbackCondition,
        Func<TTail, CancellationToken, Task<TNextOut>> fallbackStep,
        PipelineStepOptions options = default)
    {
        TransformBlock<TTail, TNextOut?> nextBlock = new(
            TelemetryExtensions.HandleExecution(logger, GetTelemetryName(stepName), GetTelemetryName(fallbackStepName), stepFunc, fallbackCondition, fallbackStep, GetDeadLetterQueueSender(stepName), retryOptions, pipelineCancellationToken),
            options.ToDataflowOptions(pipelineCancellationToken));

        tailBlock.LinkTo(nextBlock, ignoreNulls: deadLetterQueueBlock is not null);
        return new DataflowPipelineBuilder<THead, TNextOut>(logger, headBlock, nextBlock!, deadLetterQueueBlock, retryOptions, pipelineCancellationToken);
    }

    public DataflowPipelineBuilder<THead, TTail[]> AddBatchStep(int batchSize, PipelineStepOptions options = default)
    {
        BatchBlock<TTail> nextBlock = new(batchSize, new GroupingDataflowBlockOptions
        {
            CancellationToken = pipelineCancellationToken,
            BoundedCapacity = options.MaxBufferSize
        });

        tailBlock.LinkTo(nextBlock);
        return new DataflowPipelineBuilder<THead, TTail[]>(logger, headBlock, nextBlock, deadLetterQueueBlock, retryOptions, pipelineCancellationToken);
    }

    public DataflowPipelineBuilder<THead, TTail> AddBroadcastStep(Func<TTail, TTail> cloneFunc, PipelineStepOptions options = default)
    {
        BroadcastBlock<TTail> nextBlock = new(cloneFunc, options.ToDataflowOptions(pipelineCancellationToken));
        tailBlock.LinkTo(nextBlock);
        return new DataflowPipelineBuilder<THead, TTail>(logger, headBlock, nextBlock, deadLetterQueueBlock, retryOptions, pipelineCancellationToken);
    }

    public IDataflowPipeline<THead> BuildTerminal(string stepName, Func<TTail, CancellationToken, Task> terminalAction, PipelineStepOptions options = default)
    {
        ActionBlock<TTail> terminalBlock = new(
            TelemetryExtensions.HandleExecution(logger, GetTelemetryName(stepName), terminalAction, GetDeadLetterQueueSender(stepName), retryOptions, pipelineCancellationToken),
            options.ToDataflowOptions(pipelineCancellationToken));

        tailBlock.LinkTo(terminalBlock, new DataflowLinkOptions { PropagateCompletion = true });
        return new DataflowPipeline<THead>(headBlock, terminalBlock.Completion, logger);
    }

    private Func<(TTail Input, Exception Exception), CancellationToken, Task>? GetDeadLetterQueueSender(string stepName)
        => deadLetterQueueBlock is null ? null : async (tuple, cancellationToken) =>
        {
            FailedPayload<object?> failedPayload = new(stepName, tuple.Input, tuple.Exception);
            if (!await deadLetterQueueBlock.SendAsync(failedPayload, cancellationToken))
                logger.LogWarning("Failed to route to dead letter queue. Payload: {FailedPayload}", failedPayload);
        };

    private static string GetTelemetryName(string stepName) => $"Pipeline.Step.{stepName}";
}

file sealed class DataflowPipeline<TIn>(ITargetBlock<TIn> headBlock, Task completionTask, ILogger? logger) : IDataflowPipeline<TIn>
{
    public Task<bool> SendAsync(TIn item, CancellationToken cancellationToken = default) => headBlock.SendAsync(item, cancellationToken);

    public void Complete()
    {
        logger?.LogInformation("Pipeline completion invoked. Draining buffered messages and propagating completion state.");
        headBlock.Complete();
    }

    public Task Completion => completionTask;

    public async ValueTask DisposeAsync()
    {
        Complete();
        await Completion.ConfigureAwait(false);
    }
}