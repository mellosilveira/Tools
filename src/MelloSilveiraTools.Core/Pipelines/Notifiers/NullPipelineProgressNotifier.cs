using MelloSilveiraTools.Core.Pipelines.Models;
using System.Runtime.CompilerServices;

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
    public ValueTask NotifyAsync(string analysisIdentifier, PipelineProgressEvent progressEvent, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;

    /// <inheritdoc/>
    public async IAsyncEnumerable<PipelineProgressEvent> ReadAllAsync(string analysisIdentifier, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
        yield break;
    }

    /// <inheritdoc/>
    public bool TryComplete(string analysisIdentifier, Exception? error = null) => true;

    /// <inheritdoc/>
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
