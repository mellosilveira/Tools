using MelloSilveiraTools.Core.Managers.File;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Models;

/// <summary>
/// Strongly-typed payload encapsulating the converging branches of a mechanical simulation.
/// </summary>
public record MechanicalModelOutputPersistenceInput(
    string MechanicalModelName,
    string CurveFitIdentifier,
    double? AsymptoteTime,
    SimulationDeltaOutput Delta,
    FileData FileData);
