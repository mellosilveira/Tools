using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData;

/// <summary>
/// Handles the ingestion, validation, and physical phase segmentation of experimental raw data streams.
/// </summary>
public interface IExperimentalDataService
{

    /// <summary>
    /// Ingests strain and stress data streams and processes them through a TPL Dataflow pipeline,
    /// writing the valid processed points to a CSV file and returning the physical model parameters.
    /// </summary>
    /// <param name="input"></param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A <see cref="Result{T}"/> containing the generated output file name and the array of <see cref="ConstitutiveParameters"/>.</returns>
    Task<Result<(string OutputFileName, ConstitutiveParameters[] Parameters)>> ProcessAsync(ExperimentalDataProcessingInput input, CancellationToken cancellationToken = default);
}

