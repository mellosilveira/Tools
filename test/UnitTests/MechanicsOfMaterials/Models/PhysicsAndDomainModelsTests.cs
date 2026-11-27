using MelloSilveiraTools.Mathematics.Expressions;
using MelloSilveiraTools.Mathematics.Functions;
using MelloSilveiraTools.Mathematics.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Elasticity;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.QuasiLinear;
using MelloSilveiraTools.MechanicsOfMaterials.Models.Physics;

namespace UnitTests.MechanicsOfMaterials.Models;

/// <summary>
/// Unit tests for <see cref="Force"/>, <see cref="SpecimenParameter"/>, <see cref="MechanicalParameter"/>,
/// <see cref="AnalysisParameter"/>, <see cref="Asymptote"/>, <see cref="AcceptedRange"/>, and <see cref="MechanicalModelInput"/>.
/// </summary>
public sealed class PhysicsAndDomainModelsTests
{
    [Fact]
    public void Force_FactoriesAndVectorOperations_MaintainMagnitudeConsistency()
    {
        Force f1 = Force.Create(10.0, Vector3D.Create(0.6, 0.8, 0.0));
        Force f2 = Force.Create("-3.123,-4.456,0");

        Assert.Equal(6.0, f1.X, 10);
        Assert.Equal(8.0, f1.Y, 10);
        Assert.Equal(0.0, f1.Z, 10);
        Assert.Equal(10.0, f1.AbsoluteValue, 10);

        Force sum = f1.Sum(Force.Create(0.0, Vector3D.Empty));
        Force diff = f1.Subtract(Force.Create(5.0, Vector3D.Create(0.6, 0.8, 0.0)));
        Force divided = f1.Divide(2);
        Force rounded = f2.Round(1);
        Force abs = f2.Abs();

        Assert.Equal(10.0, sum.AbsoluteValue, 10);
        Assert.Equal(5.0, diff.AbsoluteValue, 10);
        Assert.Equal(5.0, divided.AbsoluteValue, 10);
        Assert.Equal(-3.1, rounded.X, 10);
        Assert.Equal(Math.Abs(f2.X), abs.X, 10);
        Assert.Equal(Math.Abs(f2.Y), abs.Y, 10);
    }

    [Fact]
    public void SpecimenAndMechanicalAndAnalysisParameters_ComputeDerivedProperties()
    {
        SpecimenParameter defaultSpecimen = new();
        Assert.Equal(0.0, defaultSpecimen.PreLoadLength);

        SpecimenParameter specimen = new(true, true, 0.1, 0.11, 0.002, Vector3D.Create(0.0, 0.0, 0.0));
        Assert.Equal(1.1, specimen.PreLoadFactor, 10);
        Assert.Equal(0.1, specimen.InitialLength, 10);
        Assert.Equal(0.01, specimen.PreLoadDisplacement, 10);

        MechanicalParameter emptyParam = new();
        Assert.Equal(0.0, emptyParam.CalculateValue(5.0));
        Assert.Equal(0.0, emptyParam.CalculateDerivative(5.0));

        MechanicalParameter scalarParam = new(2.5);
        scalarParam.SetInitialValue(3.0);
        Assert.Equal(3.0, scalarParam.InitialValue);

        MathExpression linearExpr = new PolynomialFunction([1.0, 2.0]);
        MechanicalParameter exprParam = new(10.0, linearExpr);
        (double val, double deriv) = exprParam.CalculateValueAndDerivative(3.0);
        Assert.Equal(17.0, val, 10);
        Assert.Equal(2.0, deriv, 10);

        AnalysisParameter analysisParam = new() { InitialDependentMechanicalParameterValue = 5.0 };
        Assert.Equal(5.0, analysisParam.InitialDependentMechanicalParameterValue);
        Assert.NotNull(analysisParam.IndependentMechanicalParameter);

        Asymptote asymptote = new(1.0, 0.01, 0.002, 5.0, 100.0);
        Assert.Equal(1.0, asymptote.Time);
        Assert.Equal(100.0, asymptote.Force);
    }

    [Fact]
    public void AcceptedRangeAndMechanicalModelInputHierarchy_PreserveAllProperties()
    {
        AcceptedRange defaultRange = new();
        AcceptedRange customRange = new(0.0, 1.0);
        Assert.Equal(0.0, defaultRange.InitialPoint);
        Assert.Equal(1.0, customRange.FinalPoint);

        MechanicalModelInput baseInput = new()
        {
            Identifier = "run-1",
            MechanicalModelName = nameof(MechanicalModel.Elastic),
            AcceptedStrainRange = customRange,
            LoadResponseRelationship = LoadResponseRelationship.StressStrain,
            ViscoelasticEffect = ViscoelasticEffect.Relaxation,
            RampTimeConsideration = RampTimeConsideration.ConsiderWithViscoelasticEffect,
            RampTime = 1.5,
            Strain = new MechanicalParameter(0.1),
            Displacement = new MechanicalParameter(0.01),
            Stress = new MechanicalParameter(10.0),
            Force = new MechanicalParameter(100.0),
            Specimen = new SpecimenParameter(),
            TimeStep = 0.01
        };

        ElasticConstitutiveParameters elasticParams = new() { YoungModulus = 200.0 };
        MechanicalModelInput<ElasticConstitutiveParameters> typedCopy = new(baseInput, elasticParams);
        GenericMechanicalModelInput genericCopy = new(baseInput, elasticParams);

        Assert.Equal("run-1", typedCopy.Identifier);
        Assert.Equal(1.5, typedCopy.RampTime);
        Assert.Equal(0.01, typedCopy.TimeStep);
        Assert.Same(elasticParams, genericCopy.ConstitutiveParameters);

        FungConstitutiveParameters fungParams = new(1.0, 2.0, new ReducedRelaxationFunction(0.5, 0.1, 10.0));
        Assert.Equal(1.0, fungParams.ElasticStressConstant);
        Assert.Equal(2.0, fungParams.ElasticPowerConstant);

        SimplifiedFungConstitutiveParameters simpFungParams = new(1.0, 2.0, new PronySeries(0.5, [0.5, -1.0]));
        Assert.Equal(1.0, simpFungParams.ElasticStressConstant);
    }
}
