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
        /// Appends an asynchronous execution step to the fluent pipeline topology.
        /// </summary>
        public IFluentPipelineBuilder<TInitial, TNextOut> AddStep<TNextOut>(IPipelineStep<TCurrentOut, TNextOut> step) 
            => builder.AddStep(step.Name, step.ExecuteAsync);

        /// <summary>
        /// Injects a synchronous data transformation projection into the pipeline execution graph.
        /// </summary>
        public IFluentPipelineBuilder<TInitial, TNextOut> AddDataMapping<TNextOut>(Func<TCurrentOut, TNextOut> mapper) 
            => builder.AddStep("DataMapping", (input, _) => Task.FromResult(mapper(input)));
    }

    extension<THead, TCurrentOut>(IDataflowPipelineBuilder<THead, TCurrentOut> builder)
    {
        /// <summary>
        /// Injects a pre-configured <see cref="IPipelineStep{TIn, TOut}"/> instance into the continuous Dataflow execution graph.
        /// </summary>
        public IDataflowPipelineBuilder<THead, TNextOut> AddStep<TNextOut>(IPipelineStep<TCurrentOut, TNextOut> step, PipelineStepOptions options = default) 
            => builder.AddStep(step.Name, step.ExecuteAsync, options);

        /// <summary>
        /// Injects a pre-configured <see cref="IPipelineStep{TIn, TOut}"/> as a side-branch consumer linked to a BroadcastBlock,
        /// while propagating the primary data stream downstream for continued pipeline orchestration.
        /// </summary>
        public IDataflowPipelineBuilder<THead, TCurrentOut> AddBroadcastStep<TBranchOut>(IPipelineStep<TCurrentOut, TBranchOut> step, PipelineStepOptions options = default) 
            => builder.AddBroadcastStep(step, null, options);

        /// <summary>
        /// Injects a pre-configured <see cref="IPipelineStep{TIn, TOut}"/> as a side-branch consumer linked to a BroadcastBlock,
        /// while propagating the primary data stream downstream for continued pipeline orchestration.
        /// </summary>
        public IDataflowPipelineBuilder<THead, TCurrentOut> AddBroadcastStep<TBranchOut>(IPipelineStep<TCurrentOut, TBranchOut> step, Func<TCurrentOut, TCurrentOut>? cloneFunc, PipelineStepOptions options = default) 
            => builder.AddBroadcastBlock(step.Name, step.ExecuteAsync, cloneFunc, options);

        /// <summary>
        /// Injects a pre-configured <see cref="IPipelineStep{TIn, TOut}"/> as a side-branch consumer linked to a BroadcastBlock,
        /// while propagating the primary data stream downstream for continued pipeline orchestration.
        /// Alias for <see cref="AddBroadcastStep"/>.
        /// </summary>
        public IDataflowPipelineBuilder<THead, TCurrentOut> AddBroadcastBlock<TBranchOut>(IPipelineStep<TCurrentOut, TBranchOut> step, PipelineStepOptions options = default)
            => builder.AddBroadcastStep(step, options);

        /// <summary>
        /// Injects a pre-configured <see cref="IPipelineStep{TIn, TOut}"/> as a side-branch consumer linked to a BroadcastBlock,
        /// while propagating the primary data stream downstream for continued pipeline orchestration.
        /// Alias for <see cref="AddBroadcastStep"/>.
        /// </summary>
        public IDataflowPipelineBuilder<THead, TCurrentOut> AddBroadcastBlock<TBranchOut>(IPipelineStep<TCurrentOut, TBranchOut> step, Func<TCurrentOut, TCurrentOut>? cloneFunc, PipelineStepOptions options = default) => builder.AddBroadcastStep(step, cloneFunc, options);

        /// <summary>
        /// Forks the pipeline execution based on success or failure using a lightweight ValueTuple envelope.
        /// Successful items proceed to the next stage, while failed items are routed to a recovery step and terminated.
        /// </summary>
        public IDataflowPipelineBuilder<THead, TNextOut> AddForkingStep<TNextOut>(IPipelineStep<TCurrentOut, TNextOut> step, Func<TNextOut, bool> fallbackCondition, IPipelineStep<TCurrentOut, TNextOut> fallbackStep, PipelineStepOptions options = default) 
            => builder.AddForkingStep(step.Name, fallbackStep.Name, step.ExecuteAsync, fallbackCondition, fallbackStep.ExecuteAsync, options);
    }
}
