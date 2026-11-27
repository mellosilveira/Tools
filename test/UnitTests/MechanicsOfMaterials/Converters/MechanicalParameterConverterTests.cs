using MelloSilveiraTools.Mathematics.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Converters.MechanicalParameter;
using MelloSilveiraTools.MechanicsOfMaterials.Models;

namespace UnitTests.MechanicsOfMaterials.Converters;

/// <summary>
/// Unit tests for <see cref="MechanicalParameterConverter"/> covering small and large displacements as well as unimplemented paths.
/// </summary>
public sealed class MechanicalParameterConverterTests
{
    private static SpecimenParameter CreateSpecimen(bool largeDisplacement) => new(
        considerLargeDisplacement: largeDisplacement,
        considerAngleVariation: false,
        preLoadStrain: 0.1,
        preLoadLength: 0.11,
        area: 0.0001,
        initialAngle: Vector3D.Empty);

    [Fact]
    public void Conversions_SmallAndLargeDisplacements_CalculateExpectedValues()
    {
        MechanicalParameterConverter converter = new();
        SpecimenParameter small = CreateSpecimen(false);
        SpecimenParameter large = CreateSpecimen(true);

        // InitialLength = 0.11 / 1.1 = 0.1
        Assert.Equal(0.02, converter.CalculateDisplacementFromStrain(small, 0.2), 8);
        Assert.Equal(0.1 * (Math.Exp(0.2) - 1.0), converter.CalculateDisplacementFromStrain(large, 0.2), 8);

        Assert.Equal(0.05, converter.CalculateDisplacementDerivativeFromStrain(small, 0.2, 0.5), 8);
        Assert.Equal(0.1 * Math.Exp(0.2) * 0.5, converter.CalculateDisplacementDerivativeFromStrain(large, 0.2, 0.5), 8);

        Assert.Equal(0.2, converter.CalculateStrainFromDisplacement(small, 0.02), 8);
        Assert.Equal(Math.Log(1.2), converter.CalculateStrainFromDisplacement(large, 0.02), 8);

        Assert.Equal(5.0, converter.CalculateStrainDerivativeFromDisplacement(small, 0.02, 0.5), 8);
        Assert.Equal(0.5 / 0.12, converter.CalculateStrainDerivativeFromDisplacement(large, 0.02, 0.5), 8);

        Assert.Equal(0.1 * (1.1 * 1.2 - 1.0), converter.CalculatePreloadedDisplacementFromStrain(small, 0.2), 8);
        Assert.Equal(0.1 * (1.1 * Math.Exp(0.2) - 1.0), converter.CalculatePreloadedDisplacementFromStrain(large, 0.2), 8);

        Assert.Equal((0.02 - small.PreLoadDisplacement) / small.PreLoadLength, converter.CalculatePreloadedStrainFromDisplacement(small, 0.02), 8);
        Assert.Throws<NotImplementedException>(() => converter.CalculatePreloadedStrainFromDisplacement(large, 0.02));

        Assert.Equal(200.0, converter.CalculateForceFromStress(small, 2.0), 8);
        Assert.Equal(2.0, converter.CalculateStressFromForce(small, 200.0), 8);
    }

    [Fact]
    public void UnimplementedDerivativeMethods_ThrowNotImplementedException()
    {
        MechanicalParameterConverter converter = new();
        SpecimenParameter specimen = CreateSpecimen(false);

        Assert.Throws<NotImplementedException>(() => converter.CalculatePreloadedDisplacementDerivativeFromStrain(specimen, 0.1, 0.1));
        Assert.Throws<NotImplementedException>(() => converter.CalculatePreloadedStrainDerivativeFromDisplacement(specimen, 0.1, 0.1));
        Assert.Throws<NotImplementedException>(() => converter.CalculateStressDerivativeFromForce(specimen, 10.0, 1.0));
    }
}
