using MelloSilveiraTools.Mathematics.Expressions;
using MelloSilveiraTools.Mathematics.Functions;
using MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels.Viscoelasticity.NonLinear.Schapery;
using MelloSilveiraTools.MechanicsOfMaterials.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.NonLinear.Schapery;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Algorithms.CurveFitting;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Models.CurveFitting;
using System.Runtime.CompilerServices;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Steps.CurveFitter;

/// <summary>
/// Thread-safe. Implements <see cref="IMechanicalModelCurveFitterStep"/> for the
/// Schapery model (relaxation-only curve fitting). Implements <c>IPipelineStep</c> for telemetry.
/// </summary>
public sealed class SchaperyRelaxationOnlyCurveFitterStep(
    ISchaperyModelCalculator mechanicalModelCalculator,
    ICurveFitter curveFitter)
    : MechanicalModelCurveFitterStepBase<ISchaperyModelCalculator, SchaperyConstitutiveParameters>(mechanicalModelCalculator, curveFitter)
{
    /// <inheritdoc />
    public override string Name => nameof(SchaperyRelaxationOnlyCurveFitterStep);

    /// <inheritdoc />
    protected override string MechanicalModelName => nameof(MechanicalModel.Schapery);

    /// <inheritdoc />
    protected override ViscoelasticEffect ViscoelasticEffect => ViscoelasticEffect.Relaxation;

    /// <inheritdoc />
    protected override RampTimeConsideration RampTimeConsideration => RampTimeConsideration.Disregard;

    /// <inheritdoc />
    protected override bool IsAcceptedSegment(SegmentType segmentType) => segmentType == SegmentType.Relaxation;

    /// <inheritdoc />
    protected override double[] CreateInitialParameters() => [1000.0, 0.1, 0.05, 1, 1, 1];

    /// <inheritdoc />
    protected override double[] CreateLowerBounds() => [1e-6, 0.0, 0.0, 0.0, 0.0, 0.0];

    /// <inheritdoc />
    protected override double[] CreateUpperBounds() => [1e5, 1e3, 10.0, 1.0, 1.0, 1.0];

    /// <inheritdoc />
    protected override Func<double[], double>? CreateEvaluateConstraintsAndPenalties()
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    protected override SchaperyConstitutiveParameters MapArrayToParameters(double[] array) => new()
    {
        Ge = array[0],
        TransientRelaxationFunction = new PowerLaw(null, null, [array[1], array[2]]),
        He = new PolynomialFunction(null, null, [array[3]]),
        H1 = new PolynomialFunction(null, null, [array[4]]),
        H2 = new PolynomialFunction(null, null, [array[5]]),
    };

    public override async IAsyncEnumerable<MechanicalModelCurveFitOutput> ExecuteAsync(CurveSegment[] input, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var curveSegments = input.Where(cs => cs.Type == SegmentType.Relaxation).OrderBy(cs => cs.ExperimentalStrain[0]).ToList();

        var curveSegment = curveSegments[0];
        double initialStrain = curveSegment.ExperimentalStrain[0];
        double finalStrain = curveSegment.ExperimentalStrain[^1];
        CurveFitInput curveFitInput = new()
        {
            TimePoints = curveSegment.TimePoints,
            StrainPoints = curveSegment.ExperimentalStrain,
            StressPoints = curveSegment.ExperimentalStress,
            CalculateStress = (parameters, time, strain) =>
            {
                MechanicalModelInput<SchaperyConstitutiveParameters> currentInput = new()
                {
                    MechanicalModelName = MechanicalModelName,
                    AcceptedStrainRange = new AcceptedRange { InitialPoint = initialStrain, FinalPoint = finalStrain },
                    MechanicalBehaviorType = MechanicalBehaviorType.StressStrain,
                    RampTimeConsideration = RampTimeConsideration,
                    ViscoelasticEffect = ViscoelasticEffect,
                    Strain = new MechanicalParameter(strain),
                    Stress = new MechanicalParameter(curveSegment.ExperimentalStress[0]),
                    TimeStep = curveSegment.TimePoints[1] - curveSegment.TimePoints[0],
                    ConstitutiveParameters = new SchaperyConstitutiveParameters
                    {
                        Ge = parameters[0],
                        He = new PolynomialFunction(initialStrain, finalStrain, [1]),
                        H1 = new PolynomialFunction(initialStrain, finalStrain, [1]),
                        H2 = new PolynomialFunction(initialStrain, finalStrain, [1]),
                        TransientRelaxationFunction = new PowerLaw(initialStrain, finalStrain, parameters[1..]),
                    }
                };
                return MechanicalModelCalculator.CalculateStress(currentInput, time, strain);
            },
            LowerBounds = [0, 0, 0],
            UpperBounds = [100, 100, 1],
            EvaluateConstraintsAndPenalties = CreateEvaluateConstraintsAndPenalties(),
            InitialParameters = [],
        };
        CurveFitOutput curveFitOutput = CurveFitter.Fit(curveFitInput);
        yield return MapToOutput(curveFitOutput);


    }
}
