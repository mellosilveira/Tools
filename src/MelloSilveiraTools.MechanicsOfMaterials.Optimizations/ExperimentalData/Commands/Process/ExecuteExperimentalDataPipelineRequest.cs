namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Commands.Process;

/// <summary>
/// Request payload for executing the experimental data processing and optimization pipeline via storage-backed file references.
/// </summary>
public record ExecuteExperimentalDataPipelineRequest : ExecuteExperimentalDataPipelineRequestBase
{
    public ExperimentalDataGroupRequest ExperimentalDataGroupRequest { get; init; }
}
