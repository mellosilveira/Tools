using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Core.Validators;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.SensitivityAnalyses.Commands;

/// <summary>
/// Validates <see cref="ExecuteParametricSweepRequest"/> payloads prior to parametric sweep execution.
/// </summary>
public sealed class ExecuteParametricSweepValidator : IValidator<ExecuteParametricSweepRequest>
{
    /// <inheritdoc/>
    public Result Validate(ExecuteParametricSweepRequest value)
    {
        if (value is null)
        {
            return Result.CreateBadRequest("Parametric sweep request cannot be null.");
        }

        List<string> errors = [];

        if (string.IsNullOrWhiteSpace(value.MechanicalModelName))
        {
            errors.Add("MechanicalModelName is required.");
        }

        if (value.Baseline is null)
        {
            errors.Add("Baseline constitutive parameters are required.");
        }

        if (value.TimeStep <= 0)
        {
            errors.Add("TimeStep must be greater than zero.");
        }

        if (value.FinalTime <= value.InitialTime)
        {
            errors.Add("FinalTime must be greater than InitialTime.");
        }

        if (value.AsymptoteTolerance <= 0)
        {
            errors.Add("AsymptoteTolerance must be greater than zero.");
        }

        bool hasConstantVariables = value.ConstantVariables is not null && value.ConstantVariables.Count > 0;
        bool hasArrayVariables = value.ArrayVariables is not null && value.ArrayVariables.Count > 0;
        bool hasStrainRange = value.StrainRange is not null;

        if (!hasConstantVariables && !hasArrayVariables && !hasStrainRange)
        {
            errors.Add("At least one swept variable (ConstantVariables, ArrayVariables, or StrainRange) must be provided.");
        }

        return errors.Count == 0
            ? Result.CreateSuccessOk()
            : Result.CreateBadRequest<Result>(errors);
    }
}
