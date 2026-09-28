using MelloSilveiraTools.Core.Pipelines.Models;
using MelloSilveiraTools.Core.Pipelines.Steps;

namespace MelloSilveiraTools.Core.Pipelines.Dataflow;

    public readonly struct DataflowStepBuilder<THead, TTail>
{
    private readonly IDataflowPipelineBuilder<THead, TTail> _pipelineBuilder;
    private readonly List<Func<TTail, CancellationToken, Task<object?>>>? _handlers;

    public IDataflowPipelineBuilder<THead, TTail> Builder => _pipelineBuilder;

    public DataflowStepBuilder(IDataflowPipelineBuilder<THead, TTail> pipelineBuilder, bool enableHandlers = true)
    {
        _pipelineBuilder = pipelineBuilder;
        _handlers = enableHandlers ? new List<Func<TTail, CancellationToken, Task<object?>>>() : null;
    }

    public DataflowStepBuilder<THead, TTail> AddHandler<THandlerOut>(IAsyncPipelineStep<TTail, THandlerOut> handler)
    {
        if (_handlers == null) throw new InvalidOperationException("Handlers are not enabled for this builder state.");
        _handlers.Add(async (payload, ct) => await handler.ExecuteAsync(payload, ct).ConfigureAwait(false));
        return this;
    }

    public DataflowStepBuilder<THead, TTail> AddHandler(IAsyncPipelineStep<TTail> handler)
    {
        if (_handlers == null) throw new InvalidOperationException("Handlers are not enabled for this builder state.");
        _handlers.Add(async (payload, ct) => 
        {
            await handler.ExecuteAsync(payload, ct).ConfigureAwait(false);
            return null;
        });
        return this;
    }

    // Forwarding methods to allow seamless chaining without needing to call CompleteHandlers()
    public DataflowStepBuilder<THead, TNextOut> AddStep<TNextOut>(ISyncPipelineStep<TTail, TNextOut> step, PipelineStepOptions options = default)
        => new(CompleteHandlers().AppendStep(step, options));

    public DataflowStepBuilder<THead, TNextOut> AddStep<TNextOut>(IAsyncPipelineStep<TTail, TNextOut> step, PipelineStepOptions options = default)
        => new(CompleteHandlers().AppendStep(step, options));

    public DataflowStepBuilder<THead, TNextOut> AddStep<TNextOut>(IAsyncEnumerablePipelineStep<TTail, TNextOut> step, PipelineStepOptions options = default)
        => new(CompleteHandlers().AppendStep(step, options));

    public DataflowStepBuilder<THead, TNextOut> AddDataMapping<TNextOut>(Func<TTail, TNextOut> mapFunc, PipelineStepOptions options = default)
        => new(CompleteHandlers().AddDataMapping(mapFunc, options));

    public DataflowStepBuilder<THead, TTail[]> AddGroupWhileStep(Func<TTail, TTail, bool> condition, PipelineStepOptions options = default)
        => new(CompleteHandlers().AddGroupWhileStep(condition, options));

    public DataflowStepBuilder<THead, TTail[]> AddCollectAllStep(PipelineStepOptions options = default)
        => new(CompleteHandlers().AddCollectAllStep(options));

    public IDataflowPipelineBuilder<THead, TNextOut> CompleteHandlers<TNextOut>(Func<TTail, object?[], TNextOut> combiner, PipelineStepOptions options = default)
    {
        if (_handlers == null || _handlers.Count == 0)
        {
            return _pipelineBuilder.AddDataMapping(payload => combiner(payload, []), options);
        }

        var handlersArray = _handlers.ToArray();

        return _pipelineBuilder.AddDataMapping(async (payload, ct) =>
        {
            var tasks = new Task<object?>[handlersArray.Length];
            for (int i = 0; i < handlersArray.Length; i++)
                tasks[i] = handlersArray[i](payload, ct);
            
            object?[] results = await Task.WhenAll(tasks).ConfigureAwait(false);
            return combiner(payload, results);
        }, options);
    }

    public IDataflowPipelineBuilder<THead, TTail> CompleteHandlers(PipelineStepOptions options = default)
    {
        if (_handlers == null || _handlers.Count == 0)
            return _pipelineBuilder;

        var handlersArray = _handlers.ToArray();
        return _pipelineBuilder.AddDataMapping(async (payload, ct) =>
        {
            var tasks = new Task<object?>[handlersArray.Length];
            for (int i = 0; i < handlersArray.Length; i++)
                tasks[i] = handlersArray[i](payload, ct);
            
            await Task.WhenAll(tasks).ConfigureAwait(false);
            return payload;
        }, options);
    }

    public DataflowStepBuilder<THead, Tuple<TOut1, TOut2>> Fork<TOut1, TOut2>(
        Func<IDataflowPipelineBuilder<THead, TTail>, IDataflowPipelineBuilder<THead, TOut1>> branch1,
        Func<IDataflowPipelineBuilder<THead, TTail>, IDataflowPipelineBuilder<THead, TOut2>> branch2,
        PipelineStepOptions options = default)
        => new(CompleteHandlers().Fork(branch1, branch2, options));

    public DataflowStepBuilder<THead, Tuple<TOut1, TOut2, TOut3>> Fork<TOut1, TOut2, TOut3>(
        Func<IDataflowPipelineBuilder<THead, TTail>, IDataflowPipelineBuilder<THead, TOut1>> branch1,
        Func<IDataflowPipelineBuilder<THead, TTail>, IDataflowPipelineBuilder<THead, TOut2>> branch2,
        Func<IDataflowPipelineBuilder<THead, TTail>, IDataflowPipelineBuilder<THead, TOut3>> branch3,
        PipelineStepOptions options = default)
        => new(CompleteHandlers().Fork(branch1, branch2, branch3, options));

    public IDataflowPipeline<THead> BuildPipelineTerminal(PipelineStepOptions options = default) => _pipelineBuilder.BuildTerminal(options: options);
}

