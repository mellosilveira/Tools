using MelloSilveiraTools.Core.ExtensionMethods;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Commands.Process.Single;

/// <summary>
/// Validates <see cref="ExecuteExperimentalDataPipelineRequest"/> payloads prior to pipeline execution.
/// </summary>
public sealed class ExecuteExperimentalDataPipelineValidator : ExecuteExperimentalDataPipelineValidatorBase<ExecuteExperimentalDataPipelineRequest>
{
    /// <inheritdoc/>
    protected override void ValidateComplementary(ExecuteExperimentalDataPipelineRequest value, List<string> errors)
    {
        errors
            .FluentAddIf(value.ExperimentalDataGroupRequest is null, "ExperimentalDataGroupRequest is required.")
            .FluentAddIf(value.ExperimentalDataGroupRequest is not null && string.IsNullOrWhiteSpace(value.ExperimentalDataGroupRequest.StrainFileLocation), "StrainFileLocation must be provided.")
            .FluentAddIf(value.ExperimentalDataGroupRequest is not null && string.IsNullOrWhiteSpace(value.ExperimentalDataGroupRequest.StressFileLocation), "StressFileLocation must be provided.");
    }
}
