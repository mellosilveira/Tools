using MelloSilveiraTools.Mathematics.Expressions;
using MelloSilveiraTools.Mathematics.Functions;
using MelloSilveiraTools.Mathematics.Models;
using MelloSilveiraTools.Mathematics.NumericalMethods.Differentiations;
using MelloSilveiraTools.Mathematics.NumericalMethods.Integrals;
using MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels.Viscoelasticity.NonLinear.ModifiedSuperpositionMethod;
using MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels.Viscoelasticity.NonLinear.Schapery;
using MelloSilveiraTools.MechanicsOfMaterials.Converters.MechanicalParameter;
using MelloSilveiraTools.MechanicsOfMaterials.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.NonLinear.ModifiedSuperpositionMethod;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.NonLinear.Schapery;

namespace UnitTests.MechanicsOfMaterials.Calculators.MechanicalModels.Viscoelasticity;

/// <summary>
/// Unit tests for <see cref="SchaperyModelCalculator"/> and <see cref="ModifiedSuperpositionMethodCalculator"/>.
/// </summary>
public sealed class NonLinearViscoelasticCalculatorsTests
{
    private static SchaperyModelCalculator CreateSchaperyCalculator()
        => new(new SimpsonRuleIntegration(), new Differentiation(), new MechanicalParameterConverter());

    private static ModifiedSuperpositionMethodCalculator CreateMsmCalculator()
        => new(new SimpsonRuleIntegration(), new MechanicalParameterConverter());

    private static MechanicalModelInput<SchaperyConstitutiveParameters> CreateSchaperyInput(
        ViscoelasticEffect effect = ViscoelasticEffect.Relaxation,
        RampTimeConsideration ramp = RampTimeConsideration.Disregard) => new()
        {
            MechanicalModelName = nameof(MechanicalModel.Schapery),
            ViscoelasticEffect = effect,
            RampTimeConsideration = ramp,
            ConstitutiveParameters = new SchaperyConstitutiveParameters
            {
                G0 = 1.0,
                G1 = 1.0,
                G2 = 1.0,
                J0 = 0.01,
                Ge = 10.0,
                He = new ConstantFunction(1.0),
                H1 = new ConstantFunction(1.0),
                H2 = new ConstantFunction(1.0),
                TransientRelaxationFunction = new PowerLaw([5.0, -0.2]),
                TransientCreepCompliance = new PronySeries(0.02, [-0.01, -0.5])
            },
            Specimen = new SpecimenParameter(false, false, 0.0, 0.1, 0.0001, Vector3D.Empty),
            Displacement = new MechanicalParameter(0.01, new PolynomialFunction([0.0, 0.005])),
            Strain = new MechanicalParameter(0.1, new PolynomialFunction([0.0, 0.05])),
            TimeStep = 0.0625
        };

    private static MechanicalModelInput<ModifiedSuperpositionMethodConstitutiveParameters> CreateMsmInput(
        ViscoelasticEffect effect = ViscoelasticEffect.Relaxation,
        RampTimeConsideration ramp = RampTimeConsideration.Disregard) => new()
        {
            MechanicalModelName = nameof(MechanicalModel.ModifiedSuperpositionMethod),
            ViscoelasticEffect = effect,
            RampTimeConsideration = ramp,
            ConstitutiveParameters = new ModifiedSuperpositionMethodConstitutiveParameters
            {
                InitialYoungModulus = new PolynomialFunction([50.0, 10.0]),
                StressRelaxationRate = new PolynomialFunction([0.2, 0.05])
            },
            Specimen = new SpecimenParameter(false, false, 0.0, 0.1, 0.0001, Vector3D.Empty),
            Displacement = new MechanicalParameter(0.01, new PolynomialFunction([0.0, 0.005])),
            Strain = new MechanicalParameter(0.1, new PolynomialFunction([0.0, 0.05])),
            TimeStep = 0.0625
        };

