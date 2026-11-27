using MelloSilveiraTools.Core.ExtensionMethods;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Commands.Process.Batch;

/// <summary>
/// Validates <see cref="ExecuteBatchExperimentalDataPipelineRequest"/> payloads prior to batch pipeline execution.
/// </summary>
public sealed class ExecuteBatchExperimentalDataPipelineValidator : ExecuteExperimentalDataPipelineValidatorBase<ExecuteBatchExperimentalDataPipelineRequest>
{
    /// <inheritdoc/>
    protected override void ValidateComplementary(ExecuteBatchExperimentalDataPipelineRequest value, List<string> errors)
    {
        errors
            .FluentAddIf(value.Groups is null || value.Groups.Count == 0, "Groups collection must not be null or empty.")
            .FluentAddIf(!Enum.IsDefined(value.ExecutionMode), "A valid ExecutionMode must be specified.")
            .FluentAddIf(value.MaxDegreeOfParallelism is <= 0, "MaxDegreeOfParallelism must be greater than zero when specified.");

        if (value.Groups is not null)
        {
            for (int i = 0; i < value.Groups.Count; i++)
            {
                ExperimentalDataGroupRequest group = value.Groups[i];
                if (group is null)
                {
                    errors.Add($"Group at index {i} cannot be null.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(group.StrainFileLocation))
                    errors.Add($"Group at index {i} requires a non-empty StrainFileLocation.");

                if (string.IsNullOrWhiteSpace(group.StressFileLocation))
                    errors.Add($"Group at index {i} requires a non-empty StressFileLocation.");
            }
        }
    }
}
