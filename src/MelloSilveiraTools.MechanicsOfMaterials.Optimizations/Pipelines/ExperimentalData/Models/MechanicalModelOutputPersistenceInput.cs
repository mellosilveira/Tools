using MelloSilveiraTools.Core.Managers.File.Models;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Models;

/// <summary>
/// Strongly-typed payload encapsulating the converging branches of a mechanical simulation.
/// </summary>
/// <param name="ExternalIdentifier">Optional external run or experiment identifier correlating this simulation run to business operations.</param>
/// <param name="MechanicalModelName">The name of the mechanical model evaluated.</param>
/// <param name="CurveFitIdentifier">The unique identifier of the associated curve fit.</param>
/// <param name="AsymptoteTime">The calculated asymptote time if steady-state was detected.</param>
/// <param name="SimulationDeltaOutput">The boundary delta metrics between initial and final simulation outputs.</param>
/// <param name="StorageEntryMetadata">The storage entry metadata for the generated simulation output file.</param>
public sealed record MechanicalModelOutputPersistenceInput(
    string ExternalIdentifier,
    string MechanicalModelName,
    string CurveFitIdentifier,
    double? AsymptoteTime,
    SimulationDeltaOutput SimulationDeltaOutput,
    StorageEntryMetadata StorageEntryMetadata);
