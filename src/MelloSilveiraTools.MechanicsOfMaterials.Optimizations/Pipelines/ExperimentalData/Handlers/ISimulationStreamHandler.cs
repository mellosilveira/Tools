using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Handlers;

/// <summary>
/// Contract for reactive stream handlers that process <see cref="MechanicalModelOutput"/> sequentially.
/// </summary>
/// <typeparam name="TResult">The aggregated result type emitted when processing completes.</typeparam>
public interface ISimulationStreamHandler<TResult> : IAsyncDisposable
{
    /// <summary>
    /// Processes a single simulation output data point emitted along the numerical simulation timeline.
    /// </summary>
    /// <param name="output">The calculated model output for the current time step.</param>
    /// <param name="index">The zero-based sequential index of the current data point.</param>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous operation.</returns>
    ValueTask OnNextAsync(MechanicalModelOutput output, int index);

    /// <summary>
    /// Completes the stream handling process and returns the aggregated result.
    /// </summary>
    /// <returns>The computed or summarized result of the simulation stream.</returns>
    ValueTask<TResult> CompleteAsync();
}
