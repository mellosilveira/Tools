using MelloSilveiraTools.Core.Pipelines.Models;
using MelloSilveiraTools.Core.Pipelines.Steps;
using MelloSilveiraTools.Core.Pipelines.Telemetry;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks.Dataflow;

namespace MelloSilveiraTools.Core.Pipelines.Dataflow;

/// <summary>
/// Sealed execution engine representing the finalized TPL Dataflow graph.
/// Exposes inputs, triggers completion, and awaits termination while automatically disposing registered steps on shutdown.
/// </summary>
internal sealed class DataflowPipeline<TIn>(
    PipelineBuildContext<TIn> context,
    Task completionTask)
    : IDataflowPipeline<TIn>
{
    private int _receivedItems;
    private int _processedItems;

    /// <inheritdoc/>
    public IReadOnlyList<(string[] UpstreamSteps, string StepName)> Topology
        => [.. context.Nodes.Select(node => (node.PreviousStepNames, node.CurrentStep.Name))];

    /// <inheritdoc/>
    public async Task<bool> SendAsync(TIn item, CancellationToken cancellationToken = default)
    {
        int received = Interlocked.Increment(ref _receivedItems);
        string? itemDetails = ObservabilityExtensions.SerializeDetails(new { item });

        await NotifyIngestionEventAsync("Started ingestion of input item into pipeline.", PipelineStepState.Started, received, Volatile.Read(ref _processedItems), itemDetails, cancellationToken).ConfigureAwait(false);

        bool accepted = await context.HeadBlock.SendAsync(item, cancellationToken).ConfigureAwait(false);
        if (!accepted)
        {
            await NotifyIngestionEventAsync("Failed to ingest item into pipeline (head block rejected input).", PipelineStepState.Faulted, received, Volatile.Read(ref _processedItems), itemDetails, cancellationToken).ConfigureAwait(false);
            context.Logger.LogError("Failed to ingest item into the pipeline. Item: {@Item}", item);
            return false;
        }

        await NotifyIngestionEventAsync("Completed ingestion of input item into pipeline.", PipelineStepState.Completed, received, Interlocked.Increment(ref _processedItems), itemDetails, cancellationToken).ConfigureAwait(false);
        return true;
    }

    /// <inheritdoc/>
    public void Complete()
    {
        context.Logger.LogInformation("Pipeline completion invoked. Draining buffered messages and propagating completion state.");
        context.HeadBlock.Complete();
    }

    /// <inheritdoc/>
    public Task Completion => completionTask;

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        Complete();
        try
        {
            await Completion.ConfigureAwait(false);
        }
        finally
        {
            HashSet<IPipelineStep> distinctSteps = [];
            foreach ((_, IPipelineStep currentStep) in context.Nodes)
                distinctSteps.Add(currentStep);

            foreach (IPipelineStep step in distinctSteps)
            {
                if (step is IAsyncDisposable asyncDisposableStep)
                    await asyncDisposableStep.DisposeAsync().ConfigureAwait(false);
                else if (step is IDisposable disposableStep)
                    disposableStep.Dispose();
            }
        }
    }

    private ValueTask NotifyIngestionEventAsync(
        string description,
        PipelineStepState state,
        int receivedItems,
        int processedItems,
        string? details,
        CancellationToken cancellationToken)
        => context.ProgressNotifier.NotifyAsync(new PipelineProgressEvent
        {
            PipelineIdentifier = context.PipelineIdentifier,
            StepName = $"{nameof(DataflowPipeline<>)}.{nameof(SendAsync)}",
            Description = description,
            State = state,
            Timestamp = context.TimeProvider.GetUtcNow(),
            ReceivedItems = receivedItems,
            ProcessedItems = processedItems,
            Details = details
        }, cancellationToken);
}
