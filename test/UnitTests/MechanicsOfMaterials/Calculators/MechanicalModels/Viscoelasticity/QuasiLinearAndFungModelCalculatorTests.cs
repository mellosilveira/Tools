using MelloSilveiraTools.Mathematics.Expressions;
using MelloSilveiraTools.Mathematics.Functions;
using MelloSilveiraTools.Mathematics.Models;
using MelloSilveiraTools.Mathematics.NumericalMethods.Differentiations;
using MelloSilveiraTools.Mathematics.NumericalMethods.Integrals;
using MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels.Viscoelasticity.QuasiLinear.Fung;
using MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels.Viscoelasticity.QuasiLinear.SimplifiedFung;
using MelloSilveiraTools.MechanicsOfMaterials.Converters.MechanicalParameter;
using MelloSilveiraTools.MechanicsOfMaterials.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.QuasiLinear;

namespace UnitTests.MechanicsOfMaterials.Calculators.MechanicalModels.Viscoelasticity;

/// <summary>
/// Unit tests for <see cref="FungModelCalculator"/>, <see cref="SimplifiedFungModelCalculator"/>,
/// and base <c>QuasiLinearModelCalculator</c>.
/// </summary>
public sealed class QuasiLinearAndFungModelCalculatorTests
{
    private static FungModelCalculator CreateFungCalculator()
        => new(new SimpsonRuleIntegration(), new Differentiation(), new MechanicalParameterConverter());

    private static SimplifiedFungModelCalculator CreateSimplifiedFungCalculator()
        => new(new SimpsonRuleIntegration(), new Differentiation(), new MechanicalParameterConverter());

    private static MechanicalModelInput<FungConstitutiveParameters> CreateFungInput(
        ViscoelasticEffect effect = ViscoelasticEffect.Relaxation,
        RampTimeConsideration ramp = RampTimeConsideration.Disregard,
        double? rampTime = null) => new()
        {
            MechanicalModelName = nameof(MechanicalModel.Fung),
            ViscoelasticEffect = effect,
            RampTimeConsideration = ramp,
            RampTime = rampTime,
            ConstitutiveParameters = new FungConstitutiveParameters(2.0, 5.0, new ReducedRelaxationFunction(0.4, 0.1, 10.0)),
            Specimen = new SpecimenParameter(false, false, 0.0, 0.1, 0.0001, Vector3D.Empty),
            Displacement = new MechanicalParameter(0.005, new PolynomialFunction([0.0, 0.005])),
            Force = new MechanicalParameter(50.0, new PolynomialFunction([0.0, 5.0])),
            Strain = new MechanicalParameter(0.05, new PolynomialFunction([0.0, 0.05])),
            Stress = new MechanicalParameter(1.0, new PolynomialFunction([0.0, 0.2])),
            TimeStep = 0.05
        };

    private static MechanicalModelInput<SimplifiedFungConstitutiveParameters> CreateSimplifiedFungInput(
        RampTimeConsideration ramp = RampTimeConsideration.ConsiderWithViscoelasticEffect) => new()
        {
            MechanicalModelName = nameof(MechanicalModel.SimplifiedFung),
            ViscoelasticEffect = ViscoelasticEffect.Relaxation,
            RampTimeConsideration = ramp,
            ConstitutiveParameters = new SimplifiedFungConstitutiveParameters(2.0, 5.0, new PronySeries(0.5, [0.5, -1.0])),
            Specimen = new SpecimenParameter(false, false, 0.0, 0.1, 0.0001, Vector3D.Empty),
            Displacement = new MechanicalParameter(0.0, new PolynomialFunction([0.0, 0.005])),
            Force = new MechanicalParameter(50.0, new PolynomialFunction([0.0, 5.0])),
            Strain = new MechanicalParameter(0.0, new PolynomialFunction([0.0, 0.05])),
            Stress = new MechanicalParameter(1.0, new PolynomialFunction([0.0, 0.2])),
            TimeStep = 0.05
        };

