using MelloSilveiraTools.Core.Managers.File;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Models;

/// <summary>
/// Strongly-typed payload encapsulating the converging branches of a mechanical simulation.
/// </summary>
public readonly record struct MechanicalModelOutputPersistenceInput(
    string CurveFitIdentifier,
    MechanicalModelCurveFitOutput CurveFit,
    double? AsymptoteTime,
    SimulationDeltaOutput Delta,
    FileData FileData);
