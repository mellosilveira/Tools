using MelloSilveiraTools.Mathematics.Expressions;
using MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels.Viscoelasticity.QuasiLinear.SimplifiedFung;
using MelloSilveiraTools.MechanicsOfMaterials.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.QuasiLinear;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Algorithms;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Models;
using System.Runtime.CompilerServices;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Steps.CurveFitter;

/// <summary>
/// Thread-safe. Implements <see cref="IMechanicalModelCurveFitterStep"/> for the
/// Simplified Fung model (relaxation-only curve fitting). Implements <c>IPipelineStep</c> for telemetry.
/// </summary>
public sealed class SimplifiedFungRelaxationOnlyCurveFitterStep(
    ISimplifiedFungModelCalculator mechanicalModelCalculator,
    ICurveFitter curveFitter) : IMechanicalModelCurveFitterStep
{
    /// <inheritdoc />
    public string Name => nameof(SimplifiedFungRelaxationOnlyCurveFitterStep);

    /// <inheritdoc />
    public async IAsyncEnumerable<MechanicalModelCurveFitOutput> ExecuteAsync(CurveSegment[] input, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var ramps = input.Where(cs => cs.Type == SegmentType.Ramp).ToList();
        var relaxations = input.Where(cs => cs.Type == SegmentType.Relaxation).ToList();

        for (int i = 0; i < relaxations.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var relaxation = relaxations[i];

            // Busca a rampa que precede esta relaxao
            var matchingRamp = ramps.LastOrDefault(r => r.TimePoints.Last() <= relaxation.TimePoints.First());

            double a = 1000.0, b = 1.0;

            if (matchingRamp != null)
            {
                CurveFitInput rampInput = new()
                {
                    IndependentVariables = [matchingRamp.TimePoints, matchingRamp.ExperimentalStrain],
                    DependentVariable = matchingRamp.ExperimentalStress,
                    Calculate = (parameters, xValues) =>
                    {
                        MechanicalModelInput<SimplifiedFungConstitutiveParameters> currentInput = new()
                        {
                            MechanicalModelName = nameof(MechanicalModel.SimplifiedFung),
                            AcceptedStrainRange = new AcceptedRange(matchingRamp.ExperimentalStrain[0], matchingRamp.ExperimentalStrain[^1] ),
                            MechanicalBehaviorType = MechanicalBehaviorType.StressStrain,
                            RampTimeConsideration = RampTimeConsideration.Disregard,
                            ViscoelasticEffect = ViscoelasticEffect.Relaxation,
                            Strain = new MechanicalParameter(matchingRamp.ExperimentalStrain[0]),
                            Stress = new MechanicalParameter(matchingRamp.ExperimentalStress[0]),
                            TimeStep = matchingRamp.TimePoints[1] - matchingRamp.TimePoints[0],
                            ConstitutiveParameters = new SimplifiedFungConstitutiveParameters
                            {
                                ElasticStressConstant = parameters[0],
                                ElasticPowerConstant = parameters[1],
                                ReducedRelaxationFunction = new PronySeries(null, null, 1.0, [0, -1.0]),
                            },
                        };
                        return mechanicalModelCalculator.CalculateStress(currentInput, xValues[0], xValues[1]);
                    },
                    LowerBounds = [1e-6, 0.0],
                    UpperBounds = [1e5, 100.0],
                    EvaluateConstraintsAndPenalties = null,
                    InitialParameters = [1000.0, 1.0],
                };

                var rampOutput = curveFitter.Fit(rampInput);
                a = rampOutput.OptimizedParameters[0];
                b = rampOutput.OptimizedParameters[1];
            }

            CurveFitInput relInput = new()
            {
                IndependentVariables = [relaxation.TimePoints, relaxation.ExperimentalStrain],
                DependentVariable = relaxation.ExperimentalStress,
                Calculate = (parameters, xValues) =>
                {
                    MechanicalModelInput<SimplifiedFungConstitutiveParameters> currentInput = new()
                    {
                        MechanicalModelName = nameof(MechanicalModel.SimplifiedFung),
                        AcceptedStrainRange = new AcceptedRange(relaxation.ExperimentalStrain[0], relaxation.ExperimentalStrain[^1] ),
                        MechanicalBehaviorType = MechanicalBehaviorType.StressStrain,
                        RampTimeConsideration = RampTimeConsideration.Disregard,
                        ViscoelasticEffect = ViscoelasticEffect.Relaxation,
                        Strain = new MechanicalParameter(relaxation.ExperimentalStrain[0]),
                        Stress = new MechanicalParameter(relaxation.ExperimentalStress[0]),
                        TimeStep = relaxation.TimePoints[1] - relaxation.TimePoints[0],
                        ConstitutiveParameters = new SimplifiedFungConstitutiveParameters
                        {
                            ElasticStressConstant = a,
                            ElasticPowerConstant = b,
                            ReducedRelaxationFunction = new PronySeries(null, null, 1.0, [parameters[0], -1.0 / parameters[1]]),
                        },
                    };
                    return mechanicalModelCalculator.CalculateStress(currentInput, xValues[0], xValues[1]);
                },
                LowerBounds = [0.0, 1e-4],
                UpperBounds = [1.0, 100.0],
                EvaluateConstraintsAndPenalties = null,
                InitialParameters = [0.1, 10.0],
            };

            var relOutput = curveFitter.Fit(relInput);

            SimplifiedFungConstitutiveParameters finalParams = new SimplifiedFungConstitutiveParameters
            {
                ElasticStressConstant = a,
                ElasticPowerConstant = b,
                ReducedRelaxationFunction = new PronySeries(null, null, 1.0, [relOutput.OptimizedParameters[0], -1.0 / relOutput.OptimizedParameters[1]]),
            };

            yield return new MechanicalModelCurveFitOutput(finalParams, relOutput.FinalError, relOutput.Iterations, new AcceptedRange(relaxation.ExperimentalStrain[0], relaxation.ExperimentalStrain[^1] ));
        }
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }
}

