using MelloSilveiraTools.Core.Pipelines.Dataflow;
using MelloSilveiraTools.Core.Pipelines.Fluent;
using MelloSilveiraTools.Core.Pipelines.Models;
using MelloSilveiraTools.Core.Pipelines.Steps;

namespace MelloSilveiraTools.Core.ExtensionMethods;

/// <summary>
/// Encapsulates extension methods for pipeline builders.
/// Abstracts underlying mechanics by providing strongly-typed fluid configuration APIs.
/// </summary>
public static class PipelineExtensions
{
    extension<TInitial, TCurrentOut>(IFluentPipelineBuilder<TInitial, TCurrentOut> builder)
    {
        /// <summary>
        /// Appends an asynchronous execution step to the fluent pipeline topology.
        /// </summary>
        /// <typeparam name="TNextOut">The resultant state type emitted by the step.</typeparam>
        /// <param name="step">The asynchronous pipeline step instance to append.</param>
        /// <returns>A new builder instance binding the root input to the newly mutated terminal state.</returns>
        public IFluentPipelineBuilder<TInitial, TNextOut> AddStep<TNextOut>(IAsyncPipelineStep<TCurrentOut, TNextOut> step) 
            => builder.AddStep(step.Name, step.ExecuteAsync);

        /// <summary>
        /// Appends a synchronous execution step to the fluent pipeline topology.
        /// </summary>
        /// <typeparam name="TNextOut">The resultant state type emitted by the step.</typeparam>
        /// <param name="step">The synchronous pipeline step instance to append.</param>
        /// <returns>A new builder instance binding the root input to the newly mutated terminal state.</returns>
        public IFluentPipelineBuilder<TInitial, TNextOut> AddStep<TNextOut>(ISyncPipelineStep<TCurrentOut, TNextOut> step) 
            => builder.AddStep(step.Name, step.Execute);

        /// <summary>
        /// Appends a streaming execution step returning an asynchronous sequence to the fluent pipeline topology.
        /// </summary>
        /// <typeparam name="TNextOut">The element type emitted by the streaming step.</typeparam>
        /// <param name="step">The streaming pipeline step instance to append.</param>
        /// <returns>A new builder instance yielding an asynchronous sequence of elements.</returns>
        public IFluentPipelineBuilder<TInitial, IAsyncEnumerable<TNextOut>> AddStep<TNextOut>(IAsyncEnumerablePipelineStep<TCurrentOut, TNextOut> step) 
            => builder.AddStep(step.Name, (input, ct) => Task.FromResult(step.ExecuteAsync(input, ct)));

        /// <summary>
        /// Injects a synchronous data transformation projection into the pipeline execution graph.
        /// </summary>
        /// <typeparam name="TNextOut">The resultant state type emitted by the mapper function.</typeparam>
        /// <param name="mapper">The synchronous data mapping projection function.</param>
        /// <returns>A new builder instance binding the root input to the newly mutated terminal state.</returns>
        public IFluentPipelineBuilder<TInitial, TNextOut> AddDataMapping<TNextOut>(Func<TCurrentOut, TNextOut> mapper) 
            => builder.AddStep("DataMapping", mapper);
    }

    extension<THead, TCurrentOut>(IDataflowPipelineBuilder<THead, TCurrentOut> builder)
    {
        /// <summary>
        /// Injects a pre-configured <see cref="IAsyncPipelineStep{TIn, TOut}"/> instance into the continuous Dataflow execution graph.
        /// </summary>
        /// <typeparam name="TNextOut">The resultant state type emitted by the step.</typeparam>
        /// <param name="step">The asynchronous pipeline step instance to append.</param>
        /// <param name="options">Options configuring buffer sizes and cancellation tokens for the block.</param>
        /// <returns>A new builder instance representing the next pipeline stage.</returns>
        public IDataflowPipelineBuilder<THead, TNextOut> AddStep<TNextOut>(IAsyncPipelineStep<TCurrentOut, TNextOut> step, PipelineStepOptions options = default) 
            => builder.AddStep(step.Name, step.ExecuteAsync, options);

        /// <summary>
        /// Injects a pre-configured <see cref="ISyncPipelineStep{TIn, TOut}"/> instance into the continuous Dataflow execution graph.
        /// </summary>
        /// <typeparam name="TNextOut">The resultant state type emitted by the step.</typeparam>
        /// <param name="step">The synchronous pipeline step instance to append.</param>
        /// <param name="options">Options configuring buffer sizes and cancellation tokens for the block.</param>
        /// <returns>A new builder instance representing the next pipeline stage.</returns>
        public IDataflowPipelineBuilder<THead, TNextOut> AddStep<TNextOut>(ISyncPipelineStep<TCurrentOut, TNextOut> step, PipelineStepOptions options = default) 
            => builder.AddStep(step.Name, step.Execute, options);

