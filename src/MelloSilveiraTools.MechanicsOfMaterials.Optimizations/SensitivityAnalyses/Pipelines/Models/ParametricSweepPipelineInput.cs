using MelloSilveiraTools.Core.Pipelines.Models;
using MelloSilveiraTools.Mathematics.Models.NumericalMethods;
using MelloSilveiraTools.Mathematics.NumericalMethods.RootFindingAlgorithms;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Models.Range;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.Pipelines.Models;

/// <summary>
/// Represents the input request for a Full Factorial Parametric Sweep (Grid Search) analysis.
/// </summary>
public record ParametricSweepPipelineInput : PipelineInputBase
{
    /// <summary>
    /// Gets the name of the mechanical model to be evaluated (e.g., "Maxwell", "Fung").
    /// </summary>
    public required string MechanicalModelName { get; init; }

    /// <summary>
    /// Gets the baseline constitutive parameters.
    /// </summary>
    public required ConstitutiveParameters Baseline { get; init; }

    /// <summary>
    /// Gets the ranges for constant scalar variables.
    /// </summary>
    public required Dictionary<string, RangeParameters> ConstantVariables { get; init; }

    /// <summary>
    /// Gets the ranges for array-based variables.
    /// </summary>
    public required Dictionary<string, Dictionary<int, RangeParameters>> ArrayVariables { get; init; }

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
    /// Gets the root-finding algorithm used to locate steady-state relaxation asymptotes.
    /// </summary>
    public required RootFindingAlgorithm RootFindingAlgorithm { get; init; }

    /// <summary>
    /// Gets the base root-finding configuration parameters for asymptote detection.
    /// </summary>
    public required RootFindingInput BaseRootFindingInput { get; init; }
}
