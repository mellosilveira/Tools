using MelloSilveiraTools.Mathematics.Converters;
using MelloSilveiraTools.Mathematics.Models;

namespace UnitTests.Mathematics.Converters;

/// <summary>
/// Unit tests for <see cref="UnitConverter"/> covering scalar and vector unit conversions.
/// </summary>
public sealed class UnitConverterTests
{
    [Fact]
    public void ScalarAndVectorConversions_ReturnExpectedValues()
    {
        Assert.Equal(10.0, UnitConverter.FromKmhToMs(36.0), 10);
        Assert.Equal(Math.PI, UnitConverter.FromRpmToRads(30.0), 10);
        Assert.Equal(25.0, UnitConverter.ConvertDecimalToPercentage(0.25), 10);
        Assert.Equal(1.0, UnitConverter.ConvertMm2ToM2(1_000_000.0), 10);
        Assert.Equal(1.5, UnitConverter.ConvertMmToM(1500.0), 10);
        Assert.Equal(1500.0, UnitConverter.ConvertMToMm(1.5), 10);
        Assert.Equal(2.5, UnitConverter.ConvertPaToMPa(2_500_000.0), 10);
        Assert.Equal(2_500_000.0, UnitConverter.ConvertMPaToPa(2.5), 10);
        Assert.Equal(180.0, UnitConverter.ConvertRadToDegree(Math.PI), 10);

        Vector3D radians = Vector3D.Create(0.0, Math.PI / 2.0, Math.PI);
        Vector3D degrees = UnitConverter.ConvertRadToDegree(radians);

        Assert.Equal(0.0, degrees.X, 10);
        Assert.Equal(90.0, degrees.Y, 10);
        Assert.Equal(180.0, degrees.Z, 10);
    }
}
