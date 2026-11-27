using MelloSilveiraTools.MechanicsOfMaterials.Calculators.ConstitutiveEquations;
using MelloSilveiraTools.MechanicsOfMaterials.Calculators.Fatigue;
using MelloSilveiraTools.MechanicsOfMaterials.Calculators.GeometricProperties;
using MelloSilveiraTools.MechanicsOfMaterials.Models.Fatigue;
using MelloSilveiraTools.MechanicsOfMaterials.Models.Materials;
using MelloSilveiraTools.MechanicsOfMaterials.Models.Profiles;

namespace UnitTests.MechanicsOfMaterials.Calculators;

/// <summary>
/// Unit tests for geometric profile calculators, <see cref="ConstitutiveEquationsCalculator"/>,
/// <see cref="FatigueCalculator"/>, and <see cref="Material"/>.
/// </summary>
public sealed class GeometricAndConstitutiveCalculatorsTests
{
    [Fact]
    public void CircularProfileGeometricPropertyCalculator_CalculatesSolidAndHollowProperties()
    {
        CircularProfileGeometricPropertyCalculator calc = new();
        CircularProfile solid = new() { Diameter = 10.0, Thickness = null };
        CircularProfile hollow = new() { Diameter = 10.0, Thickness = 2.0 };

        Assert.Equal(Math.PI * 25.0, calc.CalculateArea(solid), 8);
        Assert.Equal(Math.PI * 10000.0 / 64.0, calc.CalculateMomentOfInertia(solid), 8);

        double expectedHollowArea = Math.PI / 4.0 * (100.0 - 36.0);
        double expectedHollowI = Math.PI / 64.0 * (10000.0 - 1296.0);
        Assert.Equal(expectedHollowArea, calc.CalculateArea(hollow), 8);
        Assert.Equal(expectedHollowI, calc.CalculateMomentOfInertia(hollow), 8);
    }

    [Fact]
    public void RectangularProfileGeometricPropertyCalculator_CalculatesSolidAndHollowProperties()
    {
        RectangularProfileGeometricPropertyCalculator calc = new();
        RectangularProfile solid = new() { Width = 10.0, Height = 20.0, Thickness = null };
        RectangularProfile hollow = new() { Width = 10.0, Height = 20.0, Thickness = 2.0 };

        Assert.Equal(200.0, calc.CalculateArea(solid), 8);
        Assert.Equal(10.0 * 8000.0 / 12.0, calc.CalculateMomentOfInertia(solid), 8);

        Assert.Equal(200.0 - 6.0 * 16.0, calc.CalculateArea(hollow), 8);
        Assert.Equal((8000.0 * 10.0 - 4096.0 * 6.0) / 12.0, calc.CalculateMomentOfInertia(hollow), 8);
    }

    [Fact]
    public void ConstitutiveEquationsCalculator_ComputesStressAndBucklingCorrectly()
    {
        ConstitutiveEquationsCalculator calc = new();

        Assert.Equal(5.0, calc.CalculateNormalStress(100.0, 20.0), 8);
        // sqrt((3+0)^2 + 3*(4/sqrt(3)?) -> test exact formula sqrt((1+2)^2 + 3*(1+1)^2) = sqrt(9 + 12) = sqrt(21)
        Assert.Equal(Math.Sqrt(21.0), calc.CalculateEquivalentStress(1.0, 2.0, 1.0, 1.0), 8);

        Assert.Equal(1.0, calc.CalculateColumnEffectiveLengthFactor(FasteningType.BothEndPinned), 8);
        Assert.Equal(0.5, calc.CalculateColumnEffectiveLengthFactor(FasteningType.BothEndFixed), 8);
        Assert.Equal(Math.Sqrt(2.0) / 2.0, calc.CalculateColumnEffectiveLengthFactor(FasteningType.OneEndFixedOneEndPinned), 8);
        Assert.Equal(2.0, calc.CalculateColumnEffectiveLengthFactor(FasteningType.OneEndFixed), 8);
        Assert.Throws<ArgumentOutOfRangeException>(() => calc.CalculateColumnEffectiveLengthFactor((FasteningType)999));

        double buckling = calc.CalculateCriticalBucklingForce(200_000.0, 1000.0, 1000.0, FasteningType.BothEndPinned);
        Assert.True(buckling > 0);
    }

