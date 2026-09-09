using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Models.CurveFitting;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Models.ExperimentalData;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Services.ExperimentalData;

/// <summary>
/// Handles the ingestion, validation, and physical phase segmentation of experimental raw data streams.
/// </summary>
public interface IExperimentalDataService
{
    /// <summary>
    /// Ingests strain and stress data streams and processes them through a TPL Dataflow pipeline,
    /// writing the valid processed points to a CSV file and returning the physical curve segments categorized by deformation phases.
    /// </summary>
    /// <param name="identifier">A unique identifier prefix used to name the output file.</param>
    /// <param name="outputFileUri">The directory path where the output file will be written.</param>
    /// <param name="strainStream">A stream containing experimental strain values across time.</param>
    /// <param name="stressStream">A stream containing experimental stress values across time.</param>
    /// <param name="options">Options controlling tolerances, buffer sizing, and downsampling thresholds.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A <see cref="Result{T}"/> containing the generated output file name and the categorized <see cref="CurveSegment"/> array.</returns>
    Task<Result<(string OutputFileName, CurveSegment[] CurveSegments)>> ProcessAsync(
        string identifier,
        string outputFileUri,
        Stream strainStream,
        Stream stressStream,
        ExperimentalDataProcessingOptions? options = null,
        CancellationToken cancellationToken = default);
}

