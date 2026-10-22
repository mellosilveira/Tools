using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.QuasiLinear;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Steps.CurveFitter;

namespace AcceptanceTests.MechanicalOfMaterials.Optimizations.CurveFitterSteps;

/// <summary>
/// Acceptance tests for <see cref="SimplifiedFungRelaxationOnlyCurveFitterStep"/> validating end-to-end curve fitting.
/// </summary>
public class SimplifiedFungRelaxationOnlyCurveFitterStepAcceptanceTests : CurveFitterAcceptanceTestBase<SimplifiedFungRelaxationOnlyCurveFitterStep>
{
    /// <inheritdoc/>
    protected override string Prefix => "SimplifiedFungRelaxationOnly";

    /// <inheritdoc/>
    protected override double MinimumAcceptableRSquared => 0.90;

    /// <inheritdoc/>
    protected override void AssertPhysicalConstraints(string groupName, IReadOnlyList<CurveSegment> segments, IReadOnlyList<MechanicalModelCurveFitOutput> outputs)
    {
        foreach (MechanicalModelCurveFitOutput output in outputs)
        {
            SimplifiedFungConstitutiveParameters parameters = Assert.IsType<SimplifiedFungConstitutiveParameters>(output.ConstitutiveParameters);
            Assert.NotNull(parameters.ReducedRelaxationFunction);
            Assert.True(parameters.ReducedRelaxationFunction.IndependentParameter > 0.0, $"Group '{groupName}': IndependentParameter c must be positive.");
            Assert.NotEmpty(parameters.ReducedRelaxationFunction.IteratorCoefficients);
        }
    }
}
