namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Commands.Process;

/// <summary>
/// Request payload for executing the experimental data processing and optimization pipeline via storage-backed file references.
/// </summary>
public record ExecuteExperimentalDataPipelineRequest : ExecuteExperimentalDataPipelineRequestBase
{
    /// <summary>
    /// Gets the experimental data group configuration containing the target storage reference and strain range.
    /// </summary>
    public required ExperimentalDataGroupRequest ExperimentalDataGroupRequest { get; init; }
}
