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
        ProcessExperimentalDataRequest request = new()
        {
            MechanicalModelName = string.Empty,
            StrainFile = CreateMockFormFile(),
            StressFile = CreateMockFormFile(),
            TargetRSquared = 0.95
        };

        // Act
        Result result = _validator.Validate(request);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("MechanicalModelName is required.", result.Messages);
    }

    /// <summary>
    /// Verifies that providing an empty FormFile (0 bytes) is flagged as missing.
    /// </summary>
    [Fact]
    public void Validate_WhenFormFilesAreEmpty_ReturnsBadRequest()
    {
        // Arrange
        ProcessExperimentalDataRequest request = new()
        {
            MechanicalModelName = "Maxwell",
            StrainFile = CreateMockFormFile(0L),
            StressFile = CreateMockFormFile(0L),
            TargetRSquared = 0.95
        };

        // Act
        Result result = _validator.Validate(request);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("A non-empty StrainFile must be provided.", result.Messages);
        Assert.Contains("A non-empty StressFile must be provided.", result.Messages);
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
        ProcessExperimentalDataRequest request = new()
        {
            MechanicalModelName = "Maxwell",
            StrainFile = CreateMockFormFile(),
            StressFile = CreateMockFormFile(),
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
        ProcessExperimentalDataRequest request = new()
        {
            MechanicalModelName = "Maxwell",
            StrainFile = CreateMockFormFile(),
            StressFile = CreateMockFormFile(),
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
    /// Verifies that a well-formed request with FormFiles passes validation.
    /// </summary>
    [Fact]
    public void Validate_WhenValidWithFormFiles_ReturnsSuccess()
    {
        // Arrange
        ProcessExperimentalDataRequest request = new()
        {
            MechanicalModelName = "Maxwell",
            StrainFile = CreateMockFormFile(100L),
            StressFile = CreateMockFormFile(100L),
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

    private static IFormFile CreateMockFormFile(long length = 100L)
    {
        Mock<IFormFile> mock = new();
        mock.Setup(f => f.Length).Returns(length);
        return mock.Object;
    }
}
