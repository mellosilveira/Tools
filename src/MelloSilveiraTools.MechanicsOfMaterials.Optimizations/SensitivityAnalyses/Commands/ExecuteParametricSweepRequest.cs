using MelloSilveiraTools.Core.Application.Commands;
using MelloSilveiraTools.Core.Pipelines.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Models.Range;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.Models;
using System.Text.Json.Serialization;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.Commands;

/// <summary>
/// Request payload for executing a Parametric Sweep sensitivity analysis via the Command pattern.
/// </summary>
public record ExecuteParametricSweepRequest : RequestBase
{
    /// <summary>
    /// Optional predefined analysis identifier. If not provided, a unique GUID-based identifier is generated.
    /// </summary>
    public string? Identifier { get; init; }

    /// <summary>
    /// Gets the name of the mechanical model to be evaluated (e.g., "Maxwell", "Fung", "SimplifiedFung", "Schapery").
    /// </summary>
    public required string MechanicalModelName { get; init; }

    /// <summary>
    /// Gets the baseline constitutive parameters.
    /// </summary>
    public required ConstitutiveParameters Baseline { get; init; }

    /// <summary>
    /// Gets the ranges for constant scalar variables.
    /// </summary>
    public Dictionary<string, RangeParameters> ConstantVariables { get; init; } = [];

    /// <summary>
    /// Gets the ranges for array-based variables.
    /// </summary>
    public Dictionary<string, Dictionary<int, RangeParameters>> ArrayVariables { get; init; } = [];

    /// <summary>
    /// Gets the baseline strain applied in the mechanical simulation when strain is not the swept variable.
    /// </summary>
    public required double BaseStrain { get; init; }

    /// <summary>
    /// Gets the range of strain to be applied in the mechanical simulation.
    /// </summary>
    public RangeParameters? StrainRange { get; init; }

    /// <summary>
    /// Gets the configuration for how ramp time is considered in the simulation.
    /// </summary>
    public required RampTimeConsideration RampTimeConsideration { get; init; }

    /// <summary>
    /// Gets the duration of the initial loading ramp, if applicable.
    /// </summary>
    public double? RampTime { get; init; }

    /// <summary>
    /// Gets the initial time to evaluate the initial stress.
    /// </summary>
    public required double InitialTime { get; init; }

    /// <summary>
    /// Gets the time step increment for the simulation.
    /// </summary>
    public required double TimeStep { get; init; }

    /// <summary>
    /// Gets the final time of the simulation.
    /// </summary>
    public required double FinalTime { get; init; }

    /// <summary>
    /// Gets the relative tolerance threshold utilized by the asymptote detection step. Defaults to 1e-3.
    /// </summary>
    public double AsymptoteTolerance { get; init; } = 1e-3;

    /// <summary>
    /// Optional progress reporter for emitting real-time <see cref="PipelineProgressEvent"/> notifications.
    /// </summary>
    [JsonIgnore]
    public IProgress<PipelineProgressEvent>? Progress { get; init; }

    /// <summary>
    /// Converts this command request into a <see cref="ParametricSweepInput"/> instance.
    /// </summary>
    /// <returns>A configured <see cref="ParametricSweepInput"/>.</returns>
    public ParametricSweepInput ToInput() => new()
    {
        Identifier = Identifier,
        MechanicalModelName = MechanicalModelName,
        Baseline = Baseline,
        ConstantVariables = ConstantVariables,
        ArrayVariables = ArrayVariables,
        BaseStrain = BaseStrain,
        StrainRange = StrainRange,
        RampTimeConsideration = RampTimeConsideration,
        RampTime = RampTime,
        InitialTime = InitialTime,
        TimeStep = TimeStep,
        FinalTime = FinalTime,
        AsymptoteTolerance = AsymptoteTolerance,
        Progress = Progress
    };
}
