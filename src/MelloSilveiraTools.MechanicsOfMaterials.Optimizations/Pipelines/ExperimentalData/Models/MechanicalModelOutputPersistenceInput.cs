using MelloSilveiraTools.Core.Managers.File;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Models;

/// <summary>
/// Strongly-typed payload encapsulating the converging branches of a mechanical simulation.
/// </summary>
/// <param name="MechanicalModelName">The name of the mechanical model evaluated.</param>
/// <param name="CurveFitIdentifier">The unique identifier of the associated curve fit.</param>
/// <param name="AsymptoteTime">The calculated asymptote time if steady-state was detected.</param>
/// <param name="Delta">The boundary delta metrics between initial and final simulation outputs.</param>
/// <param name="FileData">The metadata for the generated simulation output file.</param>
public sealed record MechanicalModelOutputPersistenceInput(
    string MechanicalModelName,
    string CurveFitIdentifier,
    double? AsymptoteTime,
    SimulationDeltaOutput Delta,
    FileData FileData);
