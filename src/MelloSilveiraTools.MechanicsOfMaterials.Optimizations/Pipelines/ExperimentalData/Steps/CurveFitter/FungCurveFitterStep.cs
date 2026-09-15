using MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels.Viscoelasticity.QuasiLinear.Fung;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.QuasiLinear;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Algorithms.CurveFitting;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Models.CurveFitting;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Steps.CurveFitter;

/// <summary>
/// Thread-safe. Implements <see cref="IMechanicalModelCurveFitterStep"/> for the
/// Fung model (full curve fitting). Implements <c>IPipelineStep</c> for telemetry.
/// </summary>
public class FungCurveFitterStep(
    IFungModelCalculator mechanicalModelCalculator,
    ICurveFitter curveFitter) 
    : MechanicalModelCurveFitterStepBase<IFungModelCalculator, FungConstitutiveParameters>(mechanicalModelCalculator, curveFitter)
{
    /// <inheritdoc />
    public override string Name => nameof(FungCurveFitterStep);

    /// <inheritdoc />
    protected override string MechanicalModelName => nameof(MechanicalModel.Fung);

    /// <inheritdoc />
    protected override ViscoelasticEffect ViscoelasticEffect => ViscoelasticEffect.Relaxation;

    /// <inheritdoc />
    protected override RampTimeConsideration RampTimeConsideration => RampTimeConsideration.ConsiderWithViscoelasticEffect;

    /// <inheritdoc />
    protected override bool IsAcceptedSegment(SegmentType segmentType) => true;

    /// <inheritdoc />
    protected override double[] CreateInitialParameters() => [1, 1, 1, 1, 1];

    /// <inheritdoc />
    protected override double[] CreateLowerBounds() => [0, 0, 0, 0, 0];

    /// <inheritdoc />
    protected override double[] CreateUpperBounds() => [1e6, 1e6, 1e6, 1e6, 1e6];

    /// <inheritdoc />
    protected override Func<double[], double>? CreateEvaluateConstraintsAndPenalties() => null;

    /// <inheritdoc />
    protected override FungConstitutiveParameters MapArrayToParameters(double[] array) => new()
    {
        ElasticPowerConstant = array[0],
        ElasticStressConstant = array[1],
        ReducedRelaxationFunction = new ReducedRelaxationFunction(array[2], array[3], array[4]),
    };
}
