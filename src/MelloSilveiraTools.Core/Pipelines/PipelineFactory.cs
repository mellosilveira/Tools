using MelloSilveiraTools.Core.Pipelines.Dataflow;
using MelloSilveiraTools.Core.Pipelines.Fluent;
using MelloSilveiraTools.Core.Pipelines.Models;
using MelloSilveiraTools.Core.Pipelines.Notifiers;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks.Dataflow;

namespace MelloSilveiraTools.Core.Pipelines;

/// <summary>
/// Serves as the primary factory for bootstrapping execution topologies.
/// Encapsulates the initialization logic for both the continuous push-based Dataflow network 
/// and the strongly-typed pull-based Fluent request-response pipeline.
/// </summary>
/// <param name="progressNotifier">Mandatory pipeline progress notifier.</param>
/// <param name="timeProvider">The time provider instance used for timing and timeouts.</param>
public class PipelineFactory(IPipelineProgressNotifier progressNotifier, TimeProvider timeProvider)
{
    /// <summary>
    /// Bootstraps a continuous TPL Dataflow pipeline topology by instantiating a <see cref="BufferBlock{T}"/> 
    /// as the ingestion head. Enforces bounded capacity backpressure, binds the mandatory progress notifier, 
    /// and injects the unified cancellation graph that dictates the lifecycle of all subsequent blocks.
    /// </summary>
    /// <typeparam name="T">The immutable payload type ingested at the pipeline head.</typeparam>
    /// <param name="logger">Structured logging provider for tracking block transitions and telemetry.</param>
    /// <param name="initialBufferSize">The maximum bounded capacity of the ingestion buffer block, defaulting to 10,000 items.</param>
    /// <param name="cancellationToken">The unified cancellation token propagated down the entire Dataflow network.</param>
    /// <returns>A fluent builder instance anchored to the initial buffer head.</returns>
    public IDataflowPipelineBuilder<T, T> CreateDataflow<T>(
        ILogger logger,
        int initialBufferSize = 10000,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(progressNotifier);

        BufferBlock<T> buffer = new(new DataflowBlockOptions { BoundedCapacity = initialBufferSize, CancellationToken = cancellationToken });
        return new DataflowPipelineBuilder<T, T>(
            logger,
            progressNotifier,
            buffer,
            buffer,
            null,
            timeProvider,
            pipelineCancellationToken: cancellationToken);
    }

    /// <summary>
    /// Bootstraps a strongly-typed, pull-based Fluent pipeline builder.
    /// Binds the initial input type to the builder's root state to guarantee compile-time safety 
    /// before transitioning into the type-erased internal execution engine.
    /// </summary>
    /// <typeparam name="T">The immutable root input type validated at compile time.</typeparam>
    /// <returns>A fluent builder instance ready to receive step linkages.</returns>
    public IFluentPipelineBuilder<T, T> StartFluent<T>() => new FluentPipelineBuilder<T, T>();
}
