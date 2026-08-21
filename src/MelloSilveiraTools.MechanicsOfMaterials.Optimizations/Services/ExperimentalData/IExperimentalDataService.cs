using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Models.CurveFitting;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Models.ExperimentalData;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Services.ExperimentalData;

/// <summary>
/// Handles the validation and segmentation of the experimental data file.
/// </summary>
public interface IExperimentalDataService
{
    Task<Result<(string OutputFileName, CurveSegment[] CurveSegments)>> ProcessAsync(string identifier, string outputFileUri, Stream strainStream, Stream stressStream, ExperimentalDataProcessingOptions? options = null, CancellationToken cancellationToken = default);

    IAsyncEnumerable<SegmentedDataPoint> SegmentPointsAsync(Stream strainStream, Stream stressStream, ExperimentalDataProcessingOptions? options = null, CancellationToken cancellationToken = default);

    List<(SegmentType, ArraySegment<ExperimentalDataPoint>)> ExtractSegments(SegmentType currentType, ExperimentalDataPoint[] points, int count, ExperimentalDataProcessingOptions? options = null);
}
