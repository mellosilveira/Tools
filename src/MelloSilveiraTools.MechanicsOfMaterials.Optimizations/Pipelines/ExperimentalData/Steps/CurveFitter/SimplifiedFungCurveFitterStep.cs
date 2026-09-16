using MelloSilveiraTools.Mathematics.Expressions;
using MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels.Viscoelasticity.QuasiLinear.SimplifiedFung;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.QuasiLinear;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Algorithms.CurveFitting;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Models.CurveFitting;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Steps.CurveFitter;

/// <summary>
/// Thread-safe. Implements <see cref="IMechanicalModelCurveFitterStep"/> for the
/// Simplified Fung model (full curve fitting). Implements <c>IPipelineStep</c> for telemetry.
/// </summary>
public class SimplifiedFungCurveFitterStep(
    ISimplifiedFungModelCalculator mechanicalModelCalculator,
    ICurveFitter curveFitter) 
    : MechanicalModelCurveFitterStepBase<ISimplifiedFungModelCalculator, SimplifiedFungConstitutiveParameters>(mechanicalModelCalculator, curveFitter)
{
    /// <inheritdoc />
    public override string Name => nameof(SimplifiedFungCurveFitterStep);

    /// <inheritdoc />
    protected override string MechanicalModelName => nameof(MechanicalModel.SimplifiedFung);

    /// <inheritdoc />
    protected override ViscoelasticEffect ViscoelasticEffect => ViscoelasticEffect.Relaxation;

    /// <inheritdoc />
    protected override RampTimeConsideration RampTimeConsideration => RampTimeConsideration.ConsiderWithViscoelasticEffect;

    /// <inheritdoc />
    protected override bool IsAcceptedSegment(SegmentType segmentType) => true;

    /// <inheritdoc />
    protected override double[] CreateInitialParameters() => [1, 1, 1, 1];

    /// <inheritdoc />
    protected override Func<double[], double>? CreateEvaluateConstraintsAndPenalties() => null;

    /// <inheritdoc />
    protected override SimplifiedFungConstitutiveParameters MapArrayToParameters(double[] array)
    {
        var coeffs = new double[array.Length - 3];
        for (int i = 0; i < coeffs.Length; i++)
        {
            coeffs[i] = array[3 + i];
        }
        return new()
        {
            ElasticPowerConstant = array[0],
            ElasticStressConstant = array[1],
            ReducedRelaxationFunction = new PronySeries(null, null, array[2], coeffs),
        };
    }

    /// <inheritdoc />
    protected override double[] CreateLowerBounds() 
    {
        var parameters = CreateInitialParameters();
        var array = new double[parameters.Length];
        array[0] = 1e-5;
        array[1] = 1e-2;
        array[2] = 0.0;
        for (int i = 3; i < array.Length; i++) array[i] = 0.0;
        return array;
    }

    /// <inheritdoc />
    protected override double[] CreateUpperBounds() 
    {
        var parameters = CreateInitialParameters();
        var array = new double[parameters.Length];
        array[0] = 100.0;
        array[1] = 1000.0;
        array[2] = 1.0;
        for (int i = 3; i < array.Length; i++) array[i] = 1e6;
        return array;
    }
}
