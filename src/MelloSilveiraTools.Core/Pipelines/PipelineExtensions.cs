using MelloSilveiraTools.Core.Pipelines.Dataflow;
using MelloSilveiraTools.Core.Pipelines.Fluent;

namespace MelloSilveiraTools.Core.Pipelines;

/// <summary>
/// Encapsulates extension methods for <see cref="IFluentPipelineBuilder{TInitialIn, TCurrentOut}"/> .
/// Abstracts the underlying type-erased builder mechanics by providing strongly-typed fluid configuration APIs.
/// </summary>
public static class PipelineExtensions
{
    extension<TInitial, TCurrentOut>(IFluentPipelineBuilder<TInitial, TCurrentOut> builder)
    {
        /// <summary>
        /// Appends an asynchronous execution step to the pipeline topology.
        /// Resolves the step's runtime type name for telemetry and delegates execution via the strongly-typed interface.
        /// </summary>
        public IFluentPipelineBuilder<TInitial, TNextOut> AddStep<TNextOut>(IStep<TCurrentOut, TNextOut> step)
        {
            ArgumentNullException.ThrowIfNull(step);
            return builder.AddStep(step.Name, step.ExecuteAsync);
        }

        /// <summary>
        /// Injects a synchronous data transformation projection into the pipeline execution graph.
        /// Wraps the synchronous lambda within a Task to satisfy the asynchronous execution engine contract.
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
        /// Injects a pre-configured <see cref="IStep{TIn, TOut}"/> instance into the continuous Dataflow execution graph.
        /// Encapsulates the reference-type Task allocation within a ValueTask fast-path wrapper to satisfy 
        /// the optimized asynchronous requirements of the Dataflow engine.
        /// </summary>
        /// <typeparam name="TNextOut">The terminal output payload type yielded by the appended step.</typeparam>
        /// <param name="step">The concrete step implementation encapsulating the domain execution logic.</param>
        /// <param name="options">The localized concurrency and backpressure configuration defining the block's throughput limits.</param>
        /// <returns>A mutated builder instance binding the downstream pipeline to the newly yielded terminal state.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the provided step instance is null.</exception>
        public IDataflowPipelineBuilder<THead, TNextOut> AddStep<TNextOut>(IStep<TCurrentOut, TNextOut> step, PipelineStepOptions options = default)
        {
            ArgumentNullException.ThrowIfNull(step);
            return builder.AddStep(step.Name, step.ExecuteAsync, options);
        }
    }
}