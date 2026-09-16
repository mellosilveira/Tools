using MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels.Viscoelasticity.QuasiLinear.SimplifiedFung;
using MelloSilveiraTools.MechanicsOfMaterials.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.QuasiLinear;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Algorithms.CurveFitting;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Models.CurveFitting;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Steps.CurveFitter;

/// <summary>
/// Thread-safe. Implements <see cref="IMechanicalModelCurveFitterStep"/> for the
/// Simplified Fung model (relaxation-only curve fitting). Implements <c>IPipelineStep</c> for telemetry.
/// </summary>
public sealed class SimplifiedFungRelaxationOnlyCurveFitterStep(
    ISimplifiedFungModelCalculator mechanicalModelCalculator,
    ICurveFitter curveFitter) 
    : SimplifiedFungCurveFitterStep(mechanicalModelCalculator, curveFitter)
{
    /// <inheritdoc />
    public override string Name => nameof(SimplifiedFungRelaxationOnlyCurveFitterStep);

    /// <inheritdoc />
    public override async IAsyncEnumerable<MechanicalModelCurveFitOutput> ExecuteAsync(CurveSegment[] input, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var ramps = input.Where(s => s.Type == SegmentType.Ramp).ToList();
        var relaxations = input.Where(s => s.Type == SegmentType.Relaxation).ToList();

        // Assuming each ramp is followed by a relaxation with the same initial strain.
        // We will match them in pairs chronologically.
        for (int i = 0; i < Math.Min(ramps.Count, relaxations.Count); i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var ramp = ramps[i];
            var relaxation = relaxations[i];

            double[] initialParams = CreateInitialParameters();
            double[] lowerBounds = CreateLowerBounds();
            double[] upperBounds = CreateUpperBounds();

            // 1. Fit Ramp (Optimize only A and B, lock the rest)
            double[] rampLowerBounds = (double[])lowerBounds.Clone();
            double[] rampUpperBounds = (double[])upperBounds.Clone();
            for (int j = 2; j < initialParams.Length; j++)
            {
                rampLowerBounds[j] = initialParams[j];
                rampUpperBounds[j] = initialParams[j];
            }

            CurveFitInput rampInput = new()
            {
                TimePoints = ramp.TimePoints,
                StrainPoints = ramp.ExperimentalStrain,
                StressPoints = ramp.ExperimentalStress,
                CalculateStress = (parameters, time, strain) =>
                {
                    MechanicalModelInput<SimplifiedFungConstitutiveParameters> currentInput = new()
                    {
                        MechanicalModelName = MechanicalModelName,
                        AcceptedStrainRange = new AcceptedRange
                        {
                            InitialPoint = ramp.ExperimentalStrain[0],
                            FinalPoint = ramp.ExperimentalStrain[^1],
                        },
                        MechanicalBehaviorType = MechanicalBehaviorType.StressStrain,
                        RampTimeConsideration = RampTimeConsideration.ConsiderWithViscoelasticEffect,
                        ViscoelasticEffect = ViscoelasticEffect.Relaxation,
                        Strain = new MechanicalParameter(ramp.ExperimentalStrain[0]),
                        Stress = new MechanicalParameter(ramp.ExperimentalStress[0]),
                        TimeStep = ramp.TimePoints[1] - ramp.TimePoints[0],
                        ConstitutiveParameters = MapArrayToParameters(parameters),
                    };
                    return MechanicalModelCalculator.CalculateStress(currentInput, time, strain);
                },
                LowerBounds = rampLowerBounds,
                UpperBounds = rampUpperBounds,
                EvaluateConstraintsAndPenalties = CreateEvaluateConstraintsAndPenalties(),
                InitialParameters = initialParams,
            };

            CurveFitOutput rampOutput = CurveFitter.Fit(rampInput);
            double[] intermediateParams = rampOutput.OptimizedParameters;

            // 2. Fit Relaxation (Optimize only Prony, lock A and B)
            double[] relLowerBounds = (double[])lowerBounds.Clone();
            double[] relUpperBounds = (double[])upperBounds.Clone();
            relLowerBounds[0] = intermediateParams[0];
            relUpperBounds[0] = intermediateParams[0];
            relLowerBounds[1] = intermediateParams[1];
            relUpperBounds[1] = intermediateParams[1];

            CurveFitInput relInput = new()
            {
                TimePoints = relaxation.TimePoints,
                StrainPoints = relaxation.ExperimentalStrain,
                StressPoints = relaxation.ExperimentalStress,
                CalculateStress = (parameters, time, strain) =>
                {
                    MechanicalModelInput<SimplifiedFungConstitutiveParameters> currentInput = new()
                    {
                        MechanicalModelName = MechanicalModelName,
                        AcceptedStrainRange = new AcceptedRange
                        {
                            InitialPoint = relaxation.ExperimentalStrain[0],
                            FinalPoint = relaxation.ExperimentalStrain[^1],
                        },
                        MechanicalBehaviorType = MechanicalBehaviorType.StressStrain,
                        RampTimeConsideration = RampTimeConsideration.Disregard,
                        ViscoelasticEffect = ViscoelasticEffect.Relaxation,
                        Strain = new MechanicalParameter(relaxation.ExperimentalStrain[0]),
                        Stress = new MechanicalParameter(relaxation.ExperimentalStress[0]),
                        TimeStep = relaxation.TimePoints[1] - relaxation.TimePoints[0],
                        ConstitutiveParameters = MapArrayToParameters(parameters),
                    };
                    return MechanicalModelCalculator.CalculateStress(currentInput, time, strain);
                },
                LowerBounds = relLowerBounds,
                UpperBounds = relUpperBounds,
                EvaluateConstraintsAndPenalties = CreateEvaluateConstraintsAndPenalties(),
                InitialParameters = intermediateParams,
            };

            CurveFitOutput relOutput = CurveFitter.Fit(relInput);
            yield return MapToOutput(relOutput);
        }
    }
}
