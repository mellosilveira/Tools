using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Models.CurveFitting;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Models.ExperimentalData;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Services.ExperimentalData;

/// <summary>
/// Handles the validation and segmentation of the experimental data file.
/// </summary>
public interface IExperimentalDataService
{
    Task<Result<CurveSegment[]>> ProcessAsync(string identifier, string outputFileUri, Stream strainStream, Stream stressStream, ExperimentalDataProcessingOptions options, CancellationToken cancellationToken);
}
