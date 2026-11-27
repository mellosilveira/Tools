using MelloSilveiraTools.Mathematics.Functions;
using MelloSilveiraTools.Mathematics.Models;
using MelloSilveiraTools.Mathematics.NumericalMethods.Integrals;
using MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels.Viscoelasticity.Linear.Maxwell;
using MelloSilveiraTools.MechanicsOfMaterials.Converters.MechanicalParameter;
using MelloSilveiraTools.MechanicsOfMaterials.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity.Linear.Maxwell;

namespace UnitTests.MechanicsOfMaterials.Calculators.MechanicalModels.Viscoelasticity;

/// <summary>
/// Unit tests for <see cref="MaxwellModelCalculator"/> and base <c>LinearModelCalculator</c>.
/// </summary>
public sealed class MaxwellAndLinearModelCalculatorTests
{
    private static MaxwellModelCalculator CreateCalculator()
        => new(new SimpsonRuleIntegration(), new MechanicalParameterConverter());

    private static MechanicalModelInput<MaxwellConstitutiveParameters> CreateInput(
        ViscoelasticEffect effect = ViscoelasticEffect.Relaxation,
        RampTimeConsideration ramp = RampTimeConsideration.Disregard) => new()
        {
            MechanicalModelName = nameof(MechanicalModel.Maxwell),
            ViscoelasticEffect = effect,
            RampTimeConsideration = ramp,
            ConstitutiveParameters = new MaxwellConstitutiveParameters { Stiffness = 100.0, Viscosity = 200.0 },
            Specimen = new SpecimenParameter(false, false, 0.0, 0.1, 0.0001, Vector3D.Empty),
            Displacement = new MechanicalParameter(0.01, new PolynomialFunction([0.0, 0.01])),
            Force = new MechanicalParameter(100.0, new PolynomialFunction([0.0, 10.0])),
            Strain = new MechanicalParameter(0.1, new PolynomialFunction([0.0, 0.05])),
            Stress = new MechanicalParameter(10.0, new PolynomialFunction([0.0, 1.0])),
            TimeStep = 0.05
        };

    [Fact]
    public void KernelFunctions_ComputeRelaxationTimeRelaxationFunctionAndCreepCompliance()
    {
        MaxwellModelCalculator calc = CreateCalculator();
        MechanicalModelInput<MaxwellConstitutiveParameters> input = CreateInput();

        // tau = eta / mu = 200 / 100 = 2
        Assert.Equal(2.0, calc.CalculateRelaxationTime(input), 10);
        Assert.Equal(100.0 * Math.Exp(-1.0 / 2.0), calc.CalculateRelaxationFunction(input, 1.0), 10);
        Assert.Equal(1.0 / 100.0 + 4.0 / 200.0, calc.CalculateCreepCompliance(input, 4.0), 10);
    }

    [Fact]
    public void ForceDisplacementStressStrain_DisregardAndConsiderRamp_CoverAllBranches()
    {
        MaxwellModelCalculator calc = CreateCalculator();

        // Disregard + Relaxation
        MechanicalModelInput<MaxwellConstitutiveParameters> disregardRelax = CreateInput(ViscoelasticEffect.Relaxation, RampTimeConsideration.Disregard);
        Assert.True(calc.CalculateForce(disregardRelax, 1.0) > 0);
        Assert.True(calc.CalculateDisplacement(disregardRelax, 1.0) > 0);
        Assert.True(calc.CalculateStress(disregardRelax, 1.0) > 0);
        Assert.True(calc.CalculateStrain(disregardRelax, 1.0) > 0);

        // Disregard + Creep
        MechanicalModelInput<MaxwellConstitutiveParameters> disregardCreep = CreateInput(ViscoelasticEffect.Creep, RampTimeConsideration.Disregard);
        Assert.Throws<NotImplementedException>(() => calc.CalculateForce(disregardCreep, 1.0));
        Assert.Throws<NotImplementedException>(() => calc.CalculateStress(disregardCreep, 1.0));
        Assert.True(calc.CalculateDisplacement(disregardCreep, 1.0) > 0);
        Assert.True(calc.CalculateStrain(disregardCreep, 1.0) > 0);

        // ConsiderWithViscoelasticEffect
        MechanicalModelInput<MaxwellConstitutiveParameters> consider = CreateInput(ViscoelasticEffect.Relaxation, RampTimeConsideration.ConsiderWithViscoelasticEffect);
        Assert.Equal(0.0, calc.CalculateForce(consider, 0.0));
        Assert.True(calc.CalculateForce(consider, 0.5) > 0);
        Assert.Equal(0.0, calc.CalculateDisplacement(consider, 0.0));
        // CalculateStressDerivativeFromForce in MechanicalParameterConverter is NotImplementedException.
        Assert.Throws<NotImplementedException>(() => calc.CalculateDisplacement(consider, 0.5));
        Assert.True(calc.CalculateStress(consider, 0.5) > 0);
        Assert.True(calc.CalculateStrain(consider, 0.5) > 0);

        // ConsiderWithoutViscoelasticEffect exercises the fallback branches in LinearModelCalculator
        MechanicalModelInput<MaxwellConstitutiveParameters> withoutVe = CreateInput(ViscoelasticEffect.Relaxation, RampTimeConsideration.ConsiderWithoutViscoelasticEffect);
        Assert.Equal(0.0, calc.CalculateForce(withoutVe, 0.5));
        Assert.Equal(0.0, calc.CalculateDisplacement(withoutVe, 0.5));
    }
}
