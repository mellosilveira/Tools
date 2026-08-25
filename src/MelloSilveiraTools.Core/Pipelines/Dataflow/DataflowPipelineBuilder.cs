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
    ITargetBlock<FailedPayload<object>>? deadLetterQueueBlock,
    CancellationToken pipelineCancellationToken)
    : IDataflowPipelineBuilder<THead, TTail>
{
    /// <inheritdoc/>
    public IDataflowPipelineBuilder<THead, TTail> WithDeadLetterQueue(ITargetBlock<FailedPayload<object>> deadLetterQueueSink)
    {
        ArgumentNullException.ThrowIfNull(deadLetterQueueSink);
        return new DataflowPipelineBuilder<THead, TTail>(logger, headBlock, tailBlock, deadLetterQueueSink, pipelineCancellationToken);
    }

    /// <inheritdoc/>
    public IDataflowPipelineBuilder<THead, TTail> WithDeadLetterQueue(Action<FailedPayload<object>> errorHandler, PipelineStepOptions options = default)
    {
        ActionBlock<FailedPayload<object>> actionBlock = new(
            TelemetryExtensions.HandleExecution(logger, "DeadLetterQueue", errorHandler, pipelineCancellationToken),
            options.ToDataflowOptions(pipelineCancellationToken));

        return WithDeadLetterQueue(actionBlock);
    }

    /// <inheritdoc/>
    public IDataflowPipelineBuilder<THead, TTail> WithDeadLetterQueue(Func<FailedPayload<object>, CancellationToken, Task> errorHandlerAsync, PipelineStepOptions options = default)
    {
        ActionBlock<FailedPayload<object>> actionBlock = new(
            TelemetryExtensions.HandleStepExecution(logger, "DeadLetterQueue", errorHandlerAsync, pipelineCancellationToken),
            options.ToDataflowOptions(pipelineCancellationToken));

        return WithDeadLetterQueue(actionBlock);
    }

    /// <inheritdoc/>
    public IDataflowPipelineBuilder<THead, TNextOut> AddStep<TNextOut>(string stepName, Func<TTail, CancellationToken, Task<TNextOut>> stepFunc, PipelineStepOptions options = default)
    {
        TransformBlock<TTail, TNextOut?> nextBlock = new(
            TelemetryExtensions.HandleStepExecution(logger, stepName, "DeadLetterQueue", stepFunc, deadLetterQueueBlock.SendAsync, pipelineCancellationToken),
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
            TelemetryExtensions.HandleStepExecution(logger, primaryStepName, recoveryStepName, primaryFunc, recoveryFunc, pipelineCancellationToken),
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
    public IDataflowPipelineBuilder<THead, TNextOut> AddDataMapping<TNextOut>(string stepName, Func<TTail, TNextOut> mapFunc, PipelineStepOptions options = default)
    {
        Func<TTail, Task<TNextOut?>> safeExecution = ConvertToSafeExecution(stepName, mapFunc, pipelineCancellationToken);
        TransformBlock<TTail, TNextOut?> nextBlock = new(safeExecution, options.ToDataflowOptions(pipelineCancellationToken));
        tailBlock.LinkTo(nextBlock, ignoreNulls: true);

        return new DataflowPipelineBuilder<THead, TNextOut>(logger, headBlock, nextBlock!, deadLetterQueueBlock, pipelineCancellationToken);
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
