using MelloSilveiraTools.Core.Models;

namespace MelloSilveiraTools.Core.Pipelines;

/// <summary>
/// Defines a generic contract for domain processing pipelines.
/// </summary>
/// <typeparam name="TInput">The type of input configuration consumed by the pipeline.</typeparam>
/// <typeparam name="TOutput">The type of output data returned upon pipeline completion.</typeparam>
public interface IPipeline<in TInput, TOutput>
{
    /// <summary>
    /// Builds and executes the processing pipeline for the specified input.
    /// </summary>
    /// <param name="input">The pipeline input configuration.</param>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A <see cref="Result{TOutput}"/> containing the result of the pipeline execution.</returns>
    Task<Result<TOutput>> ProcessAsync(TInput input, CancellationToken cancellationToken = default);
}
