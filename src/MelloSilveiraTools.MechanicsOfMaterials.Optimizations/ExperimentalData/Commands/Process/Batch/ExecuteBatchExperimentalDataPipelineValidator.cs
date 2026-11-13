using MelloSilveiraTools.Core.ExtensionMethods;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Core.Validators;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Commands.Process.Batch;

/// <summary>
/// Validates <see cref="ExecuteBatchExperimentalDataPipelineRequest"/> payloads prior to batch pipeline execution.
/// </summary>
public sealed class ExecuteBatchExperimentalDataPipelineValidator : IValidator<ExecuteBatchExperimentalDataPipelineRequest>
{
    /// <inheritdoc/>
    public Result Validate(ExecuteBatchExperimentalDataPipelineRequest value)
    {
        if (value is null)
            return Result.CreateBadRequest("Batch experimental data processing request cannot be null.");

        List<string> errors = [];
        errors
            .FluentAddIf(value.Groups is null || value.Groups.Count == 0, "Groups collection must not be null or empty.")
            .FluentAddIf(string.IsNullOrWhiteSpace(value.MechanicalModelName), "MechanicalModelName is required.")
            .FluentAddIf(value.TargetRSquared <= 0.0 || value.TargetRSquared > 1.0, "TargetRSquared must be within the interval (0, 1].")
            .FluentAddIf(value.ConfidenceLevel <= 0.0 || value.ConfidenceLevel >= 1.0, "ConfidenceLevel must be strictly between 0 and 1 (e.g. 0.95 for 95% confidence).")
            .FluentAddIf(value.AsymptoteConsecutivePointsThreshold < 0, "AsymptoteConsecutivePointsThreshold cannot be negative.")
            .FluentAddIf(value.SimulationTimeStep is <= 0.0, "SimulationTimeStep must be greater than zero when specified.")
            .FluentAddIf(value.DownsamplingSkipTimeStep is < 0.0, "DownsamplingSkipTimeStep cannot be negative.")
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

        return errors.Count == 0
            ? Result.CreateSuccessOk()
            : Result.CreateBadRequest<Result>(errors);
    }
}
