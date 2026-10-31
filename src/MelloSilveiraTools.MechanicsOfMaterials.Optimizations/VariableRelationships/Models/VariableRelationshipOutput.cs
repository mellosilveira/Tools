using MelloSilveiraTools.Core.Managers.File.Models;
using FileMetadata = MelloSilveiraTools.Core.Managers.File.Models.StorageEntryMetadata;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.VariableRelationships.Models;

/// <summary>
/// Encapsulates the results of a variable relationship exploration pipeline, containing the execution identifier,
/// valid parameter combination points, and persistent CSV file metadata.
/// </summary>
/// <param name="Identifier">Unique run or correlation identifier for the relationship analysis execution.</param>
/// <param name="Combinations">The collection of evaluated parameter combinations satisfying the stress and tolerance criteria.</param>
/// <param name="CsvMetadata">The persistent storage entry metadata describing the generated CSV artifact.</param>
public sealed record VariableRelationshipOutput(
    string Identifier,
    IReadOnlyList<VariableCombinationPoint> Combinations,
    FileMetadata CsvMetadata);
