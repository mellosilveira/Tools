using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Commands;
using Microsoft.AspNetCore.Http;
using Moq;

namespace UnitTests.MechanicsOfMaterials.Optimizations.Pipelines.ExperimentalData.Commands;

/// <summary>
/// Unit tests for <see cref="ProcessExperimentalDataValidator"/> validating all constraint checks.
/// </summary>
public sealed class ProcessExperimentalDataValidatorTests
{
    private readonly ProcessExperimentalDataValidator _validator = new();

    /// <summary>
    /// Verifies that validating a null request returns a bad request failure.
    /// </summary>
    [Fact]
    public void Validate_WhenRequestIsNull_ReturnsBadRequest()
    {
        // Act
        Result result = _validator.Validate(null!);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(StatusCode.BadRequest, result.StatusCode);
        Assert.Contains("Experimental data processing request cannot be null.", result.Messages);
    }

    /// <summary>
    /// Verifies that omitting the mechanical model name triggers a validation error.
    /// </summary>
    [Fact]
    public void Validate_WhenMechanicalModelNameIsMissing_ReturnsBadRequest()
    {
        // Arrange
        using MemoryStream strainStream = new([1, 2, 3]);
        using MemoryStream stressStream = new([4, 5, 6]);

        ProcessExperimentalDataRequest request = new()
        {
            MechanicalModelName = string.Empty,
            StrainStream = strainStream,
            StressStream = stressStream,
            TargetRSquared = 0.95
        };

        // Act
        Result result = _validator.Validate(request);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("MechanicalModelName is required.", result.Messages);
    }

    /// <summary>
    /// Verifies that omitting strain and stress streams or files triggers validation errors.
    /// </summary>
    [Fact]
    public void Validate_WhenStreamsAndFilesAreMissing_ReturnsBadRequest()
    {
        // Arrange
        ProcessExperimentalDataRequest request = new()
        {
            MechanicalModelName = "Maxwell",
            StrainStream = null,
            StressStream = null,
            StrainFile = null,
            StressFile = null,
            TargetRSquared = 0.95
        };

        // Act
        Result result = _validator.Validate(request);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("A non-empty StrainFile or StrainStream must be provided.", result.Messages);
        Assert.Contains("A non-empty StressFile or StressStream must be provided.", result.Messages);
    }

    /// <summary>
    /// Verifies that providing an empty FormFile (0 bytes) is flagged as missing.
    /// </summary>
    [Fact]
    public void Validate_WhenFormFilesAreEmpty_ReturnsBadRequest()
    {
        // Arrange
        Mock<IFormFile> emptyStrainMock = new();
        emptyStrainMock.Setup(f => f.Length).Returns(0L);

        Mock<IFormFile> emptyStressMock = new();
        emptyStressMock.Setup(f => f.Length).Returns(0L);

        ProcessExperimentalDataRequest request = new()
        {
            MechanicalModelName = "Maxwell",
            StrainFile = emptyStrainMock.Object,
            StressFile = emptyStressMock.Object,
            TargetRSquared = 0.95
        };

        // Act
        Result result = _validator.Validate(request);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("A non-empty StrainFile or StrainStream must be provided.", result.Messages);
        Assert.Contains("A non-empty StressFile or StressStream must be provided.", result.Messages);
    }

    /// <summary>
    /// Verifies that invalid TargetRSquared values (less than or equal to zero, or greater than 1.0) trigger validation errors.
    /// </summary>
    [Theory]
    [InlineData(0.0)]
    [InlineData(-0.5)]
    [InlineData(1.05)]
    public void Validate_WhenTargetRSquaredIsInvalid_ReturnsBadRequest(double targetRSquared)
    {
        // Arrange
        using MemoryStream strainStream = new([1, 2]);
        using MemoryStream stressStream = new([3, 4]);

        ProcessExperimentalDataRequest request = new()
        {
            MechanicalModelName = "Maxwell",
            StrainStream = strainStream,
            StressStream = stressStream,
            TargetRSquared = targetRSquared
        };

        // Act
        Result result = _validator.Validate(request);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("TargetRSquared must be within the interval (0, 1].", result.Messages);
    }

    /// <summary>
    /// Verifies that negative thresholds and step parameters trigger validation errors.
    /// </summary>
    [Fact]
    public void Validate_WhenThresholdsOrStepsAreInvalid_ReturnsBadRequest()
    {
        // Arrange
        using MemoryStream strainStream = new([1, 2]);
        using MemoryStream stressStream = new([3, 4]);

        ProcessExperimentalDataRequest request = new()
        {
            MechanicalModelName = "Maxwell",
            StrainStream = strainStream,
            StressStream = stressStream,
            TargetRSquared = 0.95,
            AsymptoteConsecutivePointsThreshold = -1,
            SimulationTimeStep = -0.1,
            DownsamplingSkipTimeStep = -1
        };

        // Act
        Result result = _validator.Validate(request);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("AsymptoteConsecutivePointsThreshold cannot be negative.", result.Messages);
        Assert.Contains("SimulationTimeStep must be greater than zero when specified.", result.Messages);
        Assert.Contains("DownsamplingSkipTimeStep cannot be negative.", result.Messages);
    }

    /// <summary>
    /// Verifies that a well-formed request with streams passes validation.
    /// </summary>
    [Fact]
    public void Validate_WhenValidWithStreams_ReturnsSuccess()
    {
        // Arrange
        using MemoryStream strainStream = new([1, 2, 3]);
        using MemoryStream stressStream = new([4, 5, 6]);

        ProcessExperimentalDataRequest request = new()
        {
            MechanicalModelName = "Maxwell",
            StrainStream = strainStream,
            StressStream = stressStream,
            TargetRSquared = 0.99,
            SimulationTimeStep = 0.05,
            AsymptoteConsecutivePointsThreshold = 5,
            DownsamplingSkipTimeStep = 2
        };

        // Act
        Result result = _validator.Validate(request);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(StatusCode.OK, result.StatusCode);
        Assert.Empty(result.Messages);
    }

    /// <summary>
    /// Verifies that a well-formed request with FormFiles passes validation.
    /// </summary>
    [Fact]
    public void Validate_WhenValidWithFormFiles_ReturnsSuccess()
    {
        // Arrange
        Mock<IFormFile> strainFileMock = new();
        strainFileMock.Setup(f => f.Length).Returns(100L);

        Mock<IFormFile> stressFileMock = new();
        stressFileMock.Setup(f => f.Length).Returns(100L);

        ProcessExperimentalDataRequest request = new()
        {
            MechanicalModelName = "Maxwell",
            StrainFile = strainFileMock.Object,
            StressFile = stressFileMock.Object,
            TargetRSquared = 0.99
        };

        // Act
        Result result = _validator.Validate(request);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(StatusCode.OK, result.StatusCode);
    }
}
