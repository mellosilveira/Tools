using MelloSilveiraTools.Core.Application.Commands;
using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.Core.Validators;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Factories;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Steps.CurveFitter;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Commands;

/// <summary>
/// Application command that executes constitutive model curve fitting over one or more <see cref="CurveSegment"/> items.
/// </summary>
/// <param name="stepFactory">Factory resolving model-specific curve fitter pipeline steps.</param>
/// <param name="validator">Request validator for <see cref="FitCurveRequest"/>.</param>
public sealed class FitCurve(
    IMechanicalModelStepFactory stepFactory,
    IValidator<FitCurveRequest> validator)
    : CommandBaseWithData<FitCurveRequest, FitCurveResultData>(validator)
{
    /// <inheritdoc/>
    protected override async Task<Result<FitCurveResultData>> ExecuteCommandAsync(FitCurveRequest request)
    {
        IMechanicalModelCurveFitterStep fitterStep = stepFactory.Create(
            request.MechanicalModelName,
            request.TargetSegments,
            request.CurveFitterType,
            request.TargetRSquared);

        await using (fitterStep.ConfigureAwait(false))
        {
            List<ParameterGroupResultData> groups = [];
            double totalError = 0;

            foreach (CurveSegment segment in request.Segments)
            {
                await foreach (MechanicalModelCurveFitOutput output in fitterStep.ExecuteAsync(segment, CancellationToken.None).ConfigureAwait(false))
                {
                    totalError += output.FinalError;
                    groups.Add(new ParameterGroupResultData
                    {
                        OptimizedParameters = output.ConstitutiveParameters,
                        AcceptedStrainRange = output.AcceptedRange,
                        RSquared = output.RSquared,
                        FinalError = output.FinalError,
                        Iterations = output.Iterations
                    });
                }
            }

            return new FitCurveResultData(totalError, [.. groups]);
        }
    }
}