    [Theory]
    [InlineData(SurfaceFinish.Rectified, LoadingType.Bending, Reliability.Fifty, true, 20.0)]
    [InlineData(SurfaceFinish.Machined, LoadingType.Axial, Reliability.Ninety, false, 20.0)]
    [InlineData(SurfaceFinish.ColdRolled, LoadingType.Torsion, Reliability.NinetyFive, false, 10.0)]
    [InlineData(SurfaceFinish.HotRolled, LoadingType.Bending, Reliability.NinetyNine, false, 100.0)]
    [InlineData(SurfaceFinish.Wrought, LoadingType.Bending, Reliability.NinetyNinePointNine, true, 20.0)]
    public void FatigueCalculator_AllValidMarinCombinations_ComputeOutput(
        SurfaceFinish finish,
        LoadingType loading,
        Reliability reliability,
        bool rotating,
        double diameter)
    {
        FatigueCalculator calc = new();
        FatigueInput input = new()
        {
            MinimumAppliedStress = -100.0,
            MaximumAppliedStress = 200.0,
            TensileStress = 500.0,
            FatigueLimit = 250.0,
            FatigueLimitFraction = 0.9,
            IsRotativeSection = rotating,
            Reliability = reliability,
            SurfaceFinish = finish,
            LoadingType = loading,
            Temperature = 25.0,
            Profile = new CircularProfile { Diameter = diameter }
        };

        FatigueOutput output = calc.Calculate(input);
        Assert.Equal(150.0, output.StressAmplitude, 6);
        Assert.Equal(50.0, output.MeanStress, 6);
        Assert.True(output.EquivalentStress > 0);
        Assert.True(output.NumberOfCycles > 0 && output.NumberOfCycles <= 1e6);
        Assert.True(output.SafetyFactor > 0);
    }

