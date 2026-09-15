using MelloSilveiraTools.Mathematics.Functions;
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

    protected override MechanicalModelCurveFitOutput FitCurve(CurveSegment curveSegment)
    {
        return base.FitCurve(curveSegment);
    }
}
