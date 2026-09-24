using MelloSilveiraTools.Mathematics.Expressions;
using MelloSilveiraTools.Mathematics.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels.Viscoelasticity.QuasiLinear.SimplifiedFung;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.QuasiLinear;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Algorithms;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.MathExpressions;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExtensionMethods;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Models;
using Microsoft.Extensions.DependencyInjection;
using System.Runtime.CompilerServices;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Steps.CurveFitter;

/// <summary>
/// Thread-safe. Implements <see cref="IMechanicalModelCurveFitterStep"/> for the
/// Simplified Fung model (relaxation-only curve fitting). Implements <c>IPipelineStep</c> for telemetry.
/// </summary>
public sealed class SimplifiedFungRelaxationOnlyCurveFitterStep(
    ISimplifiedFungModelCalculator mechanicalModelCalculator,
    ICurveFitter curveFitter,
    [FromKeyedServices(MathExpressionType.PronySeries)] IMathExpressionCurveFitter pronySeriesCurveFitter)
    : MechanicalModelCurveFitterStepBase
{
    private static readonly double[] RelaxationLowerBounds = [0.1, 0.1, -1000.0, 0.1, -1000.0, 0.1, -1000.0];
    private static readonly double[] RelaxationUpperBounds = [1.0, 1.0, 0, 1.0, 0, 1.0, 0];
    private static readonly double[] RelaxationInitialParameters = [0.4, 0.2, -0.1, 0.2, -1.0, 0.2, -100.0];
    private static readonly double[] RampLowerBounds = [1e-6, 0.0];
    private static readonly double[] RampUpperBounds = [1e6, 100.0];
    private static readonly double[] RampInitialParameters = [1000.0, 1.0];

    /// <inheritdoc />
    protected override string MechanicalModelName => nameof(MechanicalModel.SimplifiedFung);

    /// <inheritdoc />
    protected override MechanicalBehaviorType MechanicalBehaviorType => MechanicalBehaviorType.StressStrain;

    /// <inheritdoc />
    protected override ViscoelasticEffect ViscoelasticEffect => ViscoelasticEffect.Relaxation;

    /// <inheritdoc />
    public override async IAsyncEnumerable<MechanicalModelCurveFitOutput> ExecuteAsync(CurveSegment[] input, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        CurveSegment? currentRamp = null;

        foreach (CurveSegment segment in input)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (segment.Type is SegmentType.Ramp)
            {
                currentRamp = segment;
                continue;
            }

            if (segment.Type is SegmentType.Relaxation)
            {
                CurveSegment relaxation = segment;

                double[] relaxationTime = relaxation.TimePoints.TranslateToOrigin();
                double[] normalizedStress = relaxation.ExperimentalStress.Normalize(relaxation.ExperimentalStress[0]);

                MathematicalCurveFitInput relaxationInput = new()
                {
                    NumberOfParameters = 7,
                    IndependentVariable = relaxationTime,
                    DependentVariable = normalizedStress,
                    LowerBounds = RelaxationLowerBounds,
                    UpperBounds = RelaxationUpperBounds,
                    InitialParameters = RelaxationInitialParameters,
                    Profile = CurveFitProfile.Automatic,
                    ValidateParameters = parameters => parameters[2] > parameters[4] && parameters[4] > parameters[6]
                };

                CurveFitOutput relaxationOutput = pronySeriesCurveFitter.Fit(relaxationInput);

                PronySeries reducedRelaxationFunction = new(
                    independentParameter: relaxationOutput.OptimizedParameters[0],
                    iteratorCoefficients: [relaxationOutput.OptimizedParameters[1], relaxationOutput.OptimizedParameters[2], relaxationOutput.OptimizedParameters[3], relaxationOutput.OptimizedParameters[4], relaxationOutput.OptimizedParameters[5], relaxationOutput.OptimizedParameters[6]]);

                if (currentRamp != null)
                {
                    yield return FitRamp(reducedRelaxationFunction, relaxationOutput, relaxation, currentRamp, RampTimeConsideration.ConsiderWithoutViscoelasticEffect);
                    yield return FitRamp(reducedRelaxationFunction, relaxationOutput, relaxation, currentRamp, RampTimeConsideration.ConsiderWithViscoelasticEffect);
                }
                else
                {
                    SimplifiedFungConstitutiveParameters constitutiveParameters = new(0, 0, reducedRelaxationFunction);
                    AcceptedRange acceptedStrainRange = new(relaxation.ExperimentalStrain[0], relaxation.ExperimentalStrain[^1]);
                    yield return CreateCurveFitOutput(constitutiveParameters, acceptedStrainRange, relaxationOutput.RSquared, relaxationOutput.FinalError, relaxationOutput.Iterations);
                }
            }

            currentRamp = null;
        }
    }

    private MechanicalModelCurveFitOutput FitRamp(PronySeries reducedRelaxationFunction, CurveFitOutput relaxationOutput, CurveSegment relaxation, CurveSegment ramp, RampTimeConsideration rampTimeConsideration)
    {
        double[] rampTimePoints = ramp.TimePoints.TranslateToOrigin();
        CurveFitInput rampInput = new()
        {
            IndependentVariables = [rampTimePoints, ramp.ExperimentalStrain],
            DependentVariable = ramp.ExperimentalStress,
            Calculate = (parameters, xValues) =>
            {
                SimplifiedFungConstitutiveParameters constitutiveParameters = new(parameters[0], parameters[1], reducedRelaxationFunction);
                MechanicalModelInput<SimplifiedFungConstitutiveParameters> currentInput = CreateModelInput(ramp, constitutiveParameters, rampTimeConsideration, relaxation.TimePoints[0]);
                return mechanicalModelCalculator.CalculateStress(currentInput, xValues[0], xValues[1]);
            },
            LowerBounds = RampLowerBounds,
            UpperBounds = RampUpperBounds,
            InitialParameters = RampInitialParameters,
            Profile = CurveFitProfile.Automatic
        };
        CurveFitOutput rampOutput = curveFitter.Fit(rampInput);
        
        // TODO: MELHORAR CALCULO DE R^2 E ERRO.
        double precision = (relaxationOutput.RSquared + rampOutput.RSquared) / 2;
        double totalError = relaxationOutput.FinalError * rampOutput.FinalError;
        int totalIterations = relaxationOutput.Iterations + rampOutput.Iterations;

        SimplifiedFungConstitutiveParameters constitutiveParameters = new(rampOutput.OptimizedParameters[0], rampOutput.OptimizedParameters[1], reducedRelaxationFunction);
        AcceptedRange acceptedStrainRange = new(relaxation.ExperimentalStrain[0], relaxation.ExperimentalStrain[^1]);
        return CreateCurveFitOutput(constitutiveParameters, acceptedStrainRange, rampTimeConsideration, precision, totalError, totalIterations);
    }
}
