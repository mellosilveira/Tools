namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.ParametricSweep.Commands.ProcessFromExperimentalData;

/// <summary>
/// Represents the outcome of executing a parametric sweep for a specific fitted mechanical model.
/// </summary>
/// <param name="CurveFitOutputIdentifier">Unique identifier of the source curve fit output record.</param>
/// <param name="MechanicalModelName">Name of the mechanical model evaluated.</param>
/// <param name="SweepIdentifier">The unique identifier of the executed parametric sweep pipeline run, or null if execution failed.</param>
/// <param name="Success">Indicates whether the parametric sweep completed successfully.</param>
/// <param name="ErrorMessage">Contextual error message explaining the failure when unsuccessful.</param>
public sealed record ParametricSweepExecutionResult(
    string CurveFitOutputIdentifier,
    string MechanicalModelName,
    string? SweepIdentifier,
    bool Success,
    string? ErrorMessage = null);