    [Fact]
    public void FungModelCalculator_DisregardConsiderAndUnimplementedBranches_WorkAsExpected()
    {
        FungModelCalculator calc = CreateFungCalculator();
        MechanicalModelInput<FungConstitutiveParameters> disregardRelax = CreateFungInput(ViscoelasticEffect.Relaxation, RampTimeConsideration.Disregard);

        Assert.Throws<NotImplementedException>(() => calc.CalculateCreepCompliance(disregardRelax, 1.0));
        Assert.Equal(1.0, calc.CalculateReducedRelaxationFunction(disregardRelax, 0.0), 10);
        double gt = calc.CalculateReducedRelaxationFunction(disregardRelax, 1.0);
        Assert.True(gt > 0 && gt < 1.0);
        Assert.True(calc.CalculateRelaxationFunction(disregardRelax, 1.0) > 0);

        Assert.True(calc.CalculateForce(disregardRelax, 1.0) > 0);
        Assert.True(calc.CalculateForce(disregardRelax, 1.0, 0.005) > 0);
        MechanicalParameterConverter converter = new();
        double stressFromForce = converter.CalculateStressFromForce(disregardRelax.Specimen!, disregardRelax.Force!.InitialValue);
        double calculatedStrain = calc.CalculateStrain(disregardRelax, 1.0, stressFromForce);
        double expectedDisplacement = converter.CalculateDisplacementFromStrain(disregardRelax.Specimen!, calculatedStrain);
        Assert.Equal(expectedDisplacement, calc.CalculateDisplacement(disregardRelax, 1.0), 10);
        double strainAtUnitStress = calc.CalculateStrain(disregardRelax, 1.0);
        Assert.Equal(converter.CalculateDisplacementFromStrain(disregardRelax.Specimen!, strainAtUnitStress), calc.CalculateDisplacement(disregardRelax, 1.0, converter.CalculateForceFromStress(disregardRelax.Specimen!, 1.0)), 10);
        Assert.True(calc.CalculateStress(disregardRelax, 1.0) > 0);
        Assert.True(calc.CalculateStrain(disregardRelax, 1.0) > 0);
        Assert.Equal(50.0, calc.CalculateElasticForceResponse(disregardRelax, 1.0));
        Assert.True(calc.CalculateElasticForceResponse(disregardRelax, 1.0, 0.005) > 0);
        Assert.Equal(1.0, calc.CalculateElasticResponse(disregardRelax, 1.0));
        Assert.True(calc.CalculateStressByReducedRelaxationFunctionDerivative(disregardRelax, 1.0) > 0);
        Assert.True(calc.CalculateStressByConvolutionDerivative(disregardRelax, 1.0) > 0);

        // Disregard + Creep exceptions
        MechanicalModelInput<FungConstitutiveParameters> disregardCreep = CreateFungInput(ViscoelasticEffect.Creep, RampTimeConsideration.Disregard);
        Assert.Throws<NotImplementedException>(() => calc.CalculateForce(disregardCreep, 1.0));
        Assert.Throws<NotImplementedException>(() => calc.CalculateDisplacement(disregardCreep, 1.0));
        Assert.Throws<NotImplementedException>(() => calc.CalculateStress(disregardCreep, 1.0));
        Assert.Throws<NotImplementedException>(() => calc.CalculateStrain(disregardCreep, 1.0));
        Assert.Throws<NotImplementedException>(() => calc.CalculateStressByReducedRelaxationFunctionDerivative(disregardCreep, 1.0));
        Assert.Throws<NotImplementedException>(() => calc.CalculateStressByConvolutionDerivative(disregardCreep, 1.0));

        // ConsiderWithoutViscoelasticEffect (before and after rampTime)
        MechanicalModelInput<FungConstitutiveParameters> withoutVe = CreateFungInput(ViscoelasticEffect.Relaxation, RampTimeConsideration.ConsiderWithoutViscoelasticEffect, rampTime: 0.3);
        Assert.Equal(0.0, calc.CalculateStress(withoutVe, 0.0));
        Assert.True(calc.CalculateStress(withoutVe, 0.2) > 0);
        Assert.True(calc.CalculateStress(withoutVe, 0.6) > 0);

        // ConsiderWithViscoelasticEffect
        MechanicalModelInput<FungConstitutiveParameters> withVe = CreateFungInput(ViscoelasticEffect.Relaxation, RampTimeConsideration.ConsiderWithViscoelasticEffect);
        Assert.True(calc.CalculateForce(withVe, 0.3) > 0);
        Assert.Throws<NotImplementedException>(() => calc.CalculateDisplacement(withVe, 0.3));
        Assert.True(calc.CalculateStress(withVe, 0.3) > 0);
        Assert.Throws<NotImplementedException>(() => calc.CalculateStrain(withVe, 0.3));
        Assert.True(calc.CalculateStressByReducedRelaxationFunctionDerivative(withVe, 0.3) > 0);
        Assert.True(calc.CalculateStressByConvolutionDerivative(withVe, 0.3) > 0);
        Assert.True(calc.CalculateElasticForceResponse(withVe, 0.3) > 0);
        Assert.True(calc.CalculateElasticResponse(withVe, 0.3) > 0);

        // Explicit parameter overrides under Disregard + Relaxation & unknown ViscoelasticEffect false-branches
        Assert.True(calc.CalculateElasticForceResponse(disregardRelax, 1.0, 0.005) > 0);
        Assert.True(calc.CalculateElasticResponse(disregardRelax, 1.0, 0.05) > 0);
        Assert.True(calc.CalculateForce(disregardRelax, 1.0, 0.005) > 0);
        Assert.True(calc.CalculateDisplacement(disregardRelax, 1.0, 50.0) > 0);
        Assert.True(calc.CalculateStress(disregardRelax, 1.0, 0.05) > 0);
        Assert.True(calc.CalculateStrain(disregardRelax, 1.0, 1.0) > 0);

        MechanicalModelInput<FungConstitutiveParameters> invalidEffect = CreateFungInput((ViscoelasticEffect)999, RampTimeConsideration.Disregard);
        Assert.Equal(0.0, calc.CalculateForce(invalidEffect, 0.3));
        Assert.Throws<NotImplementedException>(() => calc.CalculateDisplacement(invalidEffect, 0.3));
        Assert.Equal(0.0, calc.CalculateStress(invalidEffect, 0.3));
        Assert.Throws<NotImplementedException>(() => calc.CalculateStrain(invalidEffect, 0.3));
        Assert.True(calc.CalculateStressByReducedRelaxationFunctionDerivative(invalidEffect, 0.3) > 0);
        Assert.True(calc.CalculateStressByConvolutionDerivative(invalidEffect, 0.3) > 0);
    }

    [Fact]
    public void SimplifiedFungModelCalculator_EvaluatesPronyReducedRelaxationAtZeroAndPositiveTime()
    {
        SimplifiedFungModelCalculator calc = CreateSimplifiedFungCalculator();
        MechanicalModelInput<SimplifiedFungConstitutiveParameters> input = CreateSimplifiedFungInput();

        Assert.Equal(1.0, calc.CalculateReducedRelaxationFunction(input, 0.0), 10);
        Assert.True(calc.CalculateReducedRelaxationFunction(input, 1.0) < 1.0);
        Assert.True(calc.CalculateStressByReducedRelaxationFunctionDerivative(input, 0.2) > 0);
    }
}
