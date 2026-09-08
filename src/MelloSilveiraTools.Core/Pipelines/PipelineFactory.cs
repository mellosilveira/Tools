using MelloSilveiraTools.Core.Pipelines.Dataflow;
using MelloSilveiraTools.Core.Pipelines.Fluent;
using MelloSilveiraTools.Core.Pipelines.Models;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks.Dataflow;

namespace MelloSilveiraTools.Core.Pipelines;

/// <summary>
/// Serves as the primary static factory for bootstrapping execution topologies.
/// Encapsulates the initialization logic for both the continuous push-based Dataflow network 
/// and the strongly-typed pull-based Fluent request-response pipeline.
/// </summary>
public static class PipelineFactory
{
    /// <summary>
    /// Bootstraps a continuous TPL Dataflow pipeline topology by instantiating a <see cref="BufferBlock{T}"/> 
    /// as the ingestion head. Enforces bounded capacity backpressure and injects the unified cancellation graph 
    /// that dictates the lifecycle of all subsequent blocks.
    /// </summary>
    /// <typeparam name="T">The immutable payload type ingested at the pipeline head.</typeparam>
    /// <param name="logger">Structured logging provider for tracking block transitions and telemetry.</param>
    /// <param name="initialBufferSize">The maximum bounded capacity of the ingestion buffer block, defaulting to 10,000 items.</param>
    /// <param name="retryOptions"></param>
    /// <param name="cancellationToken">The unified cancellation token propagated down the entire Dataflow network.</param>
    /// <returns>A fluent builder instance anchored to the initial buffer head.</returns>
    public static IDataflowPipelineBuilder<T, T> StartDataflow<T>(
        ILogger logger,
        int initialBufferSize = 10000,
        RetryOptions? retryOptions = null,
        CancellationToken cancellationToken = default)
    {
        BufferBlock<T> buffer = new(new DataflowBlockOptions { BoundedCapacity = initialBufferSize, CancellationToken = cancellationToken });
        return new DataflowPipelineBuilder<T, T>(logger, buffer, buffer, null, retryOptions, cancellationToken);
    }

    /// <summary>
    /// Bootstraps a strongly-typed, pull-based Fluent pipeline builder.
    /// Binds the initial input type to the builder's root state to guarantee compile-time safety 
    /// before transitioning into the type-erased internal execution engine.
    /// </summary>
    /// <typeparam name="T">The immutable root input type validated at compile time.</typeparam>
    /// <returns>A fluent builder instance ready to receive step linkages.</returns>
    public static IFluentPipelineBuilder<T, T> StartFluent<T>() => new FluentPipelineBuilder<T, T>();
}