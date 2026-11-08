using MelloSilveiraTools.Core.Managers.File.Models;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.Pipelines.Models;

/// <summary>
/// Strongly-typed input payload for <see cref="Steps.ParametricSweepPersistenceStep"/>,
/// aggregating the run identifier, sweep configuration, and metadata of all generated partitioned CSV files.
/// </summary>
/// <param name="ExternalIdentifier">Unique run identifier for the parametric sweep execution.</param>
/// <param name="SweepInput">The input configuration of the parametric sweep.</param>
/// <param name="GeneratedFiles">Collection of swept variable names paired with the storage entry metadata of their generated CSV files.</param>
public sealed record ParametricSweepPersistenceInput(
    string ExternalIdentifier,
    ParametricSweepPipelineInput SweepInput,
    IReadOnlyCollection<(string VariableName, StorageEntryMetadata FileMetadata)> GeneratedFiles);