        /// <summary>
        /// Injects a pre-configured <see cref="IAsyncEnumerablePipelineStep{TIn, TOut}"/> instance into the continuous Dataflow execution graph,
        /// streaming individual output items downstream.
        /// </summary>
        /// <typeparam name="TNextOut">The element type yielded by the streaming step.</typeparam>
        /// <param name="step">The streaming pipeline step instance to append.</param>
        /// <param name="options">Options configuring buffer sizes and cancellation tokens for the block.</param>
        /// <returns>A new builder instance representing the next pipeline stage.</returns>
        public IDataflowPipelineBuilder<THead, TNextOut> AddStep<TNextOut>(IAsyncEnumerablePipelineStep<TCurrentOut, TNextOut> step, PipelineStepOptions options = default) 
            => builder.AddStep(step.Name, step.ExecuteAsync, options);

        /// <summary>
        /// Injects a pre-configured <see cref="IAsyncPipelineStep{TIn, TOut}"/> as a side-branch consumer linked to a BroadcastBlock,
        /// while propagating the primary data stream downstream for continued pipeline orchestration.
        /// </summary>
        /// <typeparam name="TBranchOut">The return state type produced by the side-branch step.</typeparam>
        /// <param name="step">The asynchronous pipeline step to broadcast to.</param>
        /// <param name="options">Options configuring buffer sizes and cancellation tokens for the block.</param>
        /// <returns>The builder instance configured with the broadcast block linked to the branch target.</returns>
        public IDataflowPipelineBuilder<THead, TCurrentOut> AddBroadcastStep<TBranchOut>(IAsyncPipelineStep<TCurrentOut, TBranchOut> step, PipelineStepOptions options = default) 
            => builder.AddBroadcastStep(step, null, options);

        /// <summary>
        /// Injects a pre-configured <see cref="IAsyncPipelineStep{TIn, TOut}"/> as a side-branch consumer linked to a BroadcastBlock,
        /// while propagating the primary data stream downstream for continued pipeline orchestration.
        /// </summary>
        /// <typeparam name="TBranchOut">The return state type produced by the side-branch step.</typeparam>
        /// <param name="step">The asynchronous pipeline step to broadcast to.</param>
        /// <param name="cloneFunc">An optional delegate to clone each payload before broadcasting.</param>
        /// <param name="options">Options configuring buffer sizes and cancellation tokens for the block.</param>
        /// <returns>The builder instance configured with the broadcast block linked to the branch target.</returns>
        public IDataflowPipelineBuilder<THead, TCurrentOut> AddBroadcastStep<TBranchOut>(IAsyncPipelineStep<TCurrentOut, TBranchOut> step, Func<TCurrentOut, TCurrentOut>? cloneFunc, PipelineStepOptions options = default) 
            => builder.AddBroadcastBlock(step.Name, step.ExecuteAsync, cloneFunc, options);

        /// <summary>
        /// Injects a pre-configured <see cref="ISyncPipelineStep{TIn, TOut}"/> as a side-branch consumer linked to a BroadcastBlock,
        /// while propagating the primary data stream downstream for continued pipeline orchestration.
        /// </summary>
        /// <typeparam name="TBranchOut">The return state type produced by the side-branch step.</typeparam>
        /// <param name="step">The synchronous pipeline step to broadcast to.</param>
        /// <param name="options">Options configuring buffer sizes and cancellation tokens for the block.</param>
        /// <returns>The builder instance configured with the broadcast block linked to the branch target.</returns>
        public IDataflowPipelineBuilder<THead, TCurrentOut> AddBroadcastStep<TBranchOut>(ISyncPipelineStep<TCurrentOut, TBranchOut> step, PipelineStepOptions options = default) 
            => builder.AddBroadcastStep(step, null, options);