    [Fact]
    public void SchaperyModelCalculator_CoversRelaxationCreepRampAndUnimplementedPaths()
    {
        SchaperyModelCalculator calc = CreateSchaperyCalculator();
        MechanicalModelInput<SchaperyConstitutiveParameters> disregardRelax = CreateSchaperyInput(ViscoelasticEffect.Relaxation, RampTimeConsideration.Disregard);

        Assert.Equal(5.0, calc.CalculateTransientRelaxationFunction(disregardRelax, 1.0), 8);
        Assert.Equal(0.02 - 0.01 * Math.Exp(-0.5), calc.CalculateTransientCreepCompliance(disregardRelax, 1.0), 8);
        Assert.Equal(1.5, calc.CalculateReducedTimeFunction(disregardRelax, 1.5));
        Assert.Equal(1.5, calc.CalculateRetardationTimeFunction(disregardRelax, 1.5));
        Assert.Equal(15.0, calc.CalculateRelaxationFunction(disregardRelax, 1.0), 8);
        Assert.True(calc.CalculateCreepCompliance(disregardRelax, 1.0) > 0);

        Assert.Throws<NotImplementedException>(() => calc.CalculateStressShiftFactor(disregardRelax, 1.0));
        Assert.Throws<NotImplementedException>(() => calc.CalculateTemperatureShiftFactor(disregardRelax, 1.0));
        Assert.Throws<NotImplementedException>(() => calc.CalculateDisplacement(disregardRelax, 1.0));
        Assert.Throws<NotImplementedException>(() => calc.CalculateStrain(disregardRelax, 1.0));

        Assert.True(calc.CalculateForce(disregardRelax, 1.0) > 0);
        Assert.True(calc.CalculateStress(disregardRelax, 1.0) > 0);

        MechanicalModelInput<SchaperyConstitutiveParameters> disregardCreep = CreateSchaperyInput(ViscoelasticEffect.Creep, RampTimeConsideration.Disregard);
        Assert.Throws<NotImplementedException>(() => calc.CalculateForce(disregardCreep, 1.0));
        Assert.Throws<NotImplementedException>(() => calc.CalculateStress(disregardCreep, 1.0));

        MechanicalModelInput<SchaperyConstitutiveParameters> consider = CreateSchaperyInput(ViscoelasticEffect.Relaxation, RampTimeConsideration.ConsiderWithViscoelasticEffect);
        Assert.Equal(0.0, calc.CalculateForce(consider, 0.0));
        Assert.Equal(0.0, calc.CalculateStress(consider, 0.0));
        Assert.True(calc.CalculateForce(consider, 0.25) > 0);
        Assert.True(calc.CalculateStress(consider, 0.25) > 0);
    }

    [Fact]
    public void ModifiedSuperpositionMethodCalculator_CoversRelaxationCreepRampAndUnimplementedPaths()
    {
        ModifiedSuperpositionMethodCalculator calc = CreateMsmCalculator();
        MechanicalModelInput<ModifiedSuperpositionMethodConstitutiveParameters> disregardRelax = CreateMsmInput(ViscoelasticEffect.Relaxation, RampTimeConsideration.Disregard);

        Assert.Equal(51.0, calc.CalculateInitialYoungModulus(disregardRelax, 0.1), 8);
        Assert.Equal(0.205, calc.CalculateStressRelaxationRate(disregardRelax, 0.1), 8);
        Assert.Throws<NotImplementedException>(() => calc.CalculateCreepCompliance(disregardRelax, 1.0));
        Assert.Throws<NotImplementedException>(() => calc.CalculateDisplacement(disregardRelax, 1.0));
        Assert.Throws<NotImplementedException>(() => calc.CalculateStrain(disregardRelax, 1.0));

        Assert.True(calc.CalculateRelaxationFunction(disregardRelax, 1.0) > 0);
        Assert.True(calc.CalculateForce(disregardRelax, 1.0) > 0);
        Assert.True(calc.CalculateStress(disregardRelax, 1.0) > 0);

        MechanicalModelInput<ModifiedSuperpositionMethodConstitutiveParameters> disregardCreep = CreateMsmInput(ViscoelasticEffect.Creep, RampTimeConsideration.Disregard);
        Assert.Throws<NotImplementedException>(() => calc.CalculateForce(disregardCreep, 1.0));
        Assert.Throws<NotImplementedException>(() => calc.CalculateStress(disregardCreep, 1.0));

        MechanicalModelInput<ModifiedSuperpositionMethodConstitutiveParameters> consider = CreateMsmInput(ViscoelasticEffect.Relaxation, RampTimeConsideration.ConsiderWithViscoelasticEffect);
        Assert.Equal(0.0, calc.CalculateForce(consider, 0.0));
        Assert.Equal(0.0, calc.CalculateStress(consider, 0.0));
        Assert.True(calc.CalculateForce(consider, 0.25) > 0);
        Assert.True(calc.CalculateStress(consider, 0.25) > 0);
    }
}
