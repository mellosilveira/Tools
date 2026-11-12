using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.ParametricSweep.Commands.Process;

/// <summary>
/// Request payload for executing a Parametric Sweep sensitivity analysis via the Command pattern.
/// </summary>
public record ExecuteParametricSweepPipelineRequest : ExecuteParametricSweepPipelineRequestBase
{
    /// <summary>
    /// Optional predefined analysis identifier. If not provided, a unique GUID-based identifier is generated.
    /// </summary>
    public string? Identifier { get; init; }

    /// <summary>
    /// Gets the baseline constitutive parameters.
    /// </summary>
    public required ConstitutiveParameters Baseline { get; init; }
}
