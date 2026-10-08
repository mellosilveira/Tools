using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Models;

/// <summary>
/// Domain output resulting from the numerical forward simulation of a fitted mechanical model.
/// </summary>
/// <param name="Identifier">Unique identifier for the simulation execution.</param>
/// <param name="CurveFitIdentifier">Correlated identifier of the curve fit parameter set.</param>
/// <param name="OutputFullFileName">Full path of the generated simulation CSV file.</param>
/// <param name="InitialOutput">Calculated initial output state.</param>
/// <param name="FinalOutput">Calculated final output state.</param>
/// <param name="AbsoluteDeltaOutput">Calculated absolute delta across all output properties.</param>
/// <param name="PercentageDeltaOutput">Calculated percentage delta across all output properties.</param>
/// <param name="AsymptoteTime">Time when asymptote convergence was reached, or null if not reached.</param>
public record MechanicalModelSimulationOutput(
    string Identifier,
    string CurveFitIdentifier,
    string OutputFullFileName,
    MechanicalModelOutput InitialOutput,
    MechanicalModelOutput FinalOutput,
    MechanicalModelOutput AbsoluteDeltaOutput,
    MechanicalModelOutput PercentageDeltaOutput,
    double? AsymptoteTime);