    [Fact]
    public void FatigueCalculator_RectangularProfileCappedCyclesAndInvalidEnums_BehaveAsImplemented()
    {
        FatigueCalculator calc = new();
        FatigueInput rectSmallStress = new()
        {
            MinimumAppliedStress = 0.0,
            MaximumAppliedStress = 2.0,
            TensileStress = 500.0,
            FatigueLimit = 100.0,
            FatigueLimitFraction = 0.9,
            IsRotativeSection = false,
            Reliability = Reliability.Fifty,
            SurfaceFinish = SurfaceFinish.Rectified,
            LoadingType = LoadingType.Bending,
            Temperature = 25.0,
            Profile = new RectangularProfile { Width = 10.0, Height = 10.0 }
        };

        FatigueOutput capped = calc.Calculate(rectSmallStress);
        Assert.Equal(1e6, capped.NumberOfCycles);

        FatigueInput rotatingRect = new()
        {
            MinimumAppliedStress = 0.0,
            MaximumAppliedStress = 50.0,
            TensileStress = 500.0,
            FatigueLimit = 250.0,
            FatigueLimitFraction = 0.9,
            IsRotativeSection = true,
            Reliability = Reliability.Fifty,
            SurfaceFinish = SurfaceFinish.Rectified,
            LoadingType = LoadingType.Bending,
            Temperature = 25.0,
            Profile = new RectangularProfile { Width = 10.0, Height = 60.0 }
        };
        Assert.True(calc.CalculateModifiedFatigueStress(rotatingRect) > 0);

        Assert.Throws<ArgumentOutOfRangeException>(() => calc.CalculateModifiedFatigueStress(new FatigueInput
        {
            MinimumAppliedStress = 0.0,
            MaximumAppliedStress = 2.0,
            TensileStress = 500.0,
            FatigueLimit = 100.0,
            FatigueLimitFraction = 0.9,
            IsRotativeSection = false,
            Reliability = Reliability.Fifty,
            SurfaceFinish = SurfaceFinish.Rectified,
            LoadingType = LoadingType.Bending,
            Temperature = 25.0,
            Profile = new RectangularProfile { Width = 0.01, Height = 0.01 }
        }));

        Assert.Throws<ArgumentOutOfRangeException>(() => calc.CalculateModifiedFatigueStress(new FatigueInput
        {
            TensileStress = 500,
            FatigueLimit = 250,
            SurfaceFinish = (SurfaceFinish)999,
            LoadingType = LoadingType.Axial,
            Reliability = Reliability.Fifty,
            Profile = new CircularProfile { Diameter = 10 }
        }));

        Assert.Throws<ArgumentOutOfRangeException>(() => calc.CalculateModifiedFatigueStress(new FatigueInput
        {
            TensileStress = 500,
            FatigueLimit = 250,
            SurfaceFinish = SurfaceFinish.Rectified,
            LoadingType = LoadingType.Bending,
            Reliability = Reliability.Fifty,
            Profile = new CircularProfile { Diameter = 0.0001 }
        }));

        Assert.Throws<ArgumentOutOfRangeException>(() => calc.CalculateModifiedFatigueStress(new FatigueInput
        {
            TensileStress = 500,
            FatigueLimit = 250,
            IsRotativeSection = true,
            SurfaceFinish = SurfaceFinish.Rectified,
            LoadingType = LoadingType.Bending,
            Reliability = Reliability.Fifty,
            Profile = new CircularProfile { Diameter = 300.0 }
        }));

        // Rotating circular profile triggers isRotativeSection == true for CircularProfile
        double rotatingCircleStress = calc.CalculateModifiedFatigueStress(new FatigueInput
        {
            TensileStress = 500,
            FatigueLimit = 250,
            IsRotativeSection = true,
            SurfaceFinish = SurfaceFinish.Rectified,
            LoadingType = LoadingType.Bending,
            Reliability = Reliability.Fifty,
            Profile = new CircularProfile { Diameter = 10.0 }
        });
        Assert.True(rotatingCircleStress > 0);

        ArgumentOutOfRangeException loadingEx = Assert.Throws<ArgumentOutOfRangeException>(() => calc.CalculateModifiedFatigueStress(new FatigueInput
        {
            TensileStress = 500,
            FatigueLimit = 250,
            SurfaceFinish = SurfaceFinish.Rectified,
            LoadingType = (LoadingType)999,
            Reliability = Reliability.Fifty,
            Profile = new CircularProfile { Diameter = 10.0 }
        }));
        Assert.Equal("loadingType", loadingEx.ParamName);

        ArgumentOutOfRangeException reliabilityEx = Assert.Throws<ArgumentOutOfRangeException>(() => calc.CalculateModifiedFatigueStress(new FatigueInput
        {
            TensileStress = 500,
            FatigueLimit = 250,
            SurfaceFinish = SurfaceFinish.Rectified,
            LoadingType = LoadingType.Axial,
            Reliability = (Reliability)123,
            Profile = new CircularProfile { Diameter = 10.0 }
        }));
        Assert.Equal("reliability", reliabilityEx.ParamName);
    }

    [Fact]
    public void MaterialCreate_ResolvesAllDefinedMaterialsAndThrowsOnInvalid()
    {
        Assert.Equal(Material.Steel1020, Material.Create(MaterialType.Steel1020));
        Assert.Equal(Material.Steel1045, Material.Create(MaterialType.Steel1045));
        Assert.Equal(Material.Steel4130, Material.Create(MaterialType.Steel4130));
        Assert.Equal(Material.Aluminum6061T6, Material.Create(MaterialType.Aluminum6061T6));
        Assert.Throws<ArgumentOutOfRangeException>(() => Material.Create((MaterialType)999));
    }
}
