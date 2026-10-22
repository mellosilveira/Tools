using MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels.Viscoelasticity.QuasiLinear.Fung;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.QuasiLinear;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Algorithms;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExtensionMethods;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Models;
using System.Runtime.CompilerServices;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Steps.CurveFitter.Fung;

/// <summary>
/// Thread-safe. Implements <see cref="IMechanicalModelCurveFitterStep"/> for the
/// Fung model (relaxation-only curve fitting). Implements <c>IPipelineStep</c> for telemetry.
/// <para>
/// <b>Analytical Pre-Estimation (Warm-Start):</b><br/>
/// Employs <see cref="FungLogarithmicEstimator"/> based on Y.C. Fung (1993, Section 7.6, p. 302)
/// to analytically deduce the logarithmic slope <c>S</c> and fast relaxation time <c>τ₁</c> from
/// the early linear decay and transition knee. Due to experimental duration limits on soft tissue viability
/// (<c>t_max ~ 400 - 1600 s</c>), <c>τ₂</c> is seeded beyond <c>t_max</c> and refined numerically alongside <c>C</c>.
/// Physical parameter bounds and defaults are unified in <see cref="FungLogarithmicEstimator"/>.
/// </para>
/// </summary>
/// <param name="mechanicalModelCalculator">The calculator for Fung viscoelastic constitutive calculations.</param>
/// <param name="curveFitter">The optimization engine for non-linear regression.</param>
/// <param name="targetRSquared">Mandatory target R² determination coefficient for early-exit and precision monitoring.</param>
public sealed class FungRelaxationOnlyCurveFitterStep(
    IFungModelCalculator mechanicalModelCalculator,
    ICurveFitter curveFitter,
    double targetRSquared) : MechanicalModelCurveFitterStepBase(targetRSquared)
{
    // Ramp parameter bounds: [0] = ElasticStiffness (c), [1] = NonLinearExponent (a)
    private static readonly double[] RampLowerBounds = [1e-6, 0.0];
    private static readonly double[] RampUpperBounds = [1e6, 100.0];
    private static readonly double[] RampInitialParameters = [1000.0, 1.0];

    /// <inheritdoc />
    protected override string MechanicalModelName => nameof(MechanicalModel.Fung);

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

            // Analytical pre-estimation using logarithmic time regression (Y.C. Fung, 1993, p. 302).
            // Quality gate: if the logarithmic R² meets the minimum linearity threshold (>= 0.85), warm-start the optimizer
            // with the analytically estimated parameters; otherwise, fall back safely to standard physical defaults.
            FungLogarithmicEstimationResult logEstimation = FungLogarithmicEstimator.Estimate(relaxationTime, normalizedStress);
            double[] initialParameters = logEstimation.RSquared >= FungLogarithmicEstimator.MinimumLogarithmicRSquared
                ? [logEstimation.InitialC, logEstimation.FastRelaxationTime, logEstimation.SlowRelaxationTime]
                : FungLogarithmicEstimator.DefaultInitialParameters;

            CurveFitInput relaxationInput = new()
            {
                IndependentVariables = [relaxationTime],
                DependentVariable = normalizedStress,
                Calculate = (parameters, xValues) =>
                {
                    FungConstitutiveParameters constitutiveParameters = new(0.0, 0.0, new ReducedRelaxationFunction(parameters[0], parameters[1], parameters[2]));
                    MechanicalModelInput<FungConstitutiveParameters> currentInput = CreateModelInput(relaxation, constitutiveParameters, RampTimeConsideration.Disregard);
                    return mechanicalModelCalculator.CalculateReducedRelaxationFunction(currentInput, xValues[0]);
                },
                LowerBounds = FungLogarithmicEstimator.RelaxationLowerBounds,
                UpperBounds = FungLogarithmicEstimator.RelaxationUpperBounds,
                InitialParameters = initialParameters,
                Profile = CurveFitProfile.Standard,
                // parameters[1] = FastRelaxationTime, parameters[2] = SlowRelaxationTime (guaranteed by UpperBounds[1] < LowerBounds[2])
                ValidateParameters = parameters => parameters[1] < parameters[2],
                TargetRSquared = TargetRSquared
            };

            CurveFitOutput relaxationOutput = curveFitter.Fit(relaxationInput);
            ReducedRelaxationFunction reducedRelaxationFunction = new(relaxationOutput.OptimizedParameters[0], relaxationOutput.OptimizedParameters[1], relaxationOutput.OptimizedParameters[2]);

            if (_currentRamp != null)
            {
                yield return FitRamp(reducedRelaxationFunction, relaxationOutput, relaxation, _currentRamp, RampTimeConsideration.ConsiderWithoutViscoelasticEffect);
                yield return FitRamp(reducedRelaxationFunction, relaxationOutput, relaxation, _currentRamp, RampTimeConsideration.ConsiderWithViscoelasticEffect);
            }
            else
            {
                FungConstitutiveParameters constitutiveParameters = new(0.0, 0.0, reducedRelaxationFunction);
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

    private MechanicalModelCurveFitOutput FitRamp(ReducedRelaxationFunction reducedRelaxationFunction, CurveFitOutput relaxationOutput, CurveSegment relaxation, CurveSegment ramp, RampTimeConsideration rampTimeConsideration)
    {
        double[] rampTimePoints = ramp.TimePoints.TranslateToOrigin();
        CurveFitInput rampInput = new()
        {
            IndependentVariables = [rampTimePoints, ramp.ExperimentalStrain],
            DependentVariable = ramp.ExperimentalStress,
            Calculate = (parameters, xValues) =>
            {
                FungConstitutiveParameters constitutiveParameters = new(parameters[0], parameters[1], reducedRelaxationFunction);
                MechanicalModelInput<FungConstitutiveParameters> currentInput = CreateModelInput(ramp, constitutiveParameters, rampTimeConsideration, relaxation.TimePoints[0]);
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
        int totalIterations = rampOutput.Iterations + relaxationOutput.Iterations;

        FungConstitutiveParameters constitutiveParameters = new(rampOutput.OptimizedParameters[0], rampOutput.OptimizedParameters[1], reducedRelaxationFunction);
        AcceptedRange accepteStrainRange = new(ramp.ExperimentalStrain[0], relaxation.ExperimentalStrain[^1]);
        return CreateCurveFitOutput([ramp, relaxation], constitutiveParameters, accepteStrainRange, rampTimeConsideration, precision, totalError, totalIterations);
    }
}
