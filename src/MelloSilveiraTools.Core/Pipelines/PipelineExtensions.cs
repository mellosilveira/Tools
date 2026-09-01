using MelloSilveiraTools.Core.Pipelines.Dataflow;
using MelloSilveiraTools.Core.Pipelines.Fluent;

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
        IPipelineStep<TCurrentOut, TNextOut> step,
        Func<TNextOut, bool> fallbackCondition,
        IPipelineStep<TCurrentOut, TNextOut> fallbackStep,
        PipelineStepOptions options = default)
    {
        ArgumentNullException.ThrowIfNull(step);
        ArgumentNullException.ThrowIfNull(fallbackStep);
        return builder.AddForkingStep(step.Name, fallbackStep.Name, step.ExecuteAsync, fallbackCondition, fallbackStep.ExecuteAsync, options);
    }
}
