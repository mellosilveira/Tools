using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.VariableRelationships.Models;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.VariableRelationships.Commands;

/// <summary>
/// Response payload returned by <see cref="ExecuteVariableRelationshipCommand"/> containing the variable relationship exploration outcome.
/// </summary>
/// <param name="Output">The variable relationship exploration result payload.</param>
public record ExecuteVariableRelationshipResponse(VariableRelationshipOutput Output);
