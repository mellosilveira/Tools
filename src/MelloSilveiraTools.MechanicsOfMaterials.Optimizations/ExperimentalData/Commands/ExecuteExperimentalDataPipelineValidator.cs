using MelloSilveiraTools.Core.ExtensionMethods;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Core.Validators;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Commands;

/// <summary>
/// Validates <see cref="ExecuteExperimentalDataPipelineRequest"/> payloads prior to pipeline execution.
/// </summary>
public sealed class ExecuteExperimentalDataPipelineValidator : IValidator<ExecuteExperimentalDataPipelineRequest>
{
    /// <inheritdoc/>
    public Result Validate(ExecuteExperimentalDataPipelineRequest value)
    {
        if (value is null)
            return Result.CreateBadRequest("Experimental data processing request cannot be null.");

        List<string> errors = [];
        errors
            .FluentAddIf(string.IsNullOrWhiteSpace(value.MechanicalModelName), "MechanicalModelName is required.")
            .FluentAddIf(string.IsNullOrWhiteSpace(value.StrainFileLocation), "StrainFileLocation must be provided.")
            .FluentAddIf(string.IsNullOrWhiteSpace(value.StressFileLocation), "StressFileLocation must be provided.")
            .FluentAddIf(value.TargetRSquared <= 0 || value.TargetRSquared > 1.0, "TargetRSquared must be within the interval (0, 1].")
            .FluentAddIf(value.AsymptoteConsecutivePointsThreshold < 0, "AsymptoteConsecutivePointsThreshold cannot be negative.")
            .FluentAddIf(value.SimulationTimeStep is <= 0, "SimulationTimeStep must be greater than zero when specified.")
            .FluentAddIf(value.DownsamplingSkipTimeStep is < 0, "DownsamplingSkipTimeStep cannot be negative.");

        return errors.Count == 0
            ? Result.CreateSuccessOk()
            : Result.CreateBadRequest<Result>(errors);
    }
}
