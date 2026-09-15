using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels.Viscoelasticity.QuasiLinear.Fung;
using MelloSilveiraTools.MechanicsOfMaterials.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.QuasiLinear;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Algorithms.CurveFitting;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Models.CurveFitting;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Steps.CurveFitter;

/// <summary>
/// Thread-safe. Implements <see cref="IMechanicalModelCurveFitterStep"/> for the
/// Fung model (relaxation-only curve fitting). Implements <c>IPipelineStep</c> for telemetry.
/// </summary>
public sealed class FungRelaxationOnlyCurveFitterStep(
    IFungModelCalculator mechanicalModelCalculator,
    ICurveFitter<FungConstitutiveParameters> curveFitter) 
    : IMechanicalModelCurveFitterStep
{
    /// <inheritdoc />
    public string Name => nameof(FungRelaxationOnlyCurveFitterStep);

    /// <inheritdoc />
    public ConstitutiveParameters[] Execute(CurveSegment[] input)
    {
        foreach (CurveSegment curve in input)
        {
            if (curve.Type != SegmentType.Relaxation)
                continue;

            MechanicalModelInput<FungConstitutiveParameters> initialMechanicalModelInput = new()
            {
                MechanicalModelName = nameof(MechanicalModel.Fung),
                AcceptedStrainRange = new AcceptedRange
                {
                    InitialPoint = curve.ExperimentalStrain[0],
                    FinalPoint = curve.ExperimentalStrain[^1],
                },
                MechanicalBehaviorType = MechanicalBehaviorType.StressStrain,
                RampTimeConsideration = RampTimeConsideration.Disregard,
                ViscoelasticEffect = ViscoelasticEffect.Relaxation,
                Strain = new MechanicalParameter(curve.ExperimentalStrain[0]),
                Stress = new MechanicalParameter(curve.ExperimentalStress[0]),
                TimeStep = curve.TimePoints[1] - curve.TimePoints[0],
                ConstitutiveParameters = new FungConstitutiveParameters
                {
                    ReducedRelaxationFunction = new ReducedRelaxationFunction(1, 1, 1),
                    ElasticPowerConstant = 1,
                    ElasticStressConstant = 1,
                },
            };

            CurveFitInput<FungConstitutiveParameters> curveFitInput = new()
            {
                TimePoints = curve.TimePoints,
                StrainPoints = curve.ExperimentalStrain,
                StressPoints = curve.ExperimentalStress,
                CalculateStress = (mechanicalModelInput, time, strain) => mechanicalModelCalculator.CalculateStress(mechanicalModelInput, time, strain),
                Options = new Models.OptimizationOptions([], [], []),
                EvaluateConstraintsAndPenalties = (mechanicalModelInput) => 0.0,
                InitialMechanicalModelInput = initialMechanicalModelInput,
            };

            Result<CurveFitResultData<FungConstitutiveParameters>> curveFitResult = curveFitter.Fit(curveFitInput);
        }

        // TODO: Implement specific numerical solver for Fung relaxation-only curve fitting.
        return [];
    }

    /// <inheritdoc />
    public void Dispose()
    {
        // No unmanaged resources to release.
    }
}
