using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Models;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Commands.Process;

/// <summary>
/// Request payload for executing the experimental data processing pipeline over multiple groups of stress.csv and strain.csv files.
/// </summary>
public record ExecuteBatchExperimentalDataPipelineRequest : ExecuteExperimentalDataPipelineRequestBase
{
    /// <summary>
    /// The collection of experimental data groups to be processed.
    /// </summary>
    public required IReadOnlyList<ExperimentalDataGroupRequest> Groups { get; init; }

    /// <summary>
    /// Execution mode: Sequential or Parallel. Defaults to <see cref="PipelineExecutionMode.Sequential"/>.
    /// </summary>
    public PipelineExecutionMode ExecutionMode { get; init; } = PipelineExecutionMode.Sequential;

    /// <summary>
    /// Maximum degree of parallelism when executing in parallel. If null or not specified, defaults to <see cref="Environment.ProcessorCount"/>.
    /// </summary>
    public int? MaxDegreeOfParallelism { get; init; }
}
