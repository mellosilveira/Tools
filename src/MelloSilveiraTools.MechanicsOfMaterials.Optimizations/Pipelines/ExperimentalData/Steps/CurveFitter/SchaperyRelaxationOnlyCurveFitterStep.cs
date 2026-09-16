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

    /// <inheritdoc />
    public override async IAsyncEnumerable<MechanicalModelCurveFitOutput> ExecuteAsync(CurveSegment[] input, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var relaxations = input.Where(cs => cs.Type == SegmentType.Relaxation).OrderBy(cs => cs.ExperimentalStrain[0]).ToList();
        if (relaxations.Count == 0)
            yield break;

        // 1. Select the relaxation with the lowest strain (Anchor)
        var anchorSegment = relaxations[0];
        
        double[] initialParams = CreateInitialParameters();
        double[] baseLowerBounds = CreateLowerBounds();
        double[] baseUpperBounds = CreateUpperBounds();

        // 2. Fit Anchor (Optimize Ge, C, n; Lock he=1, h1=1, h2=1)
        double[] anchorLowerBounds = (double[])baseLowerBounds.Clone();
        double[] anchorUpperBounds = (double[])baseUpperBounds.Clone();
        anchorLowerBounds[3] = 1.0; anchorUpperBounds[3] = 1.0; // he
        anchorLowerBounds[4] = 1.0; anchorUpperBounds[4] = 1.0; // h1
        anchorLowerBounds[5] = 1.0; anchorUpperBounds[5] = 1.0; // h2

        CurveFitInput anchorInput = new()
        {
            TimePoints = anchorSegment.TimePoints,
            StrainPoints = anchorSegment.ExperimentalStrain,
            StressPoints = anchorSegment.ExperimentalStress,
            CalculateStress = (parameters, time, strain) =>
            {
                MechanicalModelInput<SchaperyConstitutiveParameters> currentInput = new()
                {
                    MechanicalModelName = MechanicalModelName,
                    AcceptedStrainRange = new AcceptedRange { InitialPoint = anchorSegment.ExperimentalStrain[0], FinalPoint = anchorSegment.ExperimentalStrain[^1] },
                    MechanicalBehaviorType = MechanicalBehaviorType.StressStrain,
                    RampTimeConsideration = RampTimeConsideration,
                    ViscoelasticEffect = ViscoelasticEffect,
                    Strain = new MechanicalParameter(strain),
                    Stress = new MechanicalParameter(anchorSegment.ExperimentalStress[0]),
                    TimeStep = anchorSegment.TimePoints[1] - anchorSegment.TimePoints[0],
                    ConstitutiveParameters = MapArrayToParameters(parameters),
                };
                return MechanicalModelCalculator.CalculateStress(currentInput, time, strain);
            },
            LowerBounds = anchorLowerBounds,
            UpperBounds = anchorUpperBounds,
            EvaluateConstraintsAndPenalties = CreateEvaluateConstraintsAndPenalties(),
            InitialParameters = initialParams,
        };

        CurveFitOutput anchorOutput = CurveFitter.Fit(anchorInput);
        yield return MapToOutput(anchorOutput);

        double[] optimizedLinearParams = anchorOutput.OptimizedParameters;

        // 3. Fit remaining relaxations (Lock Ge, C, n; Optimize he, h1, h2)
        for (int i = 1; i < relaxations.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var segment = relaxations[i];

            double[] segmentLowerBounds = (double[])baseLowerBounds.Clone();
            double[] segmentUpperBounds = (double[])baseUpperBounds.Clone();
            segmentLowerBounds[0] = optimizedLinearParams[0]; segmentUpperBounds[0] = optimizedLinearParams[0]; // Ge
            segmentLowerBounds[1] = optimizedLinearParams[1]; segmentUpperBounds[1] = optimizedLinearParams[1]; // C
            segmentLowerBounds[2] = optimizedLinearParams[2]; segmentUpperBounds[2] = optimizedLinearParams[2]; // n

            double[] segmentInitialParams = (double[])initialParams.Clone();
            segmentInitialParams[0] = optimizedLinearParams[0];
            segmentInitialParams[1] = optimizedLinearParams[1];
            segmentInitialParams[2] = optimizedLinearParams[2];

            CurveFitInput segmentInput = new()
            {
                TimePoints = segment.TimePoints,
                StrainPoints = segment.ExperimentalStrain,
                StressPoints = segment.ExperimentalStress,
                CalculateStress = (parameters, time, strain) =>
                {
                    MechanicalModelInput<SchaperyConstitutiveParameters> currentInput = new()
                    {
                        MechanicalModelName = MechanicalModelName,
                        AcceptedStrainRange = new AcceptedRange { InitialPoint = segment.ExperimentalStrain[0], FinalPoint = segment.ExperimentalStrain[^1] },
                        MechanicalBehaviorType = MechanicalBehaviorType.StressStrain,
                        RampTimeConsideration = RampTimeConsideration,
                        ViscoelasticEffect = ViscoelasticEffect,
                        Strain = new MechanicalParameter(strain),
                        Stress = new MechanicalParameter(segment.ExperimentalStress[0]),
                        TimeStep = segment.TimePoints[1] - segment.TimePoints[0],
                        ConstitutiveParameters = MapArrayToParameters(parameters),
                    };
                    return MechanicalModelCalculator.CalculateStress(currentInput, time, strain);
                },
                LowerBounds = segmentLowerBounds,
                UpperBounds = segmentUpperBounds,
                EvaluateConstraintsAndPenalties = CreateEvaluateConstraintsAndPenalties(),
                InitialParameters = segmentInitialParams,
            };

            CurveFitOutput segmentOutput = CurveFitter.Fit(segmentInput);
            yield return MapToOutput(segmentOutput);
        }
    }
}
