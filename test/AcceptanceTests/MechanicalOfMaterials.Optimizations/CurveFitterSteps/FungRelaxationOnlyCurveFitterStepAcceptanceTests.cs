using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Steps.CurveFitter;

namespace AcceptanceTests.MechanicalOfMaterials.Optimizations.CurveFitterSteps;

public class FungRelaxationOnlyCurveFitterStepAcceptanceTests : CurveFitterAcceptanceTestBase<FungRelaxationOnlyCurveFitterStep>
{
    protected override string Prefix => "FungRelaxationOnly";

    protected override void AssertPhysicalConstraints(IReadOnlyList<CurveSegment> segments, IReadOnlyList<MechanicalModelCurveFitOutput> outputs)
    {
    }
}
