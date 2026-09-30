using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Models;

/// <summary>
/// Payload containing the full array of simulated numerical points for a specific curve fit,
/// allowing multiple downstream branches (Asymptote, Delta, CSV) to analyze the same dataset concurrently.
/// </summary>
public record MechanicalModelSimulationPayload(
    MechanicalModelCurveFitOutput CurveFit,
    MechanicalModelOutput[] SimulatedPoints
);