        /// <summary>
        /// Injects a pre-configured <see cref="ISyncPipelineStep{TIn, TOut}"/> as a side-branch consumer linked to a BroadcastBlock,
        /// while propagating the primary data stream downstream for continued pipeline orchestration.
        /// </summary>
        /// <typeparam name="TBranchOut">The return state type produced by the side-branch step.</typeparam>
        /// <param name="step">The synchronous pipeline step to broadcast to.</param>
        /// <param name="cloneFunc">An optional delegate to clone each payload before broadcasting.</param>
        /// <param name="options">Options configuring buffer sizes and cancellation tokens for the block.</param>
        /// <returns>The builder instance configured with the broadcast block linked to the branch target.</returns>
        public IDataflowPipelineBuilder<THead, TCurrentOut> AddBroadcastStep<TBranchOut>(ISyncPipelineStep<TCurrentOut, TBranchOut> step, Func<TCurrentOut, TCurrentOut>? cloneFunc, PipelineStepOptions options = default) 
            => builder.AddBroadcastBlock(step.Name, item => step.Execute(item), cloneFunc, options);

        /// <summary>
        /// Injects a pre-configured <see cref="IAsyncEnumerablePipelineStep{TIn, TOut}"/> as a side-branch consumer linked to a BroadcastBlock,
        /// while propagating the primary data stream downstream for continued pipeline orchestration.
        /// </summary>
        /// <typeparam name="TBranchOut">The return state type produced by the side-branch step.</typeparam>
        /// <param name="step">The streaming pipeline step to broadcast to.</param>
        /// <param name="options">Options configuring buffer sizes and cancellation tokens for the block.</param>
        /// <returns>The builder instance configured with the broadcast block linked to the branch target.</returns>
        public IDataflowPipelineBuilder<THead, TCurrentOut> AddBroadcastStep<TBranchOut>(IAsyncEnumerablePipelineStep<TCurrentOut, TBranchOut> step, PipelineStepOptions options = default) 
            => builder.AddBroadcastStep(step, null, options);

        /// <summary>
        /// Injects a pre-configured <see cref="IAsyncEnumerablePipelineStep{TIn, TOut}"/> as a side-branch consumer linked to a BroadcastBlock,
        /// while propagating the primary data stream downstream for continued pipeline orchestration.
        /// </summary>
        /// <typeparam name="TBranchOut">The return state type produced by the side-branch step.</typeparam>
        /// <param name="step">The streaming pipeline step to broadcast to.</param>
        /// <param name="cloneFunc">An optional delegate to clone each payload before broadcasting.</param>
        /// <param name="options">Options configuring buffer sizes and cancellation tokens for the block.</param>
        /// <returns>The builder instance configured with the broadcast block linked to the branch target.</returns>
        public IDataflowPipelineBuilder<THead, TCurrentOut> AddBroadcastStep<TBranchOut>(IAsyncEnumerablePipelineStep<TCurrentOut, TBranchOut> step, Func<TCurrentOut, TCurrentOut>? cloneFunc, PipelineStepOptions options = default) 
            => builder.AddBroadcastBlock(step.Name, async (item, ct) => 
            {
                await foreach (TBranchOut _ in step.ExecuteAsync(item, ct).ConfigureAwait(false)) { }
            }, cloneFunc, options);

        /// <summary>
        /// Injects a pre-configured <see cref="IAsyncPipelineStep{TIn, TOut}"/> as a side-branch consumer linked to a BroadcastBlock,
        /// while propagating the primary data stream downstream for continued pipeline orchestration.
        /// Alias for <see cref="AddBroadcastStep{TBranchOut}(IAsyncPipelineStep{TCurrentOut, TBranchOut}, PipelineStepOptions)"/>.
        /// </summary>
        public IDataflowPipelineBuilder<THead, TCurrentOut> AddBroadcastBlock<TBranchOut>(IAsyncPipelineStep<TCurrentOut, TBranchOut> step, PipelineStepOptions options = default)
            => builder.AddBroadcastStep(step, options);

        /// <summary>
        /// Injects a pre-configured <see cref="IAsyncPipelineStep{TIn, TOut}"/> as a side-branch consumer linked to a BroadcastBlock,
        /// while propagating the primary data stream downstream for continued pipeline orchestration.
        /// Alias for <see cref="AddBroadcastStep{TBranchOut}(IAsyncPipelineStep{TCurrentOut, TBranchOut}, Func{TCurrentOut, TCurrentOut}?, PipelineStepOptions)"/>.
        /// </summary>
        public IDataflowPipelineBuilder<THead, TCurrentOut> AddBroadcastBlock<TBranchOut>(IAsyncPipelineStep<TCurrentOut, TBranchOut> step, Func<TCurrentOut, TCurrentOut>? cloneFunc, PipelineStepOptions options = default) 
            => builder.AddBroadcastStep(step, cloneFunc, options);

