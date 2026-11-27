using MelloSilveiraTools.Core.Pipelines.Models;
using MelloSilveiraTools.Core.Pipelines.Notifiers;
using MelloSilveiraTools.Core.Pipelines.Steps;
using MelloSilveiraTools.Core.Pipelines.Telemetry;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks.Dataflow;

namespace MelloSilveiraTools.Core.Pipelines.Dataflow;

/// <summary>
/// Shared immutable construction context propagated across <see cref="DataflowPipelineBuilder{THead, TTail}"/> transitions
/// and passed to the sealed <see cref="DataflowPipeline{TIn}"/> runtime.
/// </summary>
internal sealed record PipelineBuildContext<THead>(
    ILogger Logger,
    IPipelineProgressNotifier ProgressNotifier,
    ITargetBlock<THead> HeadBlock,
    ITargetBlock<FailedPayload>? DeadLetterQueueBlock,
    TimeProvider TimeProvider,
    string PipelineIdentifier,
    List<(string[] PreviousStepNames, IPipelineStep CurrentStep)> Nodes,
    CancellationToken CancellationToken)
{
    public bool IsDeadLetterQueueEnabled => DeadLetterQueueBlock is not null;

    public DataflowBlockOptions CreateBufferOptions(ExecutionDataflowBlockOptions dataFlowOptions) => new()
    {
        BoundedCapacity = dataFlowOptions.BoundedCapacity,
        CancellationToken = CancellationToken
    };

    public GroupingDataflowBlockOptions CreateGroupingOptions(ExecutionDataflowBlockOptions dataFlowOptions) => new()
    {
        BoundedCapacity = dataFlowOptions.BoundedCapacity,
        CancellationToken = CancellationToken
    };

    public ExecutionDataflowBlockOptions ToSequentialDataflowOptions(PipelineStepOptions options, string callerMethodName)
    {
        ExecutionDataflowBlockOptions dataFlowOptions = options.ToDataflowOptions(CancellationToken);

        // Stateful accumulation steps must execute sequentially to preserve deterministic ordering and state.
        if (dataFlowOptions.MaxDegreeOfParallelism > 1)
        {
            Logger.LogWarning("Max workers cannot be greater than 1 for {CallerMethodName}. Overriding to 1.", callerMethodName);
            dataFlowOptions.MaxDegreeOfParallelism = 1;
        }

        return dataFlowOptions;
    }

    public Func<TIn, Task> WrapExecution<TIn>(string callbackName, Action<TIn> callback)
        => ObservabilityExtensions.HandleExecution(Logger, ProgressNotifier, TimeProvider, PipelineIdentifier, callbackName, callback, CancellationToken);

    public Func<TIn, Task> WrapExecution<TIn>(string callbackName, Func<TIn, CancellationToken, Task> callback)
        => ObservabilityExtensions.HandleExecution(Logger, ProgressNotifier, TimeProvider, PipelineIdentifier, callbackName, callback, CancellationToken);

    public Func<TIn, Task<SafeResult<TIn>>> WrapSafeExecution<TIn>(string callbackName, Func<TIn, CancellationToken, Task> callback)
        => ObservabilityExtensions.HandleSafeExecution(Logger, ProgressNotifier, TimeProvider, PipelineIdentifier, callbackName, callback, CancellationToken);

    public Func<TIn, Task<TOut>> WrapExecution<TIn, TOut>(string callbackName, Func<TIn, TOut> callback)
        => ObservabilityExtensions.HandleExecution(Logger, ProgressNotifier, TimeProvider, PipelineIdentifier, callbackName, callback, CancellationToken);

    public Func<TIn, Task<SafeResult<TIn, TOut>>> WrapSafeExecution<TIn, TOut>(string callbackName, Func<TIn, TOut> callback)
        => ObservabilityExtensions.HandleSafeExecution(Logger, ProgressNotifier, TimeProvider, PipelineIdentifier, callbackName, callback, CancellationToken);

    public Func<TIn, Task<TOut>> WrapExecution<TIn, TOut>(string callbackName, Func<TIn, CancellationToken, Task<TOut>> callback)
        => ObservabilityExtensions.HandleExecution(Logger, ProgressNotifier, TimeProvider, PipelineIdentifier, callbackName, callback, CancellationToken);

    public Func<TIn, Task<SafeResult<TIn, TOut>>> WrapSafeExecution<TIn, TOut>(string callbackName, Func<TIn, CancellationToken, Task<TOut>> callback)
        => ObservabilityExtensions.HandleSafeExecution(Logger, ProgressNotifier, TimeProvider, PipelineIdentifier, callbackName, callback, CancellationToken);

    public Func<TIn, IAsyncEnumerable<TOut>> WrapExecution<TIn, TOut>(string callbackName, Func<TIn, CancellationToken, IAsyncEnumerable<TOut>> callback)
        => ObservabilityExtensions.HandleExecution(Logger, ProgressNotifier, TimeProvider, PipelineIdentifier, callbackName, callback, CancellationToken);

    public Func<TIn, IAsyncEnumerable<SafeResult<TIn, TOut>>> WrapSafeExecution<TIn, TOut>(string callbackName, Func<TIn, CancellationToken, IAsyncEnumerable<TOut>> callback)
        => ObservabilityExtensions.HandleSafeExecution(Logger, ProgressNotifier, TimeProvider, PipelineIdentifier, callbackName, callback, CancellationToken);

    public Func<TIn, IAsyncEnumerable<TOut>> WrapExecution<TIn, TOut>(string callbackName, Func<TIn, CancellationToken, IEnumerable<TOut>> callback)
        => ObservabilityExtensions.HandleExecution(Logger, ProgressNotifier, TimeProvider, PipelineIdentifier, callbackName, callback, CancellationToken);

    public Func<TIn, IAsyncEnumerable<SafeResult<TIn, TOut>>> WrapSafeExecution<TIn, TOut>(string callbackName, Func<TIn, CancellationToken, IEnumerable<TOut>> callback)
        => ObservabilityExtensions.HandleSafeExecution(Logger, ProgressNotifier, TimeProvider, PipelineIdentifier, callbackName, callback, CancellationToken);

    public Func<TIn, Task<TOut>> WrapForkExecution<TIn, TOut>(string primaryName, string fallbackName, Func<TIn, TOut> primary, Func<TOut, bool> fallbackCondition, Func<TIn, TOut> fallback)
        => ObservabilityExtensions.HandleExecution(Logger, ProgressNotifier, TimeProvider, PipelineIdentifier, primaryName, fallbackName, primary, fallbackCondition, fallback, CancellationToken);

    public Func<TIn, Task<SafeResult<TIn, TOut>>> WrapSafeForkExecution<TIn, TOut>(string primaryName, string fallbackName, Func<TIn, TOut> primary, Func<SafeResult<TIn, TOut>, bool> fallbackCondition, Func<TIn, TOut> fallback)
        => ObservabilityExtensions.HandleSafeExecution(Logger, ProgressNotifier, TimeProvider, PipelineIdentifier, primaryName, fallbackName, primary, fallbackCondition, fallback, CancellationToken);

    public Func<TIn, Task<TOut>> WrapForkExecution<TIn, TOut>(string primaryName, string fallbackName, Func<TIn, CancellationToken, Task<TOut>> primary, Func<TOut, bool> fallbackCondition, Func<TIn, CancellationToken, Task<TOut>> fallback)
        => ObservabilityExtensions.HandleExecution(Logger, ProgressNotifier, TimeProvider, PipelineIdentifier, primaryName, fallbackName, primary, fallbackCondition, fallback, CancellationToken);

    public Func<TIn, Task<SafeResult<TIn, TOut>>> WrapSafeForkExecution<TIn, TOut>(string primaryName, string fallbackName, Func<TIn, CancellationToken, Task<TOut>> primary, Func<SafeResult<TIn, TOut>, bool> fallbackCondition, Func<TIn, CancellationToken, Task<TOut>> fallback)
        => ObservabilityExtensions.HandleSafeExecution(Logger, ProgressNotifier, TimeProvider, PipelineIdentifier, primaryName, fallbackName, primary, fallbackCondition, fallback, CancellationToken);
}
