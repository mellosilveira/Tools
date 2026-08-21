using Microsoft.Extensions.Logging;
using System.Threading.Tasks.Dataflow;

namespace MelloSilveiraTools.Core.Pipelines.Dataflow;
/// <summary>
/// A strongly-typed fluent builder for orchestrating TPL Dataflow topologies.
/// Maintains internal block references to guarantee type invariance during pipeline construction.
/// </summary>
public sealed class DataflowBuilder<THead, TTail>(
    ITargetBlock<THead> headBlock,
    ISourceBlock<TTail> tailBlock,
    ILogger? logger)
{
    /// <summary>
    /// Appends a TransformBlock bound to an asynchronous delegate.
    /// Optimized for I/O-bound operations or computationally expensive tasks leveraging MaxWorkers > 1.
    /// </summary>
    public DataflowBuilder<THead, TNextOut> AddAsyncStep<TNextOut>(
        Func<TTail, Task<TNextOut>> stepFunc,
        PipelineStepOptions options = default)
    {
        TransformBlock<TTail, TNextOut> nextBlock = new(stepFunc, options.ToDataflowOptions());
        tailBlock.LinkTo(nextBlock, new DataflowLinkOptions { PropagateCompletion = true });
        return new DataflowBuilder<THead, TNextOut>(headBlock, nextBlock, logger);
    }

    /// <summary>
    /// Appends a TransformBlock bound to a synchronous delegate.
    /// Elides the async state machine allocation entirely, making this highly performant for 
    /// synchronous CPU-bound data mapping operations.
    /// </summary>
    public DataflowBuilder<THead, TNextOut> AddDataMapping<TNextOut>(Func<TTail, TNextOut> mapFunc, PipelineStepOptions options = default)
    {
        TransformBlock<TTail, TNextOut> nextBlock = new(mapFunc, options.ToDataflowOptions());
        tailBlock.LinkTo(nextBlock, new DataflowLinkOptions { PropagateCompletion = true });
        return new DataflowBuilder<THead, TNextOut>(headBlock, nextBlock, logger);
    }

    /// <summary>
    /// Appends an ActionBlock to consume the final pipeline output.
    /// Serves as the pipeline sink, linking the final ISourceBlock and returning the execution interface.
    /// </summary>
    public IDataflowPipeline<THead> BuildTerminal(Action<TTail> terminalAction, PipelineStepOptions options = default)
    {
        ActionBlock<TTail> terminalBlock = new(terminalAction, options.ToDataflowOptions());
        tailBlock.LinkTo(terminalBlock, new DataflowLinkOptions { PropagateCompletion = true });
        return new DataflowPipeline<THead>(headBlock, terminalBlock.Completion, logger);
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