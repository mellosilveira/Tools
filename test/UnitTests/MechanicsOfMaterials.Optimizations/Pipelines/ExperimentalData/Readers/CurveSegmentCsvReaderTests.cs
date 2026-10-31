using MelloSilveiraTools.Core.Managers.File.Providers;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Readers;
using Moq;
using System.Text;

namespace UnitTests.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Readers;

/// <summary>
/// Unit tests for <see cref="CurveSegmentCsvReader"/> ensuring segmented experimental CSV data
/// containing numeric segment type enumerations is parsed and grouped into <see cref="CurveSegment"/> instances correctly.
/// </summary>
public sealed class CurveSegmentCsvReaderTests
{
    private readonly Mock<IFileStorageService> _fileStorageServiceMock = new();

    [Fact]
    public async Task ReadSegmentsAsync_WhenValidNumericCsvStream_ParsesAndGroupsSegmentsCorrectly()
    {
        // Arrange
        string csvContent = "Time,Strain,Stress,SegmentType,StrainRate,StrainAcceleration,StressRate,StressAcceleration\r\n"
            + "0.0,0.0,0.0,1,0.01,0.0,10.0,0.0\r\n"
            + "1.0,0.01,10.0,1,0.01,0.0,10.0,0.0\r\n"
            + "2.0,0.01,9.5,2,0.0,0.0,-0.5,0.0\r\n"
            + "3.0,0.01,9.0,2,0.0,0.0,-0.5,0.0\r\n";

        byte[] bytes = Encoding.UTF8.GetBytes(csvContent);
        MemoryStream stream = new(bytes);

        _fileStorageServiceMock
            .Setup(s => s.OpenReadAsync("data/segmented.csv", It.IsAny<CancellationToken>()))
            .ReturnsAsync(stream);

        // Act
        IReadOnlyList<CurveSegment> segments = await CurveSegmentCsvReader.ReadSegmentsAsync(_fileStorageServiceMock.Object, "data/segmented.csv");

        // Assert
        Assert.NotNull(segments);
        Assert.Equal(2, segments.Count);

        CurveSegment rampSegment = segments[0];
        Assert.Equal(SegmentType.Ramp, rampSegment.Type);
        Assert.Equal([0.0, 1.0], rampSegment.TimePoints);
        Assert.Equal([0.0, 0.01], rampSegment.ExperimentalStrain);
        Assert.Equal([0.0, 10.0], rampSegment.ExperimentalStress);

        CurveSegment relaxationSegment = segments[1];
        Assert.Equal(SegmentType.Relaxation, relaxationSegment.Type);
        Assert.Equal([2.0, 3.0], relaxationSegment.TimePoints);
        Assert.Equal([0.01, 0.01], relaxationSegment.ExperimentalStrain);
        Assert.Equal([9.5, 9.0], relaxationSegment.ExperimentalStress);
    }
}
