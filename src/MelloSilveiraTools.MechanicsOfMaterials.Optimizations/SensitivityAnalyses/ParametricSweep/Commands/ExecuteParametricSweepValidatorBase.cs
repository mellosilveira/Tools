using MelloSilveiraTools.Core.ExtensionMethods;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Core.Validators;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Models.Range;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.ParametricSweep.Commands;

/// <summary>
/// Base validator providing common validation logic for parametric sweep requests inheriting from <see cref="ExecuteParametricSweepPipelineRequestBase"/>.
/// </summary>
/// <typeparam name="TRequest">The request type to validate, deriving from <see cref="ExecuteParametricSweepPipelineRequestBase"/>.</typeparam>
public abstract class ExecuteParametricSweepValidatorBase<TRequest> : IValidator<TRequest>
    where TRequest : ExecuteParametricSweepPipelineRequestBase
{
    /// <inheritdoc/>
    public ValueTask<Result> ValidateAsync(TRequest value, CancellationToken cancellationToken = default)
    {
        if (value is null)
            return ValueTask.FromResult(Result.CreateBadRequest("Parametric sweep request cannot be null."));

        List<string> errors = [];

        bool hasConstantVariables = value.ConstantVariables is { Count: > 0 };
        bool hasArrayVariables = value.ArrayVariables is { Count: > 0 };
        bool hasStrainRange = value.StrainRange is not null;

        errors
            .FluentAddIf(string.IsNullOrWhiteSpace(value.MechanicalModelName), "MechanicalModelName is required.")
            .FluentAddIf(!double.IsFinite(value.BaseStrain), "BaseStrain must be a finite number.")
            .FluentAddIf(!Enum.IsDefined(value.RampTimeConsideration), "A valid RampTimeConsideration must be specified.")
            .FluentAddIf(
                value.RampTime is double rampTime && (!double.IsFinite(rampTime) || rampTime <= 0.0),
                "RampTime must be a finite number greater than zero when specified.")
            .FluentAddIf(!double.IsFinite(value.InitialTime), "InitialTime must be a finite number.")
            .FluentAddIf(!double.IsFinite(value.FinalTime) || value.FinalTime <= value.InitialTime, "FinalTime must be greater than InitialTime.")
            .FluentAddIf(!double.IsFinite(value.TimeStep) || value.TimeStep <= 0.0, "TimeStep must be greater than zero.")
            .FluentAddIf(!Enum.IsDefined(value.RootFindingAlgorithm), "A valid RootFindingAlgorithm must be specified.")
            .FluentAddIf(value.BaseRootFindingInput is null, "BaseRootFindingInput is required.")
            .FluentAddIf(
                value.BaseRootFindingInput is not null && (!double.IsFinite(value.BaseRootFindingInput.Tolerance) || value.BaseRootFindingInput.Tolerance <= 0.0),
                "BaseRootFindingInput.Tolerance must be greater than zero.")
            .FluentAddIf(value.BaseRootFindingInput is { MaxIterations: <= 0 }, "BaseRootFindingInput.MaxIterations must be greater than zero.")
            .FluentAddIf(
                value.BaseRootFindingInput is not null
                && (!double.IsFinite(value.BaseRootFindingInput.InitialPoint)
                    || !double.IsFinite(value.BaseRootFindingInput.FinalPoint)
                    || value.BaseRootFindingInput.FinalPoint <= value.BaseRootFindingInput.InitialPoint),
                "BaseRootFindingInput.FinalPoint must be greater than BaseRootFindingInput.InitialPoint.")
            .FluentAddIf(!hasConstantVariables && !hasArrayVariables && !hasStrainRange, "At least one swept variable (ConstantVariables, ArrayVariables, or StrainRange) must be provided.");

        if (value.StrainRange is not null)
            ValidateRangeParameters("StrainRange", value.StrainRange, errors);

        if (value.ConstantVariables is not null)
        {
            foreach ((string key, RangeParameters? range) in value.ConstantVariables)
            {
                if (range is null)
                {
                    errors.Add($"ConstantVariables['{key}'] cannot be null.");
                    continue;
                }

                ValidateRangeParameters($"ConstantVariables['{key}']", range, errors);
            }
        }

        if (value.ArrayVariables is not null)
        {
            foreach ((string key, Dictionary<int, RangeParameters>? indexedRanges) in value.ArrayVariables)
            {
                if (indexedRanges is null)
                {
                    errors.Add($"ArrayVariables['{key}'] cannot be null.");
                    continue;
                }

                foreach ((int index, RangeParameters? range) in indexedRanges)
                {
                    if (range is null)
                    {
                        errors.Add($"ArrayVariables['{key}'][{index}] cannot be null.");
                        continue;
                    }

                    ValidateRangeParameters($"ArrayVariables['{key}'][{index}]", range, errors);
                }
            }
        }

        ValidateComplementary(value, errors);

        Result result = errors.Count == 0 ? Result.CreateSuccessOk() : Result.CreateBadRequest<Result>(errors);
        return ValueTask.FromResult(result);
    }

    /// <summary>
    /// Executes complementary validations specific to the concrete request type.
    /// </summary>
    /// <param name="value">The request payload instance being validated.</param>
    /// <param name="errors">Accumulator list of validation error messages.</param>
    protected abstract void ValidateComplementary(TRequest value, List<string> errors);

    /// <summary>
    /// Validates numeric bounds and progression parameters of a <see cref="RangeParameters"/> instance.
    /// </summary>
    /// <param name="name">Descriptive name of the parameter being validated.</param>
    /// <param name="range">The range parameters instance.</param>
    /// <param name="errors">Accumulator list of validation error messages.</param>
    protected static void ValidateRangeParameters(string name, RangeParameters range, List<string> errors)
    {
        if (!double.IsFinite(range.InitialPoint))
            errors.Add($"{name}.InitialPoint must be a finite number.");

        if (range.FinalPoint is null)
            return;

        if (!double.IsFinite(range.FinalPoint.Value))
        {
            errors.Add($"{name}.FinalPoint must be a finite number.");
            return;
        }

        if (range.FinalPoint.Value < range.InitialPoint)
            errors.Add($"{name}.FinalPoint must be greater than or equal to InitialPoint.");

        if (range.Step.HasValue && (!double.IsFinite(range.Step.Value) || range.Step.Value <= 0.0))
            errors.Add($"{name}.Step must be a finite number greater than zero.");

        if (!range.Step.HasValue && range.MultiplicativeFactor.HasValue)
        {
            if (!double.IsFinite(range.MultiplicativeFactor.Value) || range.MultiplicativeFactor.Value <= 1.0)
                errors.Add($"{name}.MultiplicativeFactor must be a finite number strictly greater than 1.");

            if (range.InitialPoint <= 0.0)
                errors.Add($"{name}.InitialPoint must be strictly positive when using MultiplicativeFactor.");
        }
    }
}
