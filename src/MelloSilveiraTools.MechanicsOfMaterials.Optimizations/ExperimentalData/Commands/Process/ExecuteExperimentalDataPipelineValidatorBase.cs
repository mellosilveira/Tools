using MelloSilveiraTools.Core.ExtensionMethods;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Core.Validators;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Models;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Commands.Process;

/// <summary>
/// Base validator providing common validation logic for experimental data pipeline requests inheriting from <see cref="ExecuteExperimentalDataPipelineRequestBase"/>.
/// </summary>
/// <typeparam name="TRequest">The request type to validate, deriving from <see cref="ExecuteExperimentalDataPipelineRequestBase"/>.</typeparam>
public abstract class ExecuteExperimentalDataPipelineValidatorBase<TRequest> : IValidator<TRequest>
    where TRequest : ExecuteExperimentalDataPipelineRequestBase
{
    /// <inheritdoc/>
    public Result Validate(TRequest value)
    {
        if (value is null)
            return Result.CreateBadRequest("Experimental data processing request cannot be null.");

        List<string> errors = [];

        errors
            .FluentAddIf(string.IsNullOrWhiteSpace(value.MechanicalModelName), "MechanicalModelName is required.")
            .FluentAddIf(!Enum.IsDefined(value.CurveFitterType), "A valid CurveFitterType must be specified.")
            .FluentAddIf(!double.IsFinite(value.TargetRSquared) || value.TargetRSquared <= 0.0 || value.TargetRSquared > 1.0, "TargetRSquared must be within the interval (0, 1].")
            .FluentAddIf(!double.IsFinite(value.ConfidenceLevel) || value.ConfidenceLevel <= 0.0 || value.ConfidenceLevel >= 1.0, "ConfidenceLevel must be strictly between 0 and 1 (e.g. 0.95 for 95% confidence).")
            .FluentAddIf(value.AsymptoteConsecutivePointsThreshold < 0, "AsymptoteConsecutivePointsThreshold cannot be negative.")
            .FluentAddIf(value.SimulationTimeStep is double simStep && (!double.IsFinite(simStep) || simStep <= 0.0), "SimulationTimeStep must be greater than zero when specified.")
            .FluentAddIf(value.FinalSimulationTime is double finalSim && (!double.IsFinite(finalSim) || finalSim <= 0.0), "FinalSimulationTime must be greater than zero when specified.")
            .FluentAddIf(value.DownsamplingSkipTimeStep is double skipStep && (!double.IsFinite(skipStep) || skipStep < 0.0), "DownsamplingSkipTimeStep cannot be negative.");

        if (value.TargetSegments is not null)
        {
            foreach (SegmentType segment in value.TargetSegments)
            {
                if (!Enum.IsDefined(segment))
                {
                    errors.Add($"TargetSegments contains an invalid SegmentType value '{segment}'.");
                    break;
                }
            }
        }

        ValidateComplementary(value, errors);

        return errors.Count == 0
            ? Result.CreateSuccessOk()
            : Result.CreateBadRequest<Result>(errors);
    }

    /// <summary>
    /// Executes complementary validations specific to the concrete request type.
    /// </summary>
    /// <param name="value">The request payload instance being validated.</param>
    /// <param name="errors">Accumulator list of validation error messages.</param>
    protected abstract void ValidateComplementary(TRequest value, List<string> errors);
}
