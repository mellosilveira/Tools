using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Models;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.ParametricSweep.Commands.ProcessFromExperimentalData;

/// <summary>
/// Request payload for executing parametric sweeps on all constitutive parameters generated from an experimental data pipeline run.
/// </summary>
public record ExecuteParametricSweepFromExperimentalDataRequest : ExecuteParametricSweepPipelineRequestBase
{
    /// <summary>
    /// Identifier of the experimental data pipeline run whose fitted constitutive parameters are to be analyzed.
    /// </summary>
    public required string ExperimentalDataIdentifier { get; init; }

    /// <summary>
    /// Execution strategy: Sequential (default) or Parallel.
    /// </summary>
    public PipelineExecutionMode ExecutionMode { get; init; } = PipelineExecutionMode.Sequential;

    /// <summary>
    /// Maximum degree of parallelism when executing concurrently. Defaults to processor count if omitted or non-positive.
    /// </summary>
    public int? MaxDegreeOfParallelism { get; init; }
}
