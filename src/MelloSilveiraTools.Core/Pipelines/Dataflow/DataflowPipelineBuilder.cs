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
    ILogger? logger,
    CancellationToken pipelineCancellationToken) 
    : IDataflowPipelineBuilder<THead, TTail>
{
    /// <inheritdoc/>
    public IDataflowPipelineBuilder<THead, TNextOut> AddStep<TNextOut>(string stepName, Func<TTail, CancellationToken, Task<TNextOut>> stepFunc, PipelineStepOptions options = default)
    {
        Func<TTail, Task<TNextOut>> safeExecution = ConvertToSafeExecution(stepName, stepFunc, pipelineCancellationToken);
        TransformBlock<TTail, TNextOut> nextBlock = new(safeExecution, options.ToDataflowOptions(pipelineCancellationToken));
        tailBlock.LinkTo(nextBlock, new DataflowLinkOptions { PropagateCompletion = true });

        return new DataflowPipelineBuilder<THead, TNextOut>(headBlock, nextBlock, logger, pipelineCancellationToken);
    }

    /// <inheritdoc/>
    public IDataflowPipelineBuilder<THead, TNextOut> AddDataMapping<TNextOut>(string stepName, Func<TTail, TNextOut> mapFunc, PipelineStepOptions options = default)
    {
        Func<TTail, TNextOut> safeExecution = ConvertToSafeExecution(stepName, mapFunc, pipelineCancellationToken);
        TransformBlock<TTail, TNextOut> nextBlock = new(safeExecution, options.ToDataflowOptions(pipelineCancellationToken));
        tailBlock.LinkTo(nextBlock, new DataflowLinkOptions { PropagateCompletion = true });

        return new DataflowPipelineBuilder<THead, TNextOut>(headBlock, nextBlock, logger, pipelineCancellationToken);
    }

    /// <inheritdoc/>
    public IDataflowPipelineBuilder<THead, TNextOut> AddDataMapping<TNextOut>(string stepName, Func<TTail, CancellationToken, Task<TNextOut>> mapFunc, PipelineStepOptions options = default)
    {
        Func<TTail, Task<TNextOut>> safeExecution = ConvertToSafeExecution(stepName, mapFunc, pipelineCancellationToken);
        TransformBlock<TTail, TNextOut> nextBlock = new(safeExecution, options.ToDataflowOptions(pipelineCancellationToken));
        tailBlock.LinkTo(nextBlock, new DataflowLinkOptions { PropagateCompletion = true });

        return new DataflowPipelineBuilder<THead, TNextOut>(headBlock, nextBlock, logger, pipelineCancellationToken);
    }

    /// <inheritdoc/>
    public IDataflowPipeline<THead> BuildTerminal(string stepName, Action<TTail> terminalAction, PipelineStepOptions options = default)
    {
        Action<TTail> safeExecution = ConvertToSafeExecution(stepName, terminalAction, pipelineCancellationToken);
        ActionBlock<TTail> terminalBlock = new(terminalAction, options.ToDataflowOptions(pipelineCancellationToken));
        tailBlock.LinkTo(terminalBlock, new DataflowLinkOptions { PropagateCompletion = true });

        return new DataflowPipeline<THead>(headBlock, terminalBlock.Completion, logger);
    }

    private Func<TTail, Task<TNextOut>> ConvertToSafeExecution<TNextOut>(string stepName, Func<TTail, CancellationToken, Task<TNextOut>> stepFunc, CancellationToken token) => [StackTraceHidden] async (item) =>
    {
        try
        {
            return await stepFunc(item, token).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw CreateAndLogFault(stepName, item, ex);
        }
    };

    private Func<TTail, TNextOut> ConvertToSafeExecution<TNextOut>(string stepName, Func<TTail, TNextOut> stepFunc, CancellationToken token) => [StackTraceHidden] (item) =>
    {
        token.ThrowIfCancellationRequested();

        try
        {
            return stepFunc(item);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw CreateAndLogFault(stepName, item, ex);
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
            throw CreateAndLogFault(stepName, item, ex);
        }
    };

    private PipelineExecutionException CreateAndLogFault(string stepName, TTail item, Exception ex)
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
