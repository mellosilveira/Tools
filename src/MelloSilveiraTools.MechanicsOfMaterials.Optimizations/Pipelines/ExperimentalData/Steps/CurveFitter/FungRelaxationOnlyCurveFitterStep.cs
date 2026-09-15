using MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels.Viscoelasticity.QuasiLinear.Fung;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Algorithms.CurveFitting;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Models.CurveFitting;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Steps.CurveFitter;

/// <summary>
/// Thread-safe. Implements <see cref="IMechanicalModelCurveFitterStep"/> for the
/// Fung model (relaxation-only curve fitting). Implements <c>IPipelineStep</c> for telemetry.
/// </summary>
public sealed class FungRelaxationOnlyCurveFitterStep(
    IFungModelCalculator mechanicalModelCalculator,
    ICurveFitter curveFitter) 
    : FungCurveFitterStep(mechanicalModelCalculator, curveFitter)
{
    /// <inheritdoc />
    public override string Name => nameof(FungRelaxationOnlyCurveFitterStep);

    /// <inheritdoc />
    protected override RampTimeConsideration RampTimeConsideration => RampTimeConsideration.Disregard;

    /// <inheritdoc />
    protected override bool IsAcceptedSegment(SegmentType segmentType) => segmentType == SegmentType.Relaxation;
}
