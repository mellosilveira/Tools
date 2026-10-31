using MelloSilveiraTools.MechanicsOfMaterials.Models.MechanicalModels.Elasticity;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.ExperimentalData.Serialization;

namespace UnitTests.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Serialization;

/// <summary>
/// Unit tests for <see cref="StepPayloadCompressor"/> verifying Brotli compression, decompression,
/// and fidelity of serialized step payload models.
/// </summary>
public sealed class StepPayloadCompressorTests
{
    /// <summary>
    /// Verifies that compressing a payload produces non-empty byte array and decompresses back to equivalent data.
    /// </summary>
    [Fact]
    public void CompressAndDecompress_WhenGivenCheckpointInput_ReturnsIdenticalObject()
    {
        // Arrange
        CurveFitterStepCheckpointInput input = new(
            "storage/exp_data.csv",
            "Maxwell",
            [SegmentType.Ramp, SegmentType.Relaxation],
            CurveFitterType.Alglib,
            0.985);

        // Act
        byte[] compressed = StepPayloadCompressor.Compress(input);
        CurveFitterStepCheckpointInput? decompressed = StepPayloadCompressor.Decompress<CurveFitterStepCheckpointInput>(compressed);

        // Assert
        Assert.NotNull(compressed);
        Assert.NotEmpty(compressed);
        Assert.NotNull(decompressed);
        Assert.Equal(input.SegmentedFileLocation, decompressed.SegmentedFileLocation);
        Assert.Equal(input.MechanicalModelName, decompressed.MechanicalModelName);
        Assert.Equal(input.TargetSegments, decompressed.TargetSegments);
        Assert.Equal(input.CurveFitterType, decompressed.CurveFitterType);
        Assert.Equal(input.TargetRSquared, decompressed.TargetRSquared);
    }

    /// <summary>
    /// Verifies that decompressing a null or empty byte array returns default or null without throwing exceptions.
    /// </summary>
    [Fact]
    public void Decompress_WhenGivenEmptyOrNullBytes_ReturnsDefault()
    {
        // Act
        CurveFitterStepCheckpointInput? decompressedFromNull = StepPayloadCompressor.Decompress<CurveFitterStepCheckpointInput>(null);
        CurveFitterStepCheckpointInput? decompressedFromEmpty = StepPayloadCompressor.Decompress<CurveFitterStepCheckpointInput>([]);

        // Assert
        Assert.Null(decompressedFromNull);
        Assert.Null(decompressedFromEmpty);
    }

    /// <summary>
    /// Verifies that simulation checkpoint output with nested delta structures is preserved through compression round-trip.
    /// </summary>
    [Fact]
    public void CompressAndDecompress_WhenGivenSimulationOutput_PreservesNestedDeltas()
    {
        // Arrange
        ElasticModelOutput initial = new() { Stiffness = 100.5 };
        ElasticModelOutput final = new() { Stiffness = 80.2 };
        ElasticModelOutput absolute = new() { Stiffness = 20.3 };
        ElasticModelOutput percentage = new() { Stiffness = 20.199 };

        SimulationDeltaOutput delta = new(initial, final, absolute, percentage);

        SimulationStepCheckpointOutput output = new(
            "storage/sim_output.csv",
            45.2,
            delta);

        // Act
        byte[] compressed = StepPayloadCompressor.Compress(output);
        SimulationStepCheckpointOutput? decompressed = StepPayloadCompressor.Decompress<SimulationStepCheckpointOutput>(compressed);

        // Assert
        Assert.NotNull(decompressed);
        Assert.Equal(output.SimulationOutputFileLocation, decompressed.SimulationOutputFileLocation);
        Assert.Equal(output.AsymptoteTime, decompressed.AsymptoteTime);
        Assert.NotNull(decompressed.DeltaOutput);
        Assert.NotNull(decompressed.DeltaOutput.InitialOutput);
        Assert.NotNull(decompressed.DeltaOutput.FinalOutput);
        Assert.NotNull(decompressed.DeltaOutput.AbsoluteDelta);
        Assert.NotNull(decompressed.DeltaOutput.PercentageDelta);
    }
}
