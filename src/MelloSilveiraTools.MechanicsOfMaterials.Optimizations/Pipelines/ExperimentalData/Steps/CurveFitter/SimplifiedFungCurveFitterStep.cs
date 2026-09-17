using MelloSilveiraTools.Mathematics.Expressions;
using MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels.Viscoelasticity.QuasiLinear.SimplifiedFung;
using MelloSilveiraTools.MechanicsOfMaterials.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.QuasiLinear;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Algorithms.CurveFitting;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Models.CurveFitting;
using System.Runtime.CompilerServices;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Steps.CurveFitter;

/// <summary>
/// Thread-safe. Implements <see cref="IMechanicalModelCurveFitterStep"/> for the
/// Simplified Fung model (full curve fitting). Implements <c>IPipelineStep</c> for telemetry.
/// </summary>
public sealed class SimplifiedFungCurveFitterStep(
    ISimplifiedFungModelCalculator mechanicalModelCalculator,
    ICurveFitter curveFitter) : IMechanicalModelCurveFitterStep
{
    /// <inheritdoc />
    public string Name => nameof(SimplifiedFungCurveFitterStep);

    /// <inheritdoc />
    public async IAsyncEnumerable<MechanicalModelCurveFitOutput> ExecuteAsync(CurveSegment[] input, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (CurveSegment curveSegment in input)
        {
            cancellationToken.ThrowIfCancellationRequested();

            CurveFitInput curveFitInput = new()
            {
                IndependentVariables = [curveSegment.TimePoints, curveSegment.ExperimentalStrain],
                DependentVariable = curveSegment.ExperimentalStress,
                Calculate = (parameters, xValues) =>
                {
                    MechanicalModelInput<SimplifiedFungConstitutiveParameters> currentInput = new()
                    {
                        MechanicalModelName = nameof(MechanicalModel.SimplifiedFung),
                        AcceptedStrainRange = new AcceptedRange
                        {
                            InitialPoint = curveSegment.ExperimentalStrain[0],
                            FinalPoint = curveSegment.ExperimentalStrain[^1],
                        },
                        MechanicalBehaviorType = MechanicalBehaviorType.StressStrain,
                        RampTimeConsideration = RampTimeConsideration.ConsiderWithViscoelasticEffect,
                        ViscoelasticEffect = ViscoelasticEffect.Relaxation,
                        Strain = new MechanicalParameter(curveSegment.ExperimentalStrain[0]),
                        Stress = new MechanicalParameter(curveSegment.ExperimentalStress[0]),
                        TimeStep = curveSegment.TimePoints[1] - curveSegment.TimePoints[0],
                        ConstitutiveParameters = new SimplifiedFungConstitutiveParameters
                        {
                            ElasticStressConstant = parameters[0],
                            ElasticPowerConstant = parameters[1],
                            ReducedRelaxationFunction = new PronySeries(null, null, 1.0, [parameters[2], -1.0 / parameters[3]]),
                        },
                    };
                    return mechanicalModelCalculator.CalculateStress(currentInput, xValues[0], xValues[1]);
                },
                LowerBounds = [1e-6, 0.0, 0.0, 1e-4],
                UpperBounds = [1e5, 100.0, 1.0, 100.0],
                EvaluateConstraintsAndPenalties = null,
                InitialParameters = [1000.0, 1.0, 0.1, 10.0],
            };

            CurveFitOutput curveFitOutput = curveFitter.Fit(curveFitInput);

            SimplifiedFungConstitutiveParameters finalParams = new SimplifiedFungConstitutiveParameters
            {
                ElasticStressConstant = curveFitOutput.OptimizedParameters[0],
                ElasticPowerConstant = curveFitOutput.OptimizedParameters[1],
                ReducedRelaxationFunction = new PronySeries(null, null, 1.0, [curveFitOutput.OptimizedParameters[2], -1.0 / curveFitOutput.OptimizedParameters[3]]),
            };

            yield return new MechanicalModelCurveFitOutput(finalParams, curveFitOutput.FinalError, curveFitOutput.Iterations, new AcceptedRange { InitialPoint = curveSegment.ExperimentalStrain[0], FinalPoint = curveSegment.ExperimentalStrain[^1] });
        }
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }
}
