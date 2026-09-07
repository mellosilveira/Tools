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
    /// returning the physical curve segments categorized by deformation phases.
    /// </summary>
    /// <param name="strainStream">A stream containing experimental strain values across time.</param>
    /// <param name="stressStream">A stream containing experimental stress values across time.</param>
    /// <param name="options">Options controlling tolerances, buffer sizing, and downsampling thresholds.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A <see cref="Result{T}"/> containing the categorized <see cref="CurveSegment"/> array.</returns>
    Task<Result<CurveSegment[]>> ProcessAsync(
        Stream strainStream,
        Stream stressStream,
        ExperimentalDataProcessingOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously streams categorized data points from strain and stress streams.
    /// </summary>
    /// <param name="strainStream">A stream containing experimental strain values across time.</param>
    /// <param name="stressStream">A stream containing experimental stress values across time.</param>
    /// <param name="options">Options controlling tolerances and buffer sizing.</param>
    /// <param name="cancellationToken">A token to cancel the streaming operation.</param>
    /// <returns>An async enumerable of <see cref="SegmentedDataPoint"/> instances.</returns>
    IAsyncEnumerable<SegmentedDataPoint> SegmentPointsAsync(
        Stream strainStream,
        Stream stressStream,
        ExperimentalDataProcessingOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Extracts physical deformation segments from a sliding buffer of experimental data points.
    /// </summary>
    /// <param name="currentType">The active segment type preceding the current buffer.</param>
    /// <param name="points">An array of buffered experimental points.</param>
    /// <param name="count">The valid number of items inside <paramref name="points"/>.</param>
    /// <param name="options">Processing options.</param>
    /// <returns>A list of extracted segment types and their corresponding point slices.</returns>
    List<(SegmentType, ArraySegment<ExperimentalDataPoint>)> ExtractSegments(
        SegmentType currentType,
        ExperimentalDataPoint[] points,
        int count,
        ExperimentalDataProcessingOptions? options = null);
}

