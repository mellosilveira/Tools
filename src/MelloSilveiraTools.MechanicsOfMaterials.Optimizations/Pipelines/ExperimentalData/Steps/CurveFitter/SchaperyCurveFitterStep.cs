using MelloSilveiraTools.Mathematics.Functions;
using MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels.Viscoelasticity.NonLinear.Schapery;
using MelloSilveiraTools.MechanicsOfMaterials.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.NonLinear.Schapery;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Algorithms;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Models;
using System.Runtime.CompilerServices;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Steps.CurveFitter;

/// <summary>
/// Thread-safe. Implements <see cref="IMechanicalModelCurveFitterStep"/> for the
/// Schapery model (full curve fitting). Implements <c>IPipelineStep</c> for telemetry.
/// </summary>
public sealed class SchaperyCurveFitterStep(
    ISchaperyModelCalculator mechanicalModelCalculator,
    ICurveFitter curveFitter) : IMechanicalModelCurveFitterStep
{
    /// <inheritdoc />
    public string Name => nameof(SchaperyCurveFitterStep);

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
                    MechanicalModelInput<SchaperyConstitutiveParameters> currentInput = new()
                    {
                        MechanicalModelName = nameof(MechanicalModel.Schapery),
                        AcceptedStrainRange = new AcceptedRange(curveSegment.ExperimentalStrain[0], curveSegment.ExperimentalStrain[^1]),
                        MechanicalBehaviorType = MechanicalBehaviorType.StressStrain,
                        RampTimeConsideration = RampTimeConsideration.ConsiderWithViscoelasticEffect,
                        ViscoelasticEffect = ViscoelasticEffect.Relaxation,
                        Strain = new MechanicalParameter(curveSegment.ExperimentalStrain[0]),
                        Stress = new MechanicalParameter(curveSegment.ExperimentalStress[0]),
                        TimeStep = curveSegment.TimePoints[1] - curveSegment.TimePoints[0],
                        ConstitutiveParameters = new SchaperyConstitutiveParameters
                        {
                            Ge = parameters[0],
                            TransientRelaxationFunction = new PowerLaw(null, null, [parameters[1], parameters[2]]),
                            He = new PolynomialFunction(null, null, [parameters[3]]),
                            H1 = new PolynomialFunction(null, null, [parameters[4]]),
                            H2 = new PolynomialFunction(null, null, [parameters[5]]),
                        },
                    };
                    return mechanicalModelCalculator.CalculateStress(currentInput, xValues[0], xValues[1]);
                },
                LowerBounds = [1e-6, 0.0, 0.0, 0.0, 0.0, 0.0],
                UpperBounds = [1e5, 1e3, 10.0, 1.0, 1.0, 1.0],
                EvaluateConstraintsAndPenalties = null,
                InitialParameters = [1000.0, 0.1, 0.05, 1.0, 1.0, 1.0],
            };

            CurveFitOutput curveFitOutput = curveFitter.Fit(curveFitInput);

            SchaperyConstitutiveParameters finalParams = new SchaperyConstitutiveParameters
            {
                Ge = curveFitOutput.OptimizedParameters[0],
                TransientRelaxationFunction = new PowerLaw(curveSegment.ExperimentalStrain[0], curveSegment.ExperimentalStrain[^1], [curveFitOutput.OptimizedParameters[1], curveFitOutput.OptimizedParameters[2]]),
                He = new ConstantFunction(curveSegment.ExperimentalStrain[0], curveSegment.ExperimentalStrain[^1], curveFitOutput.OptimizedParameters[3]),
                H1 = new ConstantFunction(curveSegment.ExperimentalStrain[0], curveSegment.ExperimentalStrain[^1], curveFitOutput.OptimizedParameters[4]),
                H2 = new ConstantFunction(curveSegment.ExperimentalStrain[0], curveSegment.ExperimentalStrain[^1], curveFitOutput.OptimizedParameters[5]),
            };

            yield return new MechanicalModelCurveFitOutput(finalParams, curveFitOutput.FinalError, curveFitOutput.Iterations, new AcceptedRange(curveSegment.ExperimentalStrain[0], curveSegment.ExperimentalStrain[^1]));
        }
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }
}

