using MelloSilveiraTools.Core.Pipelines.Models;
using MelloSilveiraTools.Core.Pipelines.Notifiers;
using Microsoft.Extensions.Logging;

namespace MelloSilveiraTools.Core.Pipelines.Telemetry;

/// <summary>
/// Tracks the execution lifecycle, DAG topology relationships, and item counters (received vs. processed)
/// for a single logical step across a TPL Dataflow pipeline.
/// </summary>
/// <remarks>
/// Architectural Decisions:
/// <list type="bullet">
/// <item><description><b>Step-Level Lifecycle:</b> Emits <see cref="PipelineStepState.Started"/> once when the first item begins execution and <see cref="PipelineStepState.Completed"/> once when the underlying TPL Dataflow block drains all items (<c>block.Completion</c>), rather than falsely marking a step as completed after each individual item.</description></item>
/// <item><description><b>Lock-Free Concurrency:</b> Uses <see cref="Interlocked"/> and <see cref="Volatile"/> operations so steps configured with <c>MaxDegreeOfParallelism &gt; 1</c> can update counters without lock contention.</description></item>
/// <item><description><b>Adaptive Throttling:</b> Emits item updates for every item up to <c>50</c> items (and whenever <c>ProcessedItems == ReceivedItems</c>) to provide immediate visual feedback on low-volume steps, then throttles to every <c>500</c> items on high-frequency streams to avoid flooding NDJSON/SSE channels.</description></item>
/// </list>
/// </remarks>
internal sealed class PipelineStepTracker(
    ILogger logger,
    IPipelineProgressNotifier progressNotifier,
    TimeProvider timeProvider,
    string stepName,
    IReadOnlyList<string> upstreamSteps)
{
    private const int ImmediateNotificationItemLimit = 50;
    private const int HighVolumeNotificationInterval = 500;

    private int _receivedItems;
    private int _processedItems;
    private int _hasStarted;
    private int _hasFaulted;
    private int _hasCompleted;
    private long _startTimestampTicks;

    /// <summary>
    /// Gets the logical name of the pipeline step.
    /// </summary>
    public string StepName => stepName;

    /// <summary>
    /// Gets the OpenTelemetry span name prefixed with <c>Pipeline.Step.</c>.
    /// </summary>
    public string TelemetryName => $"Pipeline.Step.{stepName}";

    /// <summary>
    /// Gets the names of the immediate upstream steps feeding into this step.
    /// </summary>
    public IReadOnlyList<string> UpstreamSteps => upstreamSteps;

    /// <summary>
    /// Gets the output flow node representing items emitted by this step to downstream consumers.
    /// </summary>
    public PipelineFlowNode OutputNode { get; } = new([stepName]);

    /// <summary>
    /// Emits the initial <see cref="PipelineStepState.Pending"/> notification so consumers can construct the full DAG topology upfront.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    public ValueTask NotifyPendingAsync(CancellationToken cancellationToken = default) => NotifyEventAsync(
        PipelineStepState.Pending,
        $"Step '{stepName}' is pending execution.",
        received: 0,
        processed: 0,
        percentage: 0.0,
        cancellationToken: cancellationToken);

    /// <summary>
    /// Increments the count of items queued into this step from upstream and emits a progress update if within notification thresholds.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    public ValueTask OnItemReceivedAsync(CancellationToken cancellationToken = default)
    {
        int received = Interlocked.Increment(ref _receivedItems);
        bool started = Volatile.Read(ref _hasStarted) == 1;

        // Suppress redundant notification for the very first item prior to step start, as OnItemStartedAsync will immediately emit Started.
        if ((!started && received == 1) || !ShouldNotifyItemUpdate(received))
            return ValueTask.CompletedTask;

        int processed = Volatile.Read(ref _processedItems);
        return NotifyEventAsync(
            started ? PipelineStepState.Running : PipelineStepState.Pending,
            started ? $"Running '{stepName}'." : $"Items queued for '{stepName}'.",
            received,
            processed,
            started ? CalculatePercentage(processed, received) : 0.0,
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Records that the step has started processing an item. Emits <see cref="PipelineStepState.Started"/> on the first item only.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async ValueTask<DateTimeOffset> OnItemStartedAsync(CancellationToken cancellationToken = default)
    {
        DateTimeOffset now = timeProvider.GetUtcNow();
        int processed = Volatile.Read(ref _processedItems);
        int received = EnsureReceivedAtLeast(processed + 1);

        if (Interlocked.CompareExchange(ref _hasStarted, 1, 0) == 0)
        {
            Interlocked.Exchange(ref _startTimestampTicks, now.UtcTicks);
            logger.LogInformation("{StartTime:O} - Starting '{Name}'.", now, TelemetryName);

            await NotifyEventAsync(
                PipelineStepState.Started,
                $"Starting '{stepName}'.",
                received,
                processed,
                CalculatePercentage(processed, received),
                timestamp: now,
                cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        return now;
    }

    /// <summary>
    /// Notifies downstream trackers that this step has emitted an output item.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    public ValueTask OnItemEmittedAsync(CancellationToken cancellationToken = default) => OutputNode.NotifyItemEmittedAsync(cancellationToken);

    /// <summary>
    /// Records that a single input item has been completely processed by this step and emits <see cref="PipelineStepState.Running"/>.
    /// </summary>
    /// <param name="details">Optional contextual details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public ValueTask OnItemProcessedAsync(string? details = null, CancellationToken cancellationToken = default)
    {
        int processed = Interlocked.Increment(ref _processedItems);
        int received = EnsureReceivedAtLeast(processed);

        if (!ShouldNotifyItemUpdate(processed) && processed != received)
            return ValueTask.CompletedTask;

        return NotifyEventAsync(
            PipelineStepState.Running,
            $"Running '{stepName}'.",
            received,
            processed,
            CalculatePercentage(processed, received),
            details,
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Emits a periodic <see cref="PipelineStepState.Running"/> notification while streaming items from a single input.
    /// </summary>
    /// <param name="emittedCount">The number of items emitted so far by the streaming step.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public ValueTask OnStreamingProgressAsync(int emittedCount, CancellationToken cancellationToken = default)
    {
        if (emittedCount % HighVolumeNotificationInterval != 0)
            return ValueTask.CompletedTask;

        int processed = Volatile.Read(ref _processedItems);
        int received = EnsureReceivedAtLeast(Math.Max(1, processed));

        return NotifyEventAsync(
            PipelineStepState.Running,
            $"Streaming '{stepName}'.",
            received,
            processed,
            CalculatePercentage(processed, received),
            $"Items emitted: {emittedCount}",
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Records that the step encountered an unhandled exception and emits <see cref="PipelineStepState.Faulted"/>.
    /// </summary>
    /// <param name="itemStartTime">The timestamp when the failing item started executing.</param>
    /// <param name="ex">The unhandled exception.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public ValueTask OnStepFaultedAsync(DateTimeOffset itemStartTime, Exception ex, CancellationToken cancellationToken = default)
    {
        Interlocked.Exchange(ref _hasFaulted, 1);

        DateTimeOffset endTime = timeProvider.GetUtcNow();
        TimeSpan duration = endTime - itemStartTime;
        int processed = Volatile.Read(ref _processedItems);
        int received = EnsureReceivedAtLeast(Math.Max(1, processed));

        logger.LogError(ex, "{EndTime:O} - Duration: {Duration} - Failed '{Name}'.", endTime, duration, TelemetryName);
        return NotifyEventAsync(
            PipelineStepState.Faulted,
            $"Failed '{stepName}'.",
            received,
            processed,
            CalculatePercentage(processed, received),
            ex.Message,
            endTime,
            cancellationToken);
    }

    /// <summary>
    /// Records that the underlying TPL Dataflow block has drained all items and completed execution,
    /// emitting a single <see cref="PipelineStepState.Completed"/> notification with 100% progress.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task OnBlockCompletedAsync(CancellationToken cancellationToken = default)
    {
        if (Volatile.Read(ref _hasFaulted) == 1 || Interlocked.CompareExchange(ref _hasCompleted, 1, 0) != 0)
            return;

        DateTimeOffset endTime = timeProvider.GetUtcNow();
        long startTicks = Volatile.Read(ref _startTimestampTicks);
        TimeSpan duration = startTicks > 0
            ? endTime - new DateTimeOffset(startTicks, TimeSpan.Zero)
            : TimeSpan.Zero;

        int processed = Volatile.Read(ref _processedItems);
        Interlocked.Exchange(ref _receivedItems, processed);

        logger.LogInformation("{EndTime:O} - Duration: {Duration} - Successfully completed '{Name}'.", endTime, duration, TelemetryName);
        await NotifyEventAsync(
            PipelineStepState.Completed,
            $"Successfully completed '{stepName}'.",
            received: processed,
            processed: processed,
            percentage: 100.0,
            details: $"Duration: {duration}",
            timestamp: endTime,
            cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    private ValueTask NotifyEventAsync(
        PipelineStepState state,
        string description,
        int received,
        int processed,
        double percentage,
        string? details = null,
        DateTimeOffset? timestamp = null,
        CancellationToken cancellationToken = default) => progressNotifier.NotifyAsync(
        new PipelineProgressEvent
        {
            StepName = stepName,
            Description = description,
            State = state,
            Timestamp = timestamp ?? timeProvider.GetUtcNow(),
            UpstreamSteps = upstreamSteps,
            ReceivedItems = received,
            ProcessedItems = processed,
            ProgressPercentage = percentage,
            Details = details
        },
        cancellationToken);

    private int EnsureReceivedAtLeast(int minValue)
    {
        while (true)
        {
            int current = Volatile.Read(ref _receivedItems);
            if (current >= minValue)
                return current;

            if (Interlocked.CompareExchange(ref _receivedItems, minValue, current) == current)
                return minValue;
        }
    }

    private static bool ShouldNotifyItemUpdate(int count) => count <= ImmediateNotificationItemLimit || count % HighVolumeNotificationInterval == 0;

    private static double CalculatePercentage(int processed, int received) => received > 0 ? Math.Round(Math.Min(100.0, (double)processed / received * 100.0), 2) : 0.0;
}
