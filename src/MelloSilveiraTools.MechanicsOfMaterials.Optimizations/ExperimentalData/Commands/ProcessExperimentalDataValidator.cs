using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Core.Validators;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Commands;

/// <summary>
/// Validates <see cref="ProcessExperimentalDataRequest"/> payloads prior to pipeline execution.
/// </summary>
public sealed class ProcessExperimentalDataValidator : IValidator<ProcessExperimentalDataRequest>
{
    /// <inheritdoc/>
    public Result Validate(ProcessExperimentalDataRequest value)
    {
        if (value is null)
        {
            return Result.CreateBadRequest("Experimental data processing request cannot be null.");
        }

        List<string> errors = [];

        if (string.IsNullOrWhiteSpace(value.MechanicalModelName))
        {
            errors.Add("MechanicalModelName is required.");
        }

        if (string.IsNullOrWhiteSpace(value.StrainFileLocation))
        {
            errors.Add("StrainFileLocation must be provided.");
        }

        if (string.IsNullOrWhiteSpace(value.StressFileLocation))
        {
            errors.Add("StressFileLocation must be provided.");
        }

        if (value.TargetRSquared <= 0 || value.TargetRSquared > 1.0)
        {
            errors.Add("TargetRSquared must be within the interval (0, 1].");
        }

        if (value.AsymptoteConsecutivePointsThreshold < 0)
        {
            errors.Add("AsymptoteConsecutivePointsThreshold cannot be negative.");
        }

        if (value.SimulationTimeStep is <= 0)
        {
            errors.Add("SimulationTimeStep must be greater than zero when specified.");
        }

        if (value.DownsamplingSkipTimeStep is < 0)
        {
            errors.Add("DownsamplingSkipTimeStep cannot be negative.");
        }

        return errors.Count == 0
            ? Result.CreateSuccessOk()
            : Result.CreateBadRequest<Result>(errors);
    }
}
