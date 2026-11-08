using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Models.Range;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.Pipelines.Models;

/// <summary>
/// Associates the numerical range parameters of a swept variable with the database identifier
/// of the storage entry metadata record for its generated sensitivity CSV file.
/// </summary>
/// <param name="Range">The numerical range parameters applied to sweep the variable.</param>
/// <param name="OutputFileMetadataId">The primary key identifier of the persisted <c>stored_files</c> metadata record.</param>
public readonly record struct SweptVariableFileMetadata(
    RangeParameters Range,
    long OutputFileMetadataId);
