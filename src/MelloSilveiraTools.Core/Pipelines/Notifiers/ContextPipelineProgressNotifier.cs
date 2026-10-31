using MelloSilveiraTools.Core.Pipelines.Models;

namespace MelloSilveiraTools.Core.Pipelines.Notifiers;

/// <summary>
/// Context-bound adapter for <see cref="IPipelineProgressNotifier"/> that automatically enriches emitted events
/// with a designated <see cref="AnalysisIdentifier"/> and routes notifications to that specific channel in the singleton notifier.
/// </summary>
/// <param name="innerNotifier">The underlying progress notifier instance.</param>
/// <param name="analysisIdentifier">Mandatory unique business identifier correlating the analysis or pipeline execution.</param>
public sealed class ContextPipelineProgressNotifier(
    IPipelineProgressNotifier innerNotifier,
    string analysisIdentifier)
    : IPipelineProgressNotifier
{
    /// <summary>
    /// Gets the unique analysis identifier bound to this context.
    /// </summary>
    public string AnalysisIdentifier { get; } = analysisIdentifier;

    /// <inheritdoc/>
    public ValueTask NotifyAsync(string analysisIdentifier, PipelineProgressEvent progressEvent, CancellationToken cancellationToken = default)
    {
        string targetIdentifier = string.IsNullOrWhiteSpace(analysisIdentifier) ? AnalysisIdentifier : analysisIdentifier;
        return innerNotifier.NotifyAsync(targetIdentifier, progressEvent with { AnalysisIdentifier = targetIdentifier }, cancellationToken);
    }

    /// <inheritdoc/>
    public IAsyncEnumerable<PipelineProgressEvent> ReadAllAsync(string analysisIdentifier, CancellationToken cancellationToken = default)
    {
        string targetIdentifier = string.IsNullOrWhiteSpace(analysisIdentifier) ? AnalysisIdentifier : analysisIdentifier;
        return innerNotifier.ReadAllAsync(targetIdentifier, cancellationToken);
    }

    /// <inheritdoc/>
    public bool TryComplete(string analysisIdentifier, Exception? error = null)
    {
        string targetIdentifier = string.IsNullOrWhiteSpace(analysisIdentifier) ? AnalysisIdentifier : analysisIdentifier;
        return innerNotifier.TryComplete(targetIdentifier, error);
    }

    /// <inheritdoc/>
    public ValueTask DisposeAsync()
    {
        innerNotifier.TryComplete(AnalysisIdentifier);
        return ValueTask.CompletedTask;
    }
}
