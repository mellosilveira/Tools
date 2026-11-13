using MelloSilveiraTools.Core.ExtensionMethods;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Core.Validators;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.ParametricSweep.Commands.ProcessFromExperimentalData;

/// <summary>
/// Validates <see cref="ExecuteParametricSweepFromExperimentalDataRequest"/> payloads prior to parametric sweep execution.
/// </summary>
public sealed class ExecuteParametricSweepFromExperimentalDataValidator : IValidator<ExecuteParametricSweepFromExperimentalDataRequest>
{
    /// <inheritdoc/>
    public Result Validate(ExecuteParametricSweepFromExperimentalDataRequest value)
    {
        if (value is null)
            return Result.CreateBadRequest("Parametric sweep request cannot be null.");

        bool hasConstantVariables = value.ConstantVariables is { Count: > 0 };
        bool hasArrayVariables = value.ArrayVariables is { Count: > 0 };
        bool hasStrainRange = value.StrainRange is not null;

        List<string> errors = [];
        errors
            .FluentAddIf(string.IsNullOrWhiteSpace(value.ExperimentalDataIdentifier), "ExperimentalDataIdentifier is required.")
            .FluentAddIf(value.TimeStep <= 0, "TimeStep must be greater than zero.")
            .FluentAddIf(value.FinalTime <= value.InitialTime, "FinalTime must be greater than InitialTime.")
            .FluentAddIf(!Enum.IsDefined(value.RootFindingAlgorithm), "A valid RootFindingAlgorithm must be specified.")
            .FluentAddIf(value.BaseRootFindingInput is null, "BaseRootFindingInput is required.")
            .FluentAddIf(value.BaseRootFindingInput is { Tolerance: <= 0 }, "BaseRootFindingInput.Tolerance must be greater than zero.")
            .FluentAddIf(value.BaseRootFindingInput is { MaxIterations: <= 0 }, "BaseRootFindingInput.MaxIterations must be greater than zero.")
            .FluentAddIf(value.BaseRootFindingInput is not null && value.BaseRootFindingInput.FinalPoint <= value.BaseRootFindingInput.InitialPoint, "BaseRootFindingInput.FinalPoint must be greater than BaseRootFindingInput.InitialPoint.")
            .FluentAddIf(!Enum.IsDefined(value.ExecutionMode), "A valid ExecutionMode must be specified.")
            .FluentAddIf(value.MaxDegreeOfParallelism is <= 0, "MaxDegreeOfParallelism must be greater than zero when specified.")
            .FluentAddIf(!hasConstantVariables && !hasArrayVariables && !hasStrainRange, "At least one swept variable (ConstantVariables, ArrayVariables, or StrainRange) must be provided.");

        return errors.Count == 0
            ? Result.CreateSuccessOk()
            : Result.CreateBadRequest<Result>(errors);
    }
}
