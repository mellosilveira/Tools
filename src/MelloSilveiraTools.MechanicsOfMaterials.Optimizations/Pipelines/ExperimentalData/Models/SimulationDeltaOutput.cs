using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Models;

/// <summary>
/// Encapsulates the boundary values and comparative deltas between the initial and final simulation outputs.
/// </summary>
/// <param name="InitialOutput">The output produced at the initial simulation step.</param>
/// <param name="FinalOutput">The output produced at the final simulation step.</param>
/// <param name="AbsoluteDelta">The absolute difference between final and initial outputs.</param>
/// <param name="PercentageDelta">The relative percentage change between final and initial outputs.</param>
public record SimulationDeltaOutput(
    MechanicalModelOutput InitialOutput,
    MechanicalModelOutput FinalOutput,
    MechanicalModelOutput AbsoluteDelta,
    MechanicalModelOutput PercentageDelta
);
