using MelloSilveiraTools.Mathematics.Expressions;
using MelloSilveiraTools.Mathematics.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels.Viscoelasticity.QuasiLinear.SimplifiedFung;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.QuasiLinear;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Algorithms;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.MathExpressions;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExtensionMethods;
using Microsoft.Extensions.DependencyInjection;
using System.Runtime.CompilerServices;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Pipelines.Steps.CurveFitter;

/// <summary>
/// Thread-safe. Implements <see cref="IMechanicalModelCurveFitterStep"/> for the
/// Simplified Fung model (relaxation-only curve fitting). Implements <c>IPipelineStep</c> for telemetry.
/// </summary>
/// <param name="mechanicalModelCalculator">The calculator for Simplified Fung viscoelastic constitutive calculations.</param>
/// <param name="curveFitter">The optimization engine for non-linear regression.</param>
/// <param name="pronySeriesCurveFitter">The Prony series curve fitting decorator.</param>
/// <param name="targetRSquared">Mandatory target R² determination coefficient for early-exit and precision monitoring.</param>
public sealed class SimplifiedFungRelaxationOnlyCurveFitterStep(
    ISimplifiedFungModelCalculator mechanicalModelCalculator,
    ICurveFitter curveFitter,
    [FromKeyedServices(MathExpressionType.PronySeries)] IMathExpressionCurveFitter pronySeriesCurveFitter,
    double targetRSquared)
    : MechanicalModelCurveFitterStepBase(targetRSquared)
{
    // 3-Term Prony Series: G(t) = c + a_0 * e^(lambda_0 * t) + a_1 * e^(lambda_1 * t) + a_2 * e^(lambda_2 * t).
    // Parameter vector layout: [0] = c, [1] = a_0, [2] = lambda_0, [3] = a_1, [4] = lambda_1, [5] = a_2, [6] = lambda_2.
    // Three distinct physical relaxation scales for biological soft tissues spanning up to 1600 seconds:
    // - Slow mode (equilibrium approach): lambda_0 in [-1.0, -1e-6] (tau_0 ~ 1 s to 1e6 s), init: lambda_0 = -0.002 (tau_0 ~ 500 s).
    // - Medium mode (viscoelastic decay): lambda_1 in [-10.0, -1e-5] (tau_1 ~ 0.1 s to 1e5 s), init: lambda_1 = -0.04 (tau_1 ~ 25 s).
    // - Fast mode (initial fluid/microfibril sliding): lambda_2 in [-100.0, -1e-4] (tau_2 ~ 0.01 s to 1e4 s), init: lambda_2 = -0.8 (tau_2 ~ 1.25 s).
    // Ordered constraint (lambda_0 > lambda_1 > lambda_2) ensures parameter identifiability and prevents mode swapping.
    private static readonly double[] RelaxationLowerBounds = [0.0, 0.0, -1.0, 0.0, -10.0, 0.0, -100.0];
    private static readonly double[] RelaxationUpperBounds = [1.0, 1.0, -1e-6, 1.0, -1e-5, 1.0, -1e-4];
    private static readonly double[] RelaxationInitialParameters = [0.5, 0.2, -0.002, 0.15, -0.04, 0.15, -0.8];
    private static readonly double[] RampLowerBounds = [1e-6, 0.0];
    private static readonly double[] RampUpperBounds = [1e6, 100.0];
    private static readonly double[] RampInitialParameters = [1000.0, 1.0];

    /// <inheritdoc />
    protected override string MechanicalModelName => nameof(MechanicalModel.SimplifiedFung);

    /// <inheritdoc />
    protected override LoadResponseRelationship LoadResponseRelationship => LoadResponseRelationship.StressStrain;

    /// <inheritdoc />
    protected override ViscoelasticEffect ViscoelasticEffect => ViscoelasticEffect.Relaxation;

    private CurveSegment? _currentRamp;

    /// <inheritdoc />
    public override async IAsyncEnumerable<MechanicalModelCurveFitOutput> ExecuteAsync(CurveSegment segment, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (segment.Type is SegmentType.Ramp)
        {
            _currentRamp = segment;
        }
        else if (segment.Type is SegmentType.Relaxation)
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
                ValidateParameters = parameters => parameters[2] > parameters[4] && parameters[4] > parameters[6],
                TargetRSquared = TargetRSquared
            };

            CurveFitOutput relaxationOutput = pronySeriesCurveFitter.Fit(relaxationInput);

            PronySeries reducedRelaxationFunction = new(
                independentParameter: relaxationOutput.OptimizedParameters[0],
                iteratorCoefficients: [relaxationOutput.OptimizedParameters[1], relaxationOutput.OptimizedParameters[2], relaxationOutput.OptimizedParameters[3], relaxationOutput.OptimizedParameters[4], relaxationOutput.OptimizedParameters[5], relaxationOutput.OptimizedParameters[6]]);

            if (_currentRamp != null)
            {
                yield return FitRamp(reducedRelaxationFunction, relaxationOutput, relaxation, _currentRamp, RampTimeConsideration.ConsiderWithoutViscoelasticEffect);
                yield return FitRamp(reducedRelaxationFunction, relaxationOutput, relaxation, _currentRamp, RampTimeConsideration.ConsiderWithViscoelasticEffect);
            }
            else
            {
                SimplifiedFungConstitutiveParameters constitutiveParameters = new(0, 0, reducedRelaxationFunction);
                AcceptedRange acceptedStrainRange = new(relaxation.ExperimentalStrain[0], relaxation.ExperimentalStrain[^1]);
                yield return CreateCurveFitOutput([relaxation], constitutiveParameters, acceptedStrainRange, relaxationOutput.RSquared, relaxationOutput.FinalError, relaxationOutput.Iterations);
            }

            _currentRamp = null;
        }
        else
        {
            _currentRamp = null;
        }

        await Task.CompletedTask;
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
            Profile = CurveFitProfile.Automatic,
            TargetRSquared = TargetRSquared
        };
        CurveFitOutput rampOutput = curveFitter.Fit(rampInput);

        int rampPoints = ramp.ExperimentalStrain.Length;
        int relaxationPoints = relaxation.ExperimentalStrain.Length;
        int totalPoints = rampPoints + relaxationPoints;

        double precision = (rampPoints * rampOutput.RSquared + relaxationPoints * relaxationOutput.RSquared) / totalPoints;
        double totalError = (rampPoints * rampOutput.FinalError + relaxationPoints * relaxationOutput.FinalError) / totalPoints;
        int totalIterations = relaxationOutput.Iterations + rampOutput.Iterations;

        SimplifiedFungConstitutiveParameters constitutiveParameters = new(rampOutput.OptimizedParameters[0], rampOutput.OptimizedParameters[1], reducedRelaxationFunction);
        AcceptedRange acceptedStrainRange = new(relaxation.ExperimentalStrain[0], relaxation.ExperimentalStrain[^1]);
        return CreateCurveFitOutput([ramp, relaxation], constitutiveParameters, acceptedStrainRange, rampTimeConsideration, precision, totalError, totalIterations);
    }
}
