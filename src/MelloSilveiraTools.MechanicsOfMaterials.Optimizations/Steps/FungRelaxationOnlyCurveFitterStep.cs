using MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels.Viscoelasticity.QuasiLinear.Fung;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Abstractions;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Models.CurveFitting;

namespace MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Steps;

/// <summary>
/// Thread-safe. Implements <see cref="IMechanicalModelCurveFitterStep"/> for the
/// Fung model (relaxation-only curve fitting). Implements <c>IPipelineStep</c> for telemetry.
/// </summary>
public sealed class FungRelaxationOnlyCurveFitterStep(IFungModelCalculator mechanicalModelCalculator) : IMechanicalModelCurveFitterStep
{
    /// <inheritdoc />
    public string Name => nameof(FungRelaxationOnlyCurveFitterStep);

    /// <inheritdoc />
    public ConstitutiveParameters[] Execute(CurveSegment[] input)
    {
        foreach (CurveSegment curve in input)
        {
            if (curve.Type != SegmentType.Relaxation)
                continue;

            var curveFitInput = new CurveFitInput
            {
                TimePoints = curve.TimePoints,
                StrainPoints = curve.ExperimentalStrain,
                StressPoints = curve.ExperimentalStress,
                CalculateStress = (mechanicalModelInput, time, strain) => mechanicalModelCalculator.CalculateStress(mechanicalModelInput, time, strain)
            };
        }


        // TODO: Implement specific numerical solver for Fung relaxation-only curve fitting.
        return [];
    }

    /// <inheritdoc />
    public void Dispose()
    {
        // No unmanaged resources to release.
    }
}
