using Microsoft.AspNetCore.Http;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Commands.Upload;

/// <summary>
/// Request payload for uploading a pair of experimental strain and stress CSV files.
/// </summary>
public sealed record UploadExperimentalDataRequest
{
    /// <summary>
    /// Raw experimental strain CSV file.
    /// </summary>
    public required IFormFile StrainFile { get; init; }

    /// <summary>
    /// Raw experimental stress CSV file.
    /// </summary>
    public required IFormFile StressFile { get; init; }
}
