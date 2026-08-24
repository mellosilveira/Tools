using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Threading.Tasks.Dataflow;

namespace MelloSilveiraTools.Core.Pipelines.Dataflow;

/// <summary>
/// A strongly-typed fluent builder for orchestrating TPL Dataflow topologies.
/// Maintains internal block references to guarantee type invariance during pipeline construction.
/// </summary>
internal class DataflowPipelineBuilder<THead, TTail>(
    ITargetBlock<THead> headBlock,
    ISourceBlock<TTail> tailBlock,
    ITargetBlock<FailedPayload<TTail>>? deadLetterQueueBlock,
    ILogger logger,
    CancellationToken pipelineCancellationToken)
    : IDataflowPipelineBuilder<THead, TTail>
{
    /// <inheritdoc/>
    public IDataflowPipelineBuilder<THead, TTail> WithDeadLetterQueue(ITargetBlock<FailedPayload<TTail>> deadLetterQueueSink)
    {
        ArgumentNullException.ThrowIfNull(deadLetterQueueSink);
        return new DataflowPipelineBuilder<THead, TTail>(headBlock, tailBlock, deadLetterQueueSink, logger, pipelineCancellationToken);
    }

    /// <inheritdoc/>
    public IDataflowPipelineBuilder<THead, TTail> WithDeadLetterQueue(Action<FailedPayload<TTail>> errorHandler, PipelineStepOptions options = default)
    {
        ArgumentNullException.ThrowIfNull(errorHandler);
        ActionBlock<FailedPayload<TTail>> actionBlock = new(errorHandler, options.ToDataflowOptions(pipelineCancellationToken));
        return WithDeadLetterQueue(actionBlock);
    }

    /// <inheritdoc/>
    public IDataflowPipelineBuilder<THead, TTail> WithDeadLetterQueue(Func<FailedPayload<TTail>, CancellationToken, Task> errorHandlerAsync, PipelineStepOptions options = default)
    {
        ArgumentNullException.ThrowIfNull(errorHandlerAsync);
        ActionBlock<FailedPayload<TTail>> actionBlock = new(async payload => await errorHandlerAsync(payload, pipelineCancellationToken).ConfigureAwait(false), options.ToDataflowOptions(pipelineCancellationToken));
        return WithDeadLetterQueue(actionBlock);
    }

    /// <inheritdoc/>
    public IDataflowPipelineBuilder<THead, TNextOut> AddStep<TNextOut>(string stepName, Func<TTail, CancellationToken, Task<TNextOut>> stepFunc, PipelineStepOptions options = default)
    {
        Func<TTail, Task<TNextOut?>> safeExecution = ConvertToSafeExecution(stepName, stepFunc, pipelineCancellationToken);
        TransformBlock<TTail, TNextOut?> nextBlock = new(safeExecution, options.ToDataflowOptions(pipelineCancellationToken));
        tailBlock.LinkTo(nextBlock, ignoreNulls: true);

        return new DataflowPipelineBuilder<THead, TNextOut>(headBlock, nextBlock!, deadLetterQueueBlock: null, logger, pipelineCancellationToken);
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

        return new DataflowPipelineBuilder<THead, TNextOut>(headBlock, successTarget, null, logger, pipelineCancellationToken);
    }

    /// <inheritdoc/>
    public IDataflowPipelineBuilder<THead, TNextOut> AddDataMapping<TNextOut>(string stepName, Func<TTail, TNextOut> mapFunc, PipelineStepOptions options = default)
    {
        Func<TTail, Task<TNextOut?>> safeExecution = ConvertToSafeExecution(stepName, mapFunc, pipelineCancellationToken);
        TransformBlock<TTail, TNextOut?> nextBlock = new(safeExecution, options.ToDataflowOptions(pipelineCancellationToken));
        tailBlock.LinkTo(nextBlock, ignoreNulls: true);

        return new DataflowPipelineBuilder<THead, TNextOut>(headBlock, nextBlock!, deadLetterQueueBlock: null, logger, pipelineCancellationToken);
    }

    /// <inheritdoc/>
    public IDataflowPipelineBuilder<THead, TNextOut> AddDataMapping<TNextOut>(string stepName, Func<TTail, CancellationToken, Task<TNextOut>> mapFunc, PipelineStepOptions options = default)
    {
        Func<TTail, Task<TNextOut?>> safeExecution = ConvertToSafeExecution(stepName, mapFunc, pipelineCancellationToken);
        TransformBlock<TTail, TNextOut?> nextBlock = new(safeExecution, options.ToDataflowOptions(pipelineCancellationToken));
        tailBlock.LinkTo(nextBlock, ignoreNulls: true);

        return new DataflowPipelineBuilder<THead, TNextOut>(headBlock, nextBlock!, deadLetterQueueBlock: null, logger, pipelineCancellationToken);
    }

    /// <inheritdoc/>
    public IDataflowPipeline<THead> BuildTerminal(string stepName, Action<TTail> terminalAction, PipelineStepOptions options = default)
    {
        Action<TTail> safeExecution = ConvertToSafeExecution(stepName, terminalAction, pipelineCancellationToken);
        ActionBlock<TTail> terminalBlock = new(terminalAction, options.ToDataflowOptions(pipelineCancellationToken));
        tailBlock.LinkTo(terminalBlock);

        return new DataflowPipeline<THead>(headBlock, terminalBlock.Completion, logger);
    }

    private Func<TTail, Task<TNextOut?>> ConvertToSafeExecution<TNextOut>(string stepName, Func<TTail, CancellationToken, Task<TNextOut>> stepFunc, CancellationToken token) => [StackTraceHidden] async (item) =>
    {
        try
        {
            return await stepFunc(item, token).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            PipelineExecutionException pipelineEx = HandleException(stepName, item, ex);

            if (deadLetterQueueBlock != null)
            {
                // Route to DLQ and return default to prevent block faulting
                await deadLetterQueueBlock.SendAsync(new FailedPayload<TTail>(item, pipelineEx, stepName), pipelineCancellationToken);
                return default;
            }

            throw pipelineEx;
        }
    };

    private Func<TTail, Task<TNextOut?>> ConvertToSafeExecution<TNextOut>(string stepName, Func<TTail, TNextOut> stepFunc, CancellationToken token) => [StackTraceHidden] async (item) =>
    {
        token.ThrowIfCancellationRequested();

        try
        {
            return stepFunc(item);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            PipelineExecutionException pipelineEx = HandleException(stepName, item, ex);

            if (deadLetterQueueBlock != null)
            {
                // Route to DLQ and return default to prevent block faulting
                await deadLetterQueueBlock.SendAsync(new FailedPayload<TTail>(item, pipelineEx, stepName), pipelineCancellationToken);
                return default;
            }

            throw pipelineEx;
        }
    };

    private Action<TTail> ConvertToSafeExecution(string stepName, Action<TTail> stepFunc, CancellationToken token) => [StackTraceHidden] (item) =>
    {
        token.ThrowIfCancellationRequested();

        try
        {
            stepFunc(item);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw HandleException(stepName, item, ex);
        }
    };

    private PipelineExecutionException HandleException(string stepName, TTail item, Exception ex)
    {
        PipelineExecutionException pipelineEx = new(stepName, $"Dataflow execution faulted at step '{stepName}'.", ex);
        logger?.LogError(pipelineEx, "Dataflow TransformBlock fault encountered. Payload: {@Item}", item);
        return pipelineEx;
    }
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
