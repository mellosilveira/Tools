using MelloSilveiraTools.Core.Pipelines.Models;
using MelloSilveiraTools.Mathematics.Expressions;
using MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels.Viscoelasticity.QuasiLinear.SimplifiedFung;
using MelloSilveiraTools.MechanicsOfMaterials.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.QuasiLinear;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Algorithms;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.MathExpressions;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExtensionMethods;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Steps.CurveFitter;

/// <summary>
/// Thread-safe. Implements <see cref="IMechanicalModelCurveFitterStep"/> for the
/// Simplified Fung model (relaxation-only curve fitting). Implements <c>IPipelineStep</c> for telemetry.
/// </summary>
public sealed class SimplifiedFungRelaxationOnlyCurveFitterStep(
    ILogger<SimplifiedFungRelaxationOnlyCurveFitterStep> logger,
    ISimplifiedFungModelCalculator mechanicalModelCalculator,
    ICurveFitter curveFitter,
    IMathExpressionCurveFitter mathExpressionCurveFitter) : IMechanicalModelCurveFitterStep
{
    /// <inheritdoc />
    public string Name => nameof(SimplifiedFungRelaxationOnlyCurveFitterStep);

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

                double initialStress = relaxation.ExperimentalStress[0];
                double[] relaxationTime = relaxation.TimePoints.TranslateToOrigin();
                double[] normalizedStress = relaxation.ExperimentalStress.Normalize(relaxation.ExperimentalStress[0]);

                MathematicalCurveFitInput relaxationInput = new()
                {
                    NumberOfParameters = 7,
                    IndependentVariable = relaxationTime,
                    DependentVariable = normalizedStress,
                    LowerBounds = [0.0, 0.0, -10.0, 0.0, -100.0, 0.0, -1000.0],
                    UpperBounds = [1.0, 1.0, -0.1, 1.0, -10.0, 1.0, -100.0],
                    InitialParameters = [0.4, 0.2, -1.0, 0.2, -10.0, 0.2, -100.0]
                };
                SafeResult<CurveFitInput, CurveFitOutput> relaxationResult = mathExpressionCurveFitter.TryFit(relaxationInput);
                if (!relaxationResult.Success)
                {
                    logger.LogWarning(relaxationResult.FailedPayload?.Exception, "Failed to fit PronySeries for relaxation segment. Result: {@Result}", relaxationResult);
                    currentRamp = null;
                    continue;
                }

                CurveFitOutput relaxationOutput = relaxationResult.Output!;
                PronySeries reducedRelaxationFunction = new(
                    independentParameter: relaxationOutput.OptimizedParameters[0],
                    iteratorCoefficients: [relaxationOutput.OptimizedParameters[1], relaxationOutput.OptimizedParameters[2], relaxationOutput.OptimizedParameters[3], relaxationOutput.OptimizedParameters[4], relaxationOutput.OptimizedParameters[5], relaxationOutput.OptimizedParameters[6]]);

                if (currentRamp != null)
                {
                    yield return FitRamp(currentRamp, relaxation, reducedRelaxationFunction, RampTimeConsideration.ConsiderWithoutViscoelasticEffect, relaxationOutput.FinalError, relaxationOutput.Iterations);
                    yield return FitRamp(currentRamp, relaxation, reducedRelaxationFunction, RampTimeConsideration.ConsiderWithViscoelasticEffect, relaxationOutput.FinalError, relaxationOutput.Iterations);
                }
                else
                {
                    SimplifiedFungConstitutiveParameters constitutiveParameters = new(0, 0, reducedRelaxationFunction);
                    yield return new MechanicalModelCurveFitOutput(constitutiveParameters, relaxationOutput.FinalError, relaxationOutput.Iterations, new AcceptedRange(relaxation.ExperimentalStrain[0], relaxation.ExperimentalStrain[^1]));
                }
            }

            currentRamp = null;
        }
    }

    private MechanicalModelCurveFitOutput FitRamp(CurveSegment ramp, CurveSegment relaxation, PronySeries reducedRelaxationFunction, RampTimeConsideration rampTimeConsideration, double relaxationError, int relaxationIterations)
    {
        double timeStep = ramp.TimePoints[1] - ramp.TimePoints[0];
        double[] rampTimePoints = ramp.TimePoints.TranslateToOrigin();

        CurveFitInput rampInput = new()
        {
            IndependentVariables = [rampTimePoints, ramp.ExperimentalStrain],
            DependentVariable = ramp.ExperimentalStress,
            Calculate = (parameters, xValues) =>
            {
                MechanicalModelInput<SimplifiedFungConstitutiveParameters> currentInput = new()
                {
                    MechanicalModelName = nameof(MechanicalModel.SimplifiedFung),
                    AcceptedStrainRange = new AcceptedRange(ramp.ExperimentalStrain[0], ramp.ExperimentalStrain[^1]),
                    MechanicalBehaviorType = MechanicalBehaviorType.StressStrain,
                    RampTimeConsideration = rampTimeConsideration,
                    RampTime = relaxation.TimePoints[0],
                    ViscoelasticEffect = ViscoelasticEffect.Relaxation,
                    Strain = new MechanicalParameter(xValues[1]),
                    Stress = new MechanicalParameter(ramp.ExperimentalStress[0]),
                    TimeStep = timeStep,
                    ConstitutiveParameters = new SimplifiedFungConstitutiveParameters
                    {
                        ElasticStressConstant = parameters[0],
                        ElasticPowerConstant = parameters[1],
                        ReducedRelaxationFunction = reducedRelaxationFunction
                    }
                };
                return mechanicalModelCalculator.CalculateStress(currentInput, xValues[0], xValues[1]);
            },
            LowerBounds = [1e-6, 0.0],
            UpperBounds = [1e6, 100.0],
            InitialParameters = [1000.0, 1.0],
            EvaluateConstraintsAndPenalties = null
        };

        var rampOutput = curveFitter.Fit(rampInput);

        double totalError = relaxationError * rampOutput.FinalError;
        int totalIterations = relaxationIterations + rampOutput.Iterations;

        SimplifiedFungConstitutiveParameters finalParams = new()
        {
            ElasticStressConstant = rampOutput.OptimizedParameters[0],
            ElasticPowerConstant = rampOutput.OptimizedParameters[1],
            ReducedRelaxationFunction = reducedRelaxationFunction
        };
        return new MechanicalModelCurveFitOutput(finalParams, totalError, totalIterations, new AcceptedRange(relaxation.ExperimentalStrain[0], relaxation.ExperimentalStrain[^1]));
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }
}

