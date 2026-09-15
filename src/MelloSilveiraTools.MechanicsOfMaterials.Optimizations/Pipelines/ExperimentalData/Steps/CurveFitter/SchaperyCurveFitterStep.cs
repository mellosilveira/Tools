using MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels.Viscoelasticity.NonLinear.Schapery;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.NonLinear.Schapery;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Algorithms.CurveFitting;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Models.CurveFitting;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Steps.CurveFitter;

/// <summary>
/// Thread-safe. Implements <see cref="IMechanicalModelCurveFitterStep"/> for the
/// Schapery model (full curve fitting). Implements <c>IPipelineStep</c> for telemetry.
/// </summary>
public sealed class SchaperyCurveFitterStep(
    ISchaperyModelCalculator mechanicalModelCalculator,
    ICurveFitter curveFitter) 
    : MechanicalModelCurveFitterStepBase<ISchaperyModelCalculator, SchaperyConstitutiveParameters>(mechanicalModelCalculator, curveFitter)
{
    /// <inheritdoc />
    public override string Name => nameof(SchaperyCurveFitterStep);

    /// <inheritdoc />
    protected override string MechanicalModelName => nameof(MechanicalModel.Schapery);

    /// <inheritdoc />
    protected override ViscoelasticEffect ViscoelasticEffect => ViscoelasticEffect.Relaxation;

    /// <inheritdoc />
    protected override RampTimeConsideration RampTimeConsideration => RampTimeConsideration.ConsiderWithViscoelasticEffect;

    /// <inheritdoc />
    protected override bool IsAcceptedSegment(SegmentType segmentType) => true;

    /// <inheritdoc />
    protected override SchaperyConstitutiveParameters CreateInitialParameters() => new()
    {
        Ge = 10.0,
        TransientRelaxationFunction = new MelloSilveiraTools.Mathematics.Functions.PowerLaw(null, null, [0.1, 0.05]),
        He = new MelloSilveiraTools.Mathematics.Functions.PolynomialFunction(null, null, [1.0]),
        H1 = new MelloSilveiraTools.Mathematics.Functions.PolynomialFunction(null, null, [1.0]),
        H2 = new MelloSilveiraTools.Mathematics.Functions.PolynomialFunction(null, null, [1.0]),
    };

    /// <inheritdoc />
    protected override double[] MapParametersToArray(SchaperyConstitutiveParameters parameters) =>
    [
        parameters.Ge,
        parameters.TransientRelaxationFunction?.Coefficients[0] ?? 0.1,
        parameters.TransientRelaxationFunction?.Coefficients[1] ?? 0.05,
        parameters.He?.Coefficients[0] ?? 1.0,
        parameters.H1?.Coefficients[0] ?? 1.0,
        parameters.H2?.Coefficients[0] ?? 1.0,
    ];

    /// <inheritdoc />
    protected override SchaperyConstitutiveParameters MapArrayToParameters(double[] array) => new()
    {
        Ge = array[0],
        TransientRelaxationFunction = new MelloSilveiraTools.Mathematics.Functions.PowerLaw(null, null, [array[1], array[2]]),
        He = new MelloSilveiraTools.Mathematics.Functions.PolynomialFunction(null, null, [array[3]]),
        H1 = new MelloSilveiraTools.Mathematics.Functions.PolynomialFunction(null, null, [array[4]]),
        H2 = new MelloSilveiraTools.Mathematics.Functions.PolynomialFunction(null, null, [array[5]]),
    };

    /// <inheritdoc />
    protected override double[] CreateLowerBounds() => [1e-2, 0.0, 0.0, 0.0, 0.0, 0.0];

    /// <inheritdoc />
    protected override double[] CreateUpperBounds() => [1e5, 1e3, 10.0, 1.0, 1.0, 1.0];
}
