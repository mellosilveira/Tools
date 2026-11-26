using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;

namespace MelloSilveiraTools.MechanicsOfMaterials.Commands.SpecimenAnalysis;

/// <summary>
/// Encapsulates the downloadable CSV stream (which deletes the temporary file on disposal),
/// file download metadata, simulation delta outputs, and asymptotic convergence time.
/// </summary>
/// <param name="DeltaOutput">Initial, final, absolute delta, and percentage delta simulation outputs.</param>
/// <param name="AsymptoteTime">Time (in seconds) at which asymptotic convergence was detected, or <see langword="null"/> if not reached.</param>
/// <param name="OutputMetadataEntityId">The ID of the output metadata entity.</param>
public sealed record ExecuteSpecimenAnalysisResultData(
    SimulationDeltaOutput DeltaOutput,
    double? AsymptoteTime,
    long OutputMetadataEntityId);
