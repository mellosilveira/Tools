using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Core.Validators;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Commands.Upload;

/// <summary>
/// Validates <see cref="UploadExperimentalDataRequest"/> payloads prior to storing experimental CSV files.
/// </summary>
/// <param name="experimentalDataSettings">Experimental data settings providing the maximum allowed CSV file size.</param>
public sealed class UploadExperimentalDataValidator(ExperimentalDataSettings experimentalDataSettings)
    : IValidator<UploadExperimentalDataRequest>
{
    /// <inheritdoc/>
    public Result Validate(UploadExperimentalDataRequest value)
    {
        if (value.StrainFile is null || value.StressFile is null)
            return Result.CreateBadRequest("Both 'strainFile' and 'stressFile' must be provided.");

        if (value.StrainFile.Length <= 0 || value.StressFile.Length <= 0)
            return Result.CreateBadRequest("Uploaded CSV files cannot be empty.");

        long maxBytes = experimentalDataSettings.MaxCsvSizeBytes;
        if (maxBytes > 0 && (value.StrainFile.Length > maxBytes || value.StressFile.Length > maxBytes))
            return Result.CreateBadRequest($"Each uploaded CSV file must not exceed {maxBytes / (1024 * 1024)} MB.");

        if (!value.StrainFile.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase)
            || !value.StressFile.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            return Result.CreateBadRequest("Only '.csv' files are supported.");

        return Result.CreateSuccessOk();
    }
}
