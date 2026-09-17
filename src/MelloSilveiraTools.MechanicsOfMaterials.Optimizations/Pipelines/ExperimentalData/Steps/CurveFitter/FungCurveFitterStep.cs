using MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels.Viscoelasticity.QuasiLinear.Fung;
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
/// Fung model (full curve fitting). Implements <c>IPipelineStep</c> for telemetry.
/// </summary>
public sealed class FungCurveFitterStep(
    IFungModelCalculator mechanicalModelCalculator,
    ICurveFitter curveFitter) : IMechanicalModelCurveFitterStep
{
    /// <inheritdoc />
    public string Name => nameof(FungCurveFitterStep);

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
                    MechanicalModelInput<FungConstitutiveParameters> currentInput = new()
                    {
                        MechanicalModelName = nameof(MechanicalModel.Fung),
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
                        ConstitutiveParameters = new FungConstitutiveParameters
                        {
                            ElasticStressConstant = parameters[0],
                            ElasticPowerConstant = parameters[1],
                            ReducedRelaxationFunction = new ReducedRelaxationFunction(parameters[2], parameters[3], parameters[4]),
                        },
                    };
                    return mechanicalModelCalculator.CalculateStress(currentInput, xValues[0], xValues[1]);
                },
                LowerBounds = [1e-6, 0.0, 0.0, 1e-4, 1.0],
                UpperBounds = [1e5, 100.0, 1.0, 0.1, 1e5],
                EvaluateConstraintsAndPenalties = null,
                InitialParameters = [1000.0, 1.0, 0.1, 0.01, 10.0],
            };

            CurveFitOutput curveFitOutput = curveFitter.Fit(curveFitInput);

            FungConstitutiveParameters finalParams = new FungConstitutiveParameters
            {
                ElasticStressConstant = curveFitOutput.OptimizedParameters[0],
                ElasticPowerConstant = curveFitOutput.OptimizedParameters[1],
                ReducedRelaxationFunction = new ReducedRelaxationFunction(curveFitOutput.OptimizedParameters[2], curveFitOutput.OptimizedParameters[3], curveFitOutput.OptimizedParameters[4]),
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
