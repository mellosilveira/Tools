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
/// Simplified Fung model (relaxation-only curve fitting). Implements <c>IPipelineStep</c> for telemetry.
/// </summary>
public sealed class SimplifiedFungRelaxationOnlyCurveFitterStep(
    ISimplifiedFungModelCalculator mechanicalModelCalculator,
    ICurveFitter curveFitter) 
    : MechanicalModelCurveFitterStepBase<ISimplifiedFungModelCalculator, SimplifiedFungConstitutiveParameters>(mechanicalModelCalculator, curveFitter)
{
    /// <inheritdoc />
    public override string Name => nameof(SimplifiedFungRelaxationOnlyCurveFitterStep);

    /// <inheritdoc />
    protected override string MechanicalModelName => nameof(MechanicalModel.SimplifiedFung);

    /// <inheritdoc />
    protected override ViscoelasticEffect ViscoelasticEffect => ViscoelasticEffect.Relaxation;

    /// <inheritdoc />
    protected override RampTimeConsideration RampTimeConsideration => RampTimeConsideration.Disregard;

    /// <inheritdoc />
    protected override bool IsAcceptedSegment(SegmentType segmentType) => segmentType == SegmentType.Relaxation;

    /// <inheritdoc />
    protected override SimplifiedFungConstitutiveParameters CreateInitialParameters() => new()
    {
        ReducedRelaxationFunction = new PronySeries(null, null, 1, [1]),
        ElasticPowerConstant = 1,
        ElasticStressConstant = 1,
    };

    /// <inheritdoc />
    protected override double[] MapParametersToArray(SimplifiedFungConstitutiveParameters parameters)
    {
        PronySeries prony = parameters.ReducedRelaxationFunction!;
        int iteratorCount = prony.IteratorCoefficients.Length;
        var array = new double[3 + iteratorCount];
        array[0] = parameters.ElasticPowerConstant;
        array[1] = parameters.ElasticStressConstant;
        array[2] = prony.IndependentParameter;
        for (int i = 0; i < iteratorCount; i++)
        {
            array[3 + i] = prony.IteratorCoefficients[i];
        }
        return array;
    }

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
        int iteratorCount = parameters.ReducedRelaxationFunction?.IteratorCoefficients.Length ?? 1;
        var array = new double[3 + iteratorCount];
        array[0] = 1e-5;
        array[1] = 1e-2;
        array[2] = 0.0;
        for (int i = 0; i < iteratorCount; i++) array[3 + i] = 0.0;
        return array;
    }

    /// <inheritdoc />
    protected override double[] CreateUpperBounds() 
    {
        var parameters = CreateInitialParameters();
        int iteratorCount = parameters.ReducedRelaxationFunction?.IteratorCoefficients.Length ?? 1;
        var array = new double[3 + iteratorCount];
        array[0] = 100.0;
        array[1] = 1000.0;
        array[2] = 1.0;
        for (int i = 0; i < iteratorCount; i++) array[3 + i] = 1e6;
        return array;
    }
}
