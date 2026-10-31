using MelloSilveiraTools.Core.Managers.File.Models;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.VariableRelationships.Models;

/// <summary>
/// Pipeline payload aggregating evaluated valid combinations, persistent CSV metadata,
/// and correlation identifiers for execution persistence.
/// </summary>
/// <param name="Identifier">The unique correlation identifier for the pipeline run.</param>
/// <param name="Input">The input specification used to execute the analysis.</param>
/// <param name="Combinations">The evaluated parameter combinations that satisfied the stress and tolerance criteria.</param>
/// <param name="CsvMetadata">The storage metadata for the generated CSV artifact.</param>
public sealed record VariableRelationshipPersistenceInput(
    string Identifier,
    VariableRelationshipInput Input,
    IReadOnlyList<VariableCombinationPoint> Combinations,
    StorageEntryMetadata CsvMetadata);
