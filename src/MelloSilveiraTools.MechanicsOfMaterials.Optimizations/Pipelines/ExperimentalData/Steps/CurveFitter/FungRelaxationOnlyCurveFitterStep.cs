using MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels.Viscoelasticity.QuasiLinear.Fung;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.QuasiLinear;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Algorithms;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExtensionMethods;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Models;
using System.Runtime.CompilerServices;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Steps.CurveFitter;

/// <summary>
/// Thread-safe. Implements <see cref="IMechanicalModelCurveFitterStep"/> for the
/// Fung model (relaxation-only curve fitting). Implements <c>IPipelineStep</c> for telemetry.
/// </summary>
public sealed class FungRelaxationOnlyCurveFitterStep(
    IFungModelCalculator mechanicalModelCalculator,
    ICurveFitter curveFitter) : MechanicalModelCurveFitterStepBase
{
    private static readonly double[] RelaxationLowerBounds = [0.0, 1e-4, 1.0];
    private static readonly double[] RelaxationUpperBounds = [1.0, 0.1, 1e5];
    private static readonly double[] RelaxationInitialParameters = [0.1, 0.01, 10.0];
    private static readonly double[] RampLowerBounds = [1e-6, 0.0];
    private static readonly double[] RampUpperBounds = [1e6, 100.0];
    private static readonly double[] RampInitialParameters = [1000.0, 1.0];

    /// <inheritdoc />
    protected override string MechanicalModelName => nameof(MechanicalModel.Fung);

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
                    LowerBounds = RelaxationLowerBounds,
                    UpperBounds = RelaxationUpperBounds,
                    InitialParameters = RelaxationInitialParameters,
                    Profile = CurveFitProfile.Automatic,
                    // parameters[1] = FastRelaxationTime, parameters[2] = SlowRelaxationTime
                    ValidateParameters = parameters => parameters[1] < parameters[2]
                };

                CurveFitOutput relaxationOutput = curveFitter.Fit(relaxationInput);
                ReducedRelaxationFunction reducedRelaxationFunction = new(relaxationOutput.OptimizedParameters[0], relaxationOutput.OptimizedParameters[1], relaxationOutput.OptimizedParameters[2]);

                if (currentRamp != null)
                {
                    yield return FitRamp(reducedRelaxationFunction, relaxationOutput, relaxation, currentRamp, RampTimeConsideration.ConsiderWithoutViscoelasticEffect);
                    yield return FitRamp(reducedRelaxationFunction, relaxationOutput, relaxation, currentRamp, RampTimeConsideration.ConsiderWithViscoelasticEffect);
                }
                else
                {
                    FungConstitutiveParameters constitutiveParameters = new(0.0, 0.0, reducedRelaxationFunction);
                    AcceptedRange acceptedStrainRange = new(relaxation.ExperimentalStrain[0], relaxation.ExperimentalStrain[^1]);
                    yield return CreateCurveFitOutput(constitutiveParameters, acceptedStrainRange, relaxationOutput.RSquared, relaxationOutput.FinalError, relaxationOutput.Iterations);
                }
            }

            currentRamp = null;
        }
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
            Profile = CurveFitProfile.Automatic
        };

        CurveFitOutput rampOutput = curveFitter.Fit(rampInput);
        
        // TODO: MELHORAR CALCULO DE R^2 E ERRO.
        double precision = (relaxationOutput.RSquared + rampOutput.RSquared) / 2;
        double totalError = relaxationOutput.FinalError * rampOutput.FinalError;
        int totalIterations = relaxationOutput.Iterations + rampOutput.Iterations;

        FungConstitutiveParameters constitutiveParameters = new(rampOutput.OptimizedParameters[0], rampOutput.OptimizedParameters[1], reducedRelaxationFunction);
        AcceptedRange accepteStrainRange = new(ramp.ExperimentalStrain[0], relaxation.ExperimentalStrain[^1]);
        return CreateCurveFitOutput(constitutiveParameters, accepteStrainRange, rampTimeConsideration, precision, totalError, totalIterations);
    }
}
