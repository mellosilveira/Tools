using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Steps.CurveFitter;

namespace AcceptanceTests.MechanicalOfMaterials.Optimizations.CurveFitterSteps;

public class SimplifiedFungRelaxationOnlyCurveFitterStepAcceptanceTests : CurveFitterAcceptanceTestBase<SimplifiedFungRelaxationOnlyCurveFitterStep>
{
    protected override string Prefix => "SimplifiedFungRelaxationOnly";

    protected override void AssertPhysicalConstraints(IReadOnlyList<CurveSegment> segments, IReadOnlyList<MechanicalModelCurveFitOutput> outputs)
    {
    }
}
