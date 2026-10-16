using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Core.Validators;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Commands;

/// <summary>
/// Validates <see cref="FitCurveRequest"/> payloads prior to executing curve fitting.
/// </summary>
public sealed class FitCurveValidator : IValidator<FitCurveRequest>
{
    /// <inheritdoc/>
    public Result Validate(FitCurveRequest value)
    {
        if (value is null)
        {
            return Result.CreateBadRequest("Curve fitting request cannot be null.");
        }

        List<string> errors = [];

        if (string.IsNullOrWhiteSpace(value.MechanicalModelName))
        {
            errors.Add("MechanicalModelName is required.");
        }

        if (value.Segments is null || value.Segments.Count == 0)
        {
            errors.Add("At least one CurveSegment must be provided in Segments.");
        }

        if (value.TargetRSquared <= 0 || value.TargetRSquared > 1.0)
        {
            errors.Add("TargetRSquared must be within the interval (0, 1].");
        }

        return errors.Count == 0
            ? Result.CreateSuccessOk()
            : Result.CreateBadRequest<Result>(errors);
    }
}
