using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Models;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData;

/// <summary>
/// Handles the ingestion, validation, and physical phase segmentation of experimental raw data streams.
/// </summary>
public interface IExperimentalDataProcessingPipeline
{
    /// <summary>
    /// Ingests strain and stress data streams and processes them through a TPL Dataflow pipeline,
    /// writing the valid processed points to a CSV file and executing numerical simulations.
    /// </summary>
    /// <param name="input">The experimental data processing configuration and data streams.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A <see cref="Result{T}"/> containing the unique identifier generated for the processing run.</returns>
    Task<Result<string>> ProcessAsync(ExperimentalDataProcessingInput input, CancellationToken cancellationToken = default);
}
