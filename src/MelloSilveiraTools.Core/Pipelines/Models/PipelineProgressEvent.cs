namespace MelloSilveiraTools.Core.Pipelines.Models;

/// <summary>
/// Represents an immutable real-time notification emitted during the execution of a pipeline step,
/// including DAG topology correlation (<see cref="UpstreamSteps"/>) and item-level progress counters.
/// </summary>
public sealed record PipelineProgressEvent
{
    /// <summary>
    /// Gets the unique identifier or logical name of the pipeline step emitting the event.
    /// </summary>
    public required string StepName { get; init; }

    /// <summary>
    /// Gets a human-readable description of the current step activity.
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// Gets the lifecycle state of the step (<see cref="PipelineStepState"/>).
    /// </summary>
    public required PipelineStepState State { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when the progress event was generated.
    /// </summary>
    public required DateTimeOffset Timestamp { get; init; }

    /// <summary>
    /// Gets the logical names of the immediate upstream steps feeding into this step in the pipeline DAG.
    /// Empty for root/ingestion steps.
    /// </summary>
    public IReadOnlyList<string> UpstreamSteps { get; init; } = [];

    /// <summary>
    /// Gets the cumulative number of items that have arrived at (been queued into) this step so far.
    /// Enables visual representations of queued/pending items (e.g., gray progress bar segment).
    /// </summary>
    public int ReceivedItems { get; init; }

    /// <summary>
    /// Gets the cumulative number of items that have been processed by this step so far.
    /// Enables visual representations of completed items (e.g., green progress bar segment).
    /// </summary>
    public int ProcessedItems { get; init; }

    /// <summary>
    /// Gets the estimated completion percentage (0.0 to 100.0) of this step based on <see cref="ProcessedItems"/> and <see cref="ReceivedItems"/>.
    /// </summary>
    public double ProgressPercentage { get; init; }

    /// <summary>
    /// Gets optional diagnostic, contextual, or execution details serialized as a JSON string (e.g., error payloads, item counters, or execution metrics).
    /// </summary>
    public string? Details { get; init; }

    /// <inheritdoc/>
    /// <remarks>
    /// Architectural Decision: Overridden explicitly because the compiler-synthesized <c>record</c> equality uses
    /// <see cref="EqualityComparer{T}.Default"/> on properties, which evaluates <see cref="UpstreamSteps"/> (<see cref="IReadOnlyList{T}"/>)
    /// by reference rather than element-by-element sequence equality (<see cref="Enumerable.SequenceEqual{TSource}(IEnumerable{TSource}, IEnumerable{TSource}, IEqualityComparer{TSource}?)"/>).
    /// </remarks>
    public bool Equals(PipelineProgressEvent? other)
    {
        if (other is null)
            return false;
        if (ReferenceEquals(this, other))
            return true;

        return string.Equals(StepName, other.StepName, StringComparison.Ordinal)
            && string.Equals(Description, other.Description, StringComparison.Ordinal)
            && State == other.State
            && Timestamp.Equals(other.Timestamp)
            && ReceivedItems == other.ReceivedItems
            && ProcessedItems == other.ProcessedItems
            && ProgressPercentage.Equals(other.ProgressPercentage)
            && string.Equals(Details, other.Details, StringComparison.Ordinal)
            && UpstreamSteps.SequenceEqual(other.UpstreamSteps, StringComparer.Ordinal);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        HashCode hash = new();
        hash.Add(StepName, StringComparer.Ordinal);
        hash.Add(Description, StringComparer.Ordinal);
        hash.Add(State);
        hash.Add(Timestamp);
        hash.Add(ReceivedItems);
        hash.Add(ProcessedItems);
        hash.Add(ProgressPercentage);
        hash.Add(Details, StringComparer.Ordinal);
        for (int i = 0; i < UpstreamSteps.Count; i++)
        {
            hash.Add(UpstreamSteps[i], StringComparer.Ordinal);
        }

        return hash.ToHashCode();
    }
}
