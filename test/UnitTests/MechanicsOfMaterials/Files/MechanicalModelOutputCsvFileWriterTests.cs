using MelloSilveiraTools.Core.Files.Models;
using MelloSilveiraTools.Core.Files.Providers;
using MelloSilveiraTools.MechanicsOfMaterials.Calculators.MechanicalModels.TypeResolvers;
using MelloSilveiraTools.MechanicsOfMaterials.Files;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Elasticity;
using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Viscoelasticity;
using Moq;
using System.Text;

namespace UnitTests.MechanicsOfMaterials.Files;

/// <summary>
/// Unit tests for <see cref="MechanicalModelOutputCsvFileWriter"/>.
/// </summary>
public sealed class MechanicalModelOutputCsvFileWriterTests
{
    [Fact]
    public async Task WriteLineAsync_WritesFilteredHeaderAndFormattedRows()
    {
        using MemoryStream stream = new();
        ElasticModelTypeResolver resolver = new();

        await using (MechanicalModelOutputCsvFileWriter writer = new(
            resolver,
            LoadResponseRelationship.ForceDisplacement,
            ViscoelasticEffect.Relaxation,
            stream))
        {
            await writer.WriteLineAsync(new ElasticModelOutput
            {
                Time = 0.5,
                Displacement = 0.01,
                DisplacementDerivative = 0.02,
                Force = 100.0,
                Stiffness = 10000.0
            });
        }

        string content = Encoding.UTF8.GetString(stream.ToArray());
        string[] lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        Assert.Equal(2, lines.Length);
        Assert.Contains("Stiffness", lines[0]);
        Assert.Contains("Force", lines[0]);
        Assert.DoesNotContain("Stress,", lines[0]);
        Assert.Contains("10000", lines[1]);
    }

    [Fact]
    public async Task CreateAsync_UsesFileStorageServiceAndReturnsLocationAndWriter()
    {
        using MemoryStream stream = new();
        Mock<IFileStorageService> storageMock = new();
        storageMock
            .Setup(s => s.CreateWriteTimebasedAsync("id-1", "Elastic", nameof(RampTimeConsideration.Disregard), FileExtensions.CommaSeparatedValues))
            .ReturnsAsync((stream, "out/elastic.csv"));

        (string location, MechanicalModelOutputCsvFileWriter writer) = await MechanicalModelOutputCsvFileWriter.CreateAsync(
            "id-1",
            storageMock.Object,
            new ElasticModelTypeResolver(),
            LoadResponseRelationship.StressStrain,
            ViscoelasticEffect.Relaxation,
            RampTimeConsideration.Disregard);

        Assert.Equal("out/elastic.csv", location);
        await writer.DisposeAsync();
    }
}