        /// <summary>
        /// Injects a pre-configured <see cref="ISyncPipelineStep{TIn, TOut}"/> as a side-branch consumer linked to a BroadcastBlock,
        /// while propagating the primary data stream downstream for continued pipeline orchestration.
        /// Alias for <see cref="AddBroadcastStep{TBranchOut}(ISyncPipelineStep{TCurrentOut, TBranchOut}, PipelineStepOptions)"/>.
        /// </summary>
        public IDataflowPipelineBuilder<THead, TCurrentOut> AddBroadcastBlock<TBranchOut>(ISyncPipelineStep<TCurrentOut, TBranchOut> step, PipelineStepOptions options = default)
            => builder.AddBroadcastStep(step, options);

        /// <summary>
        /// Injects a pre-configured <see cref="ISyncPipelineStep{TIn, TOut}"/> as a side-branch consumer linked to a BroadcastBlock,
        /// while propagating the primary data stream downstream for continued pipeline orchestration.
        /// Alias for <see cref="AddBroadcastStep{TBranchOut}(ISyncPipelineStep{TCurrentOut, TBranchOut}, Func{TCurrentOut, TCurrentOut}?, PipelineStepOptions)"/>.
        /// </summary>
        public IDataflowPipelineBuilder<THead, TCurrentOut> AddBroadcastBlock<TBranchOut>(ISyncPipelineStep<TCurrentOut, TBranchOut> step, Func<TCurrentOut, TCurrentOut>? cloneFunc, PipelineStepOptions options = default) 
            => builder.AddBroadcastStep(step, cloneFunc, options);

        /// <summary>
        /// Injects a pre-configured <see cref="IAsyncEnumerablePipelineStep{TIn, TOut}"/> as a side-branch consumer linked to a BroadcastBlock,
        /// while propagating the primary data stream downstream for continued pipeline orchestration.
        /// Alias for <see cref="AddBroadcastStep{TBranchOut}(IAsyncEnumerablePipelineStep{TCurrentOut, TBranchOut}, PipelineStepOptions)"/>.
        /// </summary>
        public IDataflowPipelineBuilder<THead, TCurrentOut> AddBroadcastBlock<TBranchOut>(IAsyncEnumerablePipelineStep<TCurrentOut, TBranchOut> step, PipelineStepOptions options = default)
            => builder.AddBroadcastStep(step, options);

        /// <summary>
        /// Injects a pre-configured <see cref="IAsyncEnumerablePipelineStep{TIn, TOut}"/> as a side-branch consumer linked to a BroadcastBlock,
        /// while propagating the primary data stream downstream for continued pipeline orchestration.
        /// Alias for <see cref="AddBroadcastStep{TBranchOut}(IAsyncEnumerablePipelineStep{TCurrentOut, TBranchOut}, Func{TCurrentOut, TCurrentOut}?, PipelineStepOptions)"/>.
        /// </summary>
        public IDataflowPipelineBuilder<THead, TCurrentOut> AddBroadcastBlock<TBranchOut>(IAsyncEnumerablePipelineStep<TCurrentOut, TBranchOut> step, Func<TCurrentOut, TCurrentOut>? cloneFunc, PipelineStepOptions options = default) 
            => builder.AddBroadcastStep(step, cloneFunc, options);

        /// <summary>
        /// Forks the pipeline execution based on success or failure using an asynchronous step and recovery step.
        /// </summary>
        public IDataflowPipelineBuilder<THead, TNextOut> AddForkingStep<TNextOut>(IAsyncPipelineStep<TCurrentOut, TNextOut> step, Func<TNextOut, bool> fallbackCondition, IAsyncPipelineStep<TCurrentOut, TNextOut> fallbackStep, PipelineStepOptions options = default) 
            => builder.AddForkingStep(step.Name, fallbackStep.Name, step.ExecuteAsync, fallbackCondition, fallbackStep.ExecuteAsync, options);

        /// <summary>
        /// Forks the pipeline execution based on success or failure using a synchronous step and recovery step.
        /// </summary>
        public IDataflowPipelineBuilder<THead, TNextOut> AddForkingStep<TNextOut>(ISyncPipelineStep<TCurrentOut, TNextOut> step, Func<TNextOut, bool> fallbackCondition, ISyncPipelineStep<TCurrentOut, TNextOut> fallbackStep, PipelineStepOptions options = default) 
            => builder.AddForkingStep(step.Name, fallbackStep.Name, (input, _) => Task.FromResult(step.Execute(input)), fallbackCondition, (input, _) => Task.FromResult(fallbackStep.Execute(input)), options);
    }
}
