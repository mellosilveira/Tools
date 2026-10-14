using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.QuasiLinear;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Steps.CurveFitter;

namespace AcceptanceTests.MechanicalOfMaterials.Optimizations.CurveFitterSteps;

/// <summary>
/// Acceptance tests for <see cref="FungRelaxationOnlyCurveFitterStep"/> validating end-to-end curve fitting.
/// </summary>
public class FungRelaxationOnlyCurveFitterStepAcceptanceTests : CurveFitterAcceptanceTestBase<FungRelaxationOnlyCurveFitterStep>
{
    /// <inheritdoc/>
    protected override string Prefix => "FungRelaxationOnly";

    /// <inheritdoc/>
    protected override double MinimumAcceptableRSquared => 0.90;

    /// <inheritdoc/>
    protected override void AssertPhysicalConstraints(string groupName, IReadOnlyList<CurveSegment> segments, IReadOnlyList<MechanicalModelCurveFitOutput> outputs)
    {
        foreach (MechanicalModelCurveFitOutput output in outputs)
        {
            FungConstitutiveParameters parameters = Assert.IsType<FungConstitutiveParameters>(output.ConstitutiveParameters);
            Assert.NotNull(parameters.ReducedRelaxationFunction);
            Assert.True(parameters.ReducedRelaxationFunction.RelaxationStiffness > 0.0, $"Group '{groupName}': RelaxationStiffness must be positive.");
            Assert.True(parameters.ReducedRelaxationFunction.FastRelaxationTime > 0.0, $"Group '{groupName}': FastRelaxationTime must be positive.");
            Assert.True(parameters.ReducedRelaxationFunction.SlowRelaxationTime > parameters.ReducedRelaxationFunction.FastRelaxationTime, $"Group '{groupName}': SlowRelaxationTime must exceed FastRelaxationTime.");
        }
    }
}
