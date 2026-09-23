using MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels.Viscoelasticity.QuasiLinear.Fung;
using MelloSilveiraTools.MechanicsOfMaterials.Models;
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
    ICurveFitter curveFitter) : IMechanicalModelCurveFitterStep
{
    private const string MechanicalModelName = nameof(MechanicalModel.Fung);
    private const MechanicalBehaviorType MechanicalBehaviorType = MechanicalBehaviorType.StressStrain;
    private const ViscoelasticEffect ViscoelasticEffect = ViscoelasticEffect.Relaxation;

    private static readonly double[] RelaxationLowerBounds = [0.0, 1e-4, 1.0];
    private static readonly double[] RelaxationUpperBounds = [1.0, 0.1, 1e5];
    private static readonly double[] RelaxationInitialParameters = [0.1, 0.01, 10.0];
    private static readonly double[] RampLowerBounds = [1e-6, 0.0];
    private static readonly double[] RampUpperBounds = [1e6, 100.0];
    private static readonly double[] RampInitialParameters = [1000.0, 1.0];

    /// <inheritdoc />
    public string Name => nameof(FungRelaxationOnlyCurveFitterStep);

    /// <inheritdoc />
    public async IAsyncEnumerable<MechanicalModelCurveFitOutput> ExecuteAsync(CurveSegment[] input, [EnumeratorCancellation] CancellationToken cancellationToken = default)
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
                    LowerBounds = RelaxationLowerBounds,
                    UpperBounds = RelaxationUpperBounds,
                    InitialParameters = RelaxationInitialParameters,
                    Calculate = (parameters, xValues) =>
                    {
                        FungConstitutiveParameters constitutiveParameters = new(0.0, 0.0, new ReducedRelaxationFunction(parameters[0], parameters[1], parameters[2]));
                        MechanicalModelInput<FungConstitutiveParameters> currentInput = CreateModelInput(relaxation, constitutiveParameters, RampTimeConsideration.Disregard);
                        return mechanicalModelCalculator.CalculateReducedRelaxationFunction(currentInput, xValues[0]);
                    },
                    EvaluateConstraintsAndPenalties = null
                };
                CurveFitOutput relaxationOutput = curveFitter.Fit(relaxationInput);

                ReducedRelaxationFunction reducedRelaxationFunction = new(relaxationOutput.OptimizedParameters[0], relaxationOutput.OptimizedParameters[1], relaxationOutput.OptimizedParameters[2]);

                if (currentRamp != null)
                {
                    yield return FitRamp(currentRamp, relaxation, reducedRelaxationFunction, RampTimeConsideration.ConsiderWithoutViscoelasticEffect, relaxationOutput.FinalError, relaxationOutput.Iterations);
                    yield return FitRamp(currentRamp, relaxation, reducedRelaxationFunction, RampTimeConsideration.ConsiderWithViscoelasticEffect, relaxationOutput.FinalError, relaxationOutput.Iterations);
                }
                else
                {
                    FungConstitutiveParameters constitutiveParameters = new(0.0, 0.0, reducedRelaxationFunction);
                    yield return new MechanicalModelCurveFitOutput(
                        MechanicalModelName,
                        constitutiveParameters,
                        new AcceptedRange(relaxation.ExperimentalStrain[0], relaxation.ExperimentalStrain[^1]),
                        MechanicalBehaviorType,
                        ViscoelasticEffect,
                        RampTimeConsideration.Disregard,
                        relaxationOutput.FinalError,
                        relaxationOutput.Iterations);
                }
            }

            currentRamp = null;
        }
    }

    private MechanicalModelCurveFitOutput FitRamp(CurveSegment ramp, CurveSegment relaxation, ReducedRelaxationFunction reducedRelaxationFunction, RampTimeConsideration rampTimeConsideration, double relaxationError, int relaxationIterations)
    {
        double timeStep = ramp.TimePoints[1] - ramp.TimePoints[0];
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
            EvaluateConstraintsAndPenalties = null
        };

        CurveFitOutput rampOutput = curveFitter.Fit(rampInput);

        double totalError = relaxationError * rampOutput.FinalError;
        int totalIterations = relaxationIterations + rampOutput.Iterations;

        FungConstitutiveParameters constitutiveParameters = new(rampOutput.OptimizedParameters[0], rampOutput.OptimizedParameters[1], reducedRelaxationFunction);
        return new MechanicalModelCurveFitOutput(
            MechanicalModelName,
            constitutiveParameters,
            new AcceptedRange(ramp.ExperimentalStrain[0], relaxation.ExperimentalStrain[^1]),
            MechanicalBehaviorType,
            ViscoelasticEffect,
            rampTimeConsideration,
            totalError,
            totalIterations);
    }

    private static MechanicalModelInput<FungConstitutiveParameters> CreateModelInput(CurveSegment segment, FungConstitutiveParameters constitutiveParameters, RampTimeConsideration rampTimeConsideration, double? rampTime = null) => new()
    {
        MechanicalModelName = MechanicalModelName,
        AcceptedStrainRange = new AcceptedRange(segment.ExperimentalStrain[0], segment.ExperimentalStrain[^1]),
        MechanicalBehaviorType = MechanicalBehaviorType,
        ViscoelasticEffect = ViscoelasticEffect,
        RampTimeConsideration = rampTimeConsideration,
        RampTime = rampTime,
        Strain = new MechanicalParameter(segment.ExperimentalStrain[0]),
        Stress = new MechanicalParameter(segment.ExperimentalStress[0]),
        TimeStep = segment.TimePoints[1] - segment.TimePoints[0],
        ConstitutiveParameters = constitutiveParameters
    };

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }
}
