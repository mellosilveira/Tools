using MelloSilveiraTools.Core.Files.Models;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Commands.Upload;

/// <summary>
/// Response payload containing canonical storage locations and persisted metadata IDs for uploaded CSV files.
/// </summary>
public sealed record UploadExperimentalDataResultData
{
    /// <summary>
    /// Unique correlation identifier assigned to the uploaded pair.
    /// </summary>
    public required string UploadIdentifier { get; init; }

    /// <summary>
    /// Canonical storage location for the uploaded strain CSV.
    /// </summary>
    public required StorageEntryMetadata StrainFileMetadata { get; init; }

    /// <summary>
    /// Canonical storage location for the uploaded stress CSV.
    /// </summary>
    public required StorageEntryMetadata StressFileMetadata { get; init; }

    /// <summary>
    /// Database ID of the stored strain file metadata record.
    /// </summary>
    public required long StrainStorageEntryId { get; init; }

    /// <summary>
    /// Database ID of the stored stress file metadata record.
    /// </summary>
    public required long StressStorageEntryId { get; init; }
}
