using MelloSilveiraTools.Core.Managers.File;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.Models;

/// <summary>
/// Strongly-typed input payload for <see cref="Pipelines.SensitivityAnalysis.Steps.ParametricSweepPersistenceStep"/>,
/// aggregating the run identifier, sweep configuration, and metadata of all generated CSV files.
/// </summary>
/// <param name="ExternalIdentifier">Unique run identifier for the parametric sweep execution.</param>
/// <param name="SweepInput">The input configuration of the parametric sweep.</param>
/// <param name="GeneratedFilesMetadata">Collection of file metadata for all partitioned CSV files produced during the sweep.</param>
public sealed record ParametricSweepPersistenceInput(
    string ExternalIdentifier,
    ParametricSweepInput SweepInput,
    IReadOnlyCollection<FileMetadata> GeneratedFilesMetadata);
