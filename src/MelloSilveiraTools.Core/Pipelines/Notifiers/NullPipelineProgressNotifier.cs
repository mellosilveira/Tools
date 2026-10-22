using MelloSilveiraTools.Core.Pipelines.Models;

namespace MelloSilveiraTools.Core.Pipelines.Notifiers;

/// <summary>
/// A no-op implementation of <see cref="IPipelineProgressNotifier"/> that discards all progress notifications with zero allocation.
/// </summary>
public sealed class NullPipelineProgressNotifier : IPipelineProgressNotifier
{
    /// <summary>
    /// Gets the shared singleton instance of <see cref="NullPipelineProgressNotifier"/>.
    /// </summary>
    public static NullPipelineProgressNotifier Instance { get; } = new();

    private NullPipelineProgressNotifier() { }

    /// <inheritdoc/>
    public ValueTask NotifyAsync(PipelineProgressEvent progressEvent, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;

    /// <inheritdoc/>
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