public static class DataflowFluentBuilderExtensions
{
    public static DataflowStepBuilder<THead, TNextOut> AddStep<THead, TTail, TNextOut>(
        this IDataflowPipelineBuilder<THead, TTail> builder,
        ISyncPipelineStep<TTail, TNextOut> step,
        PipelineStepOptions options = default)
    {
        return new DataflowStepBuilder<THead, TNextOut>(builder.AppendStep(step, options));
    }

    public static DataflowStepBuilder<THead, TNextOut> AddStep<THead, TTail, TNextOut>(
        this IDataflowPipelineBuilder<THead, TTail> builder,
        IAsyncPipelineStep<TTail, TNextOut> step,
        PipelineStepOptions options = default)
    {
        return new DataflowStepBuilder<THead, TNextOut>(builder.AppendStep(step, options));
    }

    public static DataflowStepBuilder<THead, TNextOut> AddStep<THead, TTail, TNextOut>(
        this IDataflowPipelineBuilder<THead, TTail> builder,
        IAsyncEnumerablePipelineStep<TTail, TNextOut> step,
        PipelineStepOptions options = default)
    {
        return new DataflowStepBuilder<THead, TNextOut>(builder.AppendStep(step, options));
    }

    public static DataflowStepBuilder<THead, TNextOut> AddDataMapping<THead, TTail, TNextOut>(
        this IDataflowPipelineBuilder<THead, TTail> builder,
        Func<TTail, TNextOut> mapFunc,
        PipelineStepOptions options = default)
    {
        return new DataflowStepBuilder<THead, TNextOut>(builder.AddDataMapping(mapFunc, options));
    }

    public static DataflowStepBuilder<THead, TTail[]> AddGroupWhileStep<THead, TTail>(
        this IDataflowPipelineBuilder<THead, TTail> builder,
        Func<TTail, TTail, bool> condition,
        PipelineStepOptions options = default)
    {
        return new DataflowStepBuilder<THead, TTail[]>(builder.AddGroupWhileStep(condition, options));
    }

    public static DataflowStepBuilder<THead, TTail[]> AddCollectAllStep<THead, TTail>(
        this IDataflowPipelineBuilder<THead, TTail> builder,
        PipelineStepOptions options = default)
    {
        return new DataflowStepBuilder<THead, TTail[]>(builder.AddCollectAllStep(options));
    }

    public static IDataflowPipeline<THead> BuildPipelineTerminal<THead, TTail>(
        this IDataflowPipelineBuilder<THead, TTail> builder,
        PipelineStepOptions options = default)
    {
        return builder.BuildTerminal(options: options);
    }

    public static DataflowStepBuilder<THead, TTail> AddBroadcastStep<THead, TTail>(
        this DataflowStepBuilder<THead, TTail> builder,
        IAsyncPipelineStep<TTail> step,
        Func<TTail, TTail>? cloneFunc = null,
        PipelineStepOptions options = default)
    {
        return new DataflowStepBuilder<THead, TTail>(builder.Builder.AddBroadcastStep(step, cloneFunc, options));
    }

}
