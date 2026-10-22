namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.Commands;

/// <summary>
/// Response payload returned by <see cref="ExecuteParametricSweepCommand"/> containing the unique sweep identifier.
/// </summary>
/// <param name="Identifier">The external identifier correlating database records and stored CSV artifacts for the sweep run.</param>
public record ExecuteParametricSweepResponse(string Identifier);
