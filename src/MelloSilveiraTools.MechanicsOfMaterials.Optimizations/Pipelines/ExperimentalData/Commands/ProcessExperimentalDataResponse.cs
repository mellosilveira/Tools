namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Commands;

/// <summary>
/// Response payload returned by <see cref="ProcessExperimentalDataCommand"/> containing the unique analysis identifier.
/// </summary>
/// <param name="Identifier">The external identifier correlating database records and stored CSV artifacts for the processing run.</param>
public record ProcessExperimentalDataResponse(string Identifier);
