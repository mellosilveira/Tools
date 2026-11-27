using MelloSilveiraTools.Core.ExtensionMethods;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Core.Validators;
using MelloSilveiraTools.Mathematics.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using System.Text.Json;

namespace MelloSilveiraTools.MechanicsOfMaterials.Commands.SpecimenAnalysis;

/// <summary>
/// Validates <see cref="ExecuteSpecimenAnalysisRequest"/> invariants before executing a specimen mechanical analysis.
/// </summary>
public sealed class ExecuteSpecimenAnalysisValidator : IValidator<ExecuteSpecimenAnalysisRequest>
{
    /// <inheritdoc/>
    public ValueTask<Result> ValidateAsync(ExecuteSpecimenAnalysisRequest value, CancellationToken cancellationToken = default)
    {
        if (value is null)
            return ValueTask.FromResult(Result.CreateBadRequest("Request cannot be null."));

        double initialTime = value.InitialTime ?? MathematicConstants.InitialTime;

        Result result = Result.CreateSuccessOk()
            .AddErrorIfNullOrWhiteSpace(value.MechanicalModelName, "MechanicalModelName is required.")
            .AddErrorIfNull(value.AnalysisParameter, "AnalysisParameter is required.")
            .AddErrorIfNegativeOrZero(value.TimeStep, "TimeStep must be greater than zero.")
            .AddErrorIf(value.FinalTime <= initialTime, "FinalTime must be greater than InitialTime.")
            .AddErrorIf(value.AsymptoticConvergenceThreshold <= 0, "AsymptoticConvergenceThreshold must be greater than zero.")
            .AddErrorIf(value.ConstitutiveParameters.ValueKind != JsonValueKind.Object, "ConstitutiveParameters must be a valid JSON object.")
            .AddErrorIfInvalidEnum(value.LoadResponseRelationship, "LoadResponseRelationship is invalid.")
            .AddErrorIfInvalidEnum(value.ViscoelasticEffect, "ViscoelasticEffect is invalid.")
            .AddErrorIfInvalidEnum(value.RampTimeConsideration, "RampTimeConsideration is invalid.")
            .AddErrorIf(
            value.LoadResponseRelationship == LoadResponseRelationship.ForceDisplacement && value.Specimen is null,
                "Specimen geometry is required for ForceDisplacement analyses.");

        if (value.Specimen is not null)
        {
            result = result
                .AddErrorIfNegativeOrZero(value.Specimen.AreaInSquareMillimeters, "Specimen AreaInSquareMillimeters must be greater than zero.")
                .AddErrorIfNegativeOrZero(value.Specimen.InitialLengthInMillimeters, "Specimen InitialLengthInMillimeters must be greater than zero.")
                .AddErrorIfNullOrWhiteSpace(value.Specimen.InitialAngleAsString, "Specimen InitialAngleAsString is required.")
                .AddErrorIf(
                    !string.IsNullOrWhiteSpace(value.Specimen.InitialAngleAsString) && !Vector3D.TryParse(value.Specimen.InitialAngleAsString, out _), 
                    "Specimen InitialAngleAsString must be a valid comma-separated 3D vector (e.g. '0,0,0').");
        }

        return ValueTask.FromResult(result);
    }
}
