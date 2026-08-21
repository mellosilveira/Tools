namespace MelloSilveiraTools.Core.Pipelines;

/// <summary>
/// Contract for an isolated processing step within the pipeline.
/// </summary>
/// <typeparam name="TIn">The expected input type for this step.</typeparam>
/// <typeparam name="TOut">The resulting output type of this step.</typeparam>
public interface IStep<TIn, TOut>
{
    Task<TOut> ExecuteAsync(TIn input, CancellationToken cancellationToken = default);
}
