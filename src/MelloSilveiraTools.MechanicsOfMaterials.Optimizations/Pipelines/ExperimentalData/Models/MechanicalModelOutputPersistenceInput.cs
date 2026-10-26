using MelloSilveiraTools.Core.Managers.File.Models;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Models;

/// <summary>
/// Strongly-typed payload encapsulating the converging branches of a mechanical simulation.
/// </summary>
/// <param name="CurveFitOutputIdentifier">The unique identifier of the associated curve fit.</param>
/// <param name="ExperimentalDataFileLocation"></param>
/// <param name="AsymptoteTime">The calculated asymptote time if steady-state was detected.</param>
/// <param name="SimulationDeltaOutput">The boundary delta metrics between initial and final simulation outputs.</param>
/// <param name="SimulationOutputFileMetadata">The storage entry metadata for the generated simulation output file.</param>
public sealed record MechanicalModelOutputPersistenceInput(
    string ExperimentalDataFileLocation,
    string CurveFitOutputIdentifier,
    double? AsymptoteTime,
    SimulationDeltaOutput SimulationDeltaOutput,
    StorageEntryMetadata SimulationOutputFileMetadata);
