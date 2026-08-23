using MelloSilveiraTools.Core.Pipelines.Dataflow;
using MelloSilveiraTools.Core.Pipelines.Fluent;
using System.Threading.Tasks.Dataflow;

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

    /// <summary>
    /// Forks the pipeline execution based on success or failure.
    /// Successful items proceed to the next stage of the pipeline.
    /// Failed items are routed to a recovery step and terminated.
    /// </summary>
    public static IDataflowPipelineBuilder<THead, TNextOut> AddForkingStep<THead, TCurrentOut, TNextOut>(
        this IDataflowPipelineBuilder<THead, TCurrentOut> builder,
        IStep<TCurrentOut, TNextOut> primaryStep,
        IStep<FailedPayload<TCurrentOut>, TNextOut> recoveryStep,
        PipelineStepOptions options = default,
        CancellationToken pipelineToken = default)
    {
        ArgumentNullException.ThrowIfNull(primaryStep);
        ArgumentNullException.ThrowIfNull(recoveryStep);

        // 1. The execution/splitting block
        async Task<StepOutcome<TNextOut, FailedPayload<TCurrentOut>>> ExecuteWithForkAsync(TCurrentOut item)
        {
            try
            {
                var result = await primaryStep.ExecuteAsync(item, pipelineToken).ConfigureAwait(false);
                return StepOutcome<TNextOut, FailedPayload<TCurrentOut>>.Succeeded(result);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                var pipelineEx = new PipelineExecutionException(primaryStep.Name, "Primary step faulted.", ex);
                return StepOutcome<TNextOut, FailedPayload<TCurrentOut>>.Failed(new FailedPayload<TCurrentOut>(item, pipelineEx, primaryStep.Name));
            }
        }

        var splitBlock = new TransformBlock<TCurrentOut, StepOutcome<TNextOut, FailedPayload<TCurrentOut>>>(
            ExecuteWithForkAsync,
            options.ToDataflowOptions(pipelineToken));

        // 2. Success Path: Transforms into the next TNextOut for the main pipeline
        var successTarget = new TransformBlock<StepOutcome<TNextOut, FailedPayload<TCurrentOut>>, TNextOut>(
            outcome => outcome.SuccessPayload!,
            options.ToDataflowOptions(pipelineToken));

        // 3. Failure Path: Executes recovery and terminates (drops data)
        var failureTarget = new TransformBlock<StepOutcome<TNextOut, FailedPayload<TCurrentOut>>, TNextOut>(
            async outcome =>
            {
                await recoveryStep.ExecuteAsync(outcome.FailurePayload, pipelineToken).ConfigureAwait(false);
                return default!; // Discarded
            },
            options.ToDataflowOptions(pipelineToken));

        // 4. Terminate failure path
        failureTarget.LinkTo(DataflowBlock.NullTarget<TNextOut>());

        // 5. Route to targets
        splitBlock.LinkTo(successTarget, new DataflowLinkOptions { PropagateCompletion = true }, outcome => outcome.IsSuccess);
        splitBlock.LinkTo(failureTarget, new DataflowLinkOptions { PropagateCompletion = true }, outcome => !outcome.IsSuccess);

        // 6. Return new builder, with successTarget as the new tailBlock
        // (Assuming DataflowBuilder exposes a constructor to set head/tail)
        return new DataflowPipelineBuilder<THead, TNextOut>(
            headBlock: /* Keep existing head */,
            tailBlock: successTarget,
            logger: null,
            pipelineCancellationToken: pipelineToken);
    }
}

/// <summary>
/// Encapsulates the discriminated outcome of a pipeline step execution, 
/// facilitating conditional routing (branching) between success and failure topologies.
/// </summary>
/// <typeparam name="TSuccess">The type of the payload produced upon successful execution.</typeparam>
/// <typeparam name="TFailure">The type of the failure context produced when an exception is intercepted.</typeparam>
public readonly record struct StepOutcome<TSuccess, TFailure>(
    TSuccess? SuccessPayload,
    TFailure? FailurePayload,
    bool IsSuccess)
{
    public static StepOutcome<TSuccess, TFailure> Succeeded(TSuccess payload) => new(payload, default, true);
    public static StepOutcome<TSuccess, TFailure> Failed(TFailure payload) => new(default, payload, false);
}