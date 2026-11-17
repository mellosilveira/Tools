using MelloSilveiraTools.Mathematics.Functions;
using MelloSilveiraTools.Mathematics.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels.Elasticity;
using MelloSilveiraTools.MechanicsOfMaterials.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Elasticity;

namespace UnitTests.MechanicsOfMaterials.Calculators.MechanicalModels;

/// <summary>
/// Unit tests for <see cref="ElasticModelCalculator"/> covering stiffness, force, displacement, stress, and strain calculations.
/// </summary>
public sealed class ElasticModelCalculatorTests
{
    private static MechanicalModelInput<ElasticConstitutiveParameters> CreateInput() => new()
    {
        MechanicalModelName = nameof(MechanicalModel.Elastic),
        ConstitutiveParameters = new ElasticConstitutiveParameters { YoungModulus = 200.0 },
        Specimen = new SpecimenParameter(false, false, 0.0, 0.1, 0.0002, Vector3D.Empty),
        Displacement = new MechanicalParameter(0.0, new PolynomialFunction([0.0, 0.01])),
        Force = new MechanicalParameter(0.0, new PolynomialFunction([0.0, 4000.0])),
        Strain = new MechanicalParameter(0.0, new PolynomialFunction([0.0, 0.05])),
        Stress = new MechanicalParameter(0.0, new PolynomialFunction([0.0, 10.0])),
        TimeStep = 0.01
    };

    [Fact]
    public void SmallDisplacementElasticCalculations_ReturnExactLinearElasticResponses()
    {
        ElasticModelCalculator calc = new();
        MechanicalModelInput<ElasticConstitutiveParameters> input = CreateInput();

        // K = A * (E * 1e6) / L = 0.0002 * 200e6 / 0.1 = 400_000 N/m
        double expectedStiffness = 400_000.0;
        double time = 1.0;

        Assert.Equal(expectedStiffness, calc.CalculateStiffnessThroughDisplacement(input, time), 6);
        Assert.Equal(expectedStiffness, calc.CalculateStiffnessThroughForce(input, time), 6);
        Assert.Equal(0.01 * expectedStiffness, calc.CalculateForce(input, time), 6);
        Assert.Equal(4000.0 / expectedStiffness, calc.CalculateDisplacement(input, time), 6);

        Assert.Equal(10.0, calc.CalculateStress(input, time), 6);
        Assert.Equal(0.05, calc.CalculateStrain(input, time), 6);
    }

    [Fact]
    public void LargeDisplacementElasticCalculations_AvoidRecursionAndMatchLogarithmicKinematics()
    {
        ElasticModelCalculator calc = new();
        MechanicalModelInput<ElasticConstitutiveParameters> baseInput = CreateInput();
        SpecimenParameter largeSpecimen = new(true, false, 0.0, 0.1, 0.0002, Vector3D.Empty);
        MechanicalModelInput<ElasticConstitutiveParameters> input = baseInput with { Specimen = largeSpecimen };

        double time = 1.0;
        double displacement = 0.01;
        double expectedStrain = Math.Log(1.0 + displacement / largeSpecimen.InitialLength);
        double expectedForce = largeSpecimen.Area * (200.0 * 1e6) * expectedStrain;
        double expectedSecantStiffness = expectedForce / displacement;

        double actualForce = calc.CalculateForce(input, time);
        Assert.Equal(expectedForce, actualForce, 6);

        double recoveredDisplacement = calc.CalculateDisplacement(input, time, actualForce);
        Assert.Equal(displacement, recoveredDisplacement, 10);

        Assert.Equal(expectedSecantStiffness, calc.CalculateStiffnessThroughDisplacement(input, time), 6);

        double givenForce = 4000.0;
        double expectedDispFromForce = largeSpecimen.InitialLength * (Math.Exp(givenForce / (largeSpecimen.Area * 200.0 * 1e6)) - 1.0);
        Assert.Equal(expectedDispFromForce, calc.CalculateDisplacement(input, time), 10);
        Assert.Equal(givenForce / expectedDispFromForce, calc.CalculateStiffnessThroughForce(input, time), 6);

        // At u = 0 (t = 0), secant stiffness falls back smoothly to the initial tangent stiffness.
        double expectedInitialTangentStiffness = largeSpecimen.Area * 200.0 * 1e6 / largeSpecimen.InitialLength;
        Assert.Equal(expectedInitialTangentStiffness, calc.CalculateStiffnessThroughDisplacement(input, 0.0), 6);
    }
}
