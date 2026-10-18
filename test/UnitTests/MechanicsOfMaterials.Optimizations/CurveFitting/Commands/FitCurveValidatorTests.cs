using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Commands;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Models;
using Microsoft.AspNetCore.Http;
using Moq;

namespace UnitTests.MechanicsOfMaterials.Optimizations.CurveFitting.Commands;

/// <summary>
/// Unit tests for <see cref="FitCurveValidator"/> verifying validation constraints on curve fitting requests.
/// </summary>
public sealed class FitCurveValidatorTests
{
    private readonly FitCurveValidator _validator = new();

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
        Assert.Contains("Curve fitting request cannot be null.", result.Messages);
    }

    /// <summary>
    /// Verifies that omitting the mechanical model name triggers a validation error.
    /// </summary>
    [Fact]
    public void Validate_WhenMechanicalModelNameIsMissing_ReturnsBadRequest()
    {
        // Arrange
        FitCurveRequest request = new()
        {
            MechanicalModelName = string.Empty,
            SegmentFiles =
            [
                new SegmentFilesGroup(SegmentType.Relaxation, [CreateMockFormFile()])
            ],
            TargetRSquared = 0.95
        };

        // Act
        Result result = _validator.Validate(request);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("MechanicalModelName is required.", result.Messages);
    }

    /// <summary>
    /// Verifies that omitting segment files or providing an empty collection triggers a validation error.
    /// </summary>
    [Fact]
    public void Validate_WhenSegmentFilesIsNullOrEmpty_ReturnsBadRequest()
    {
        // Arrange
        FitCurveRequest request = new()
        {
            MechanicalModelName = "Maxwell",
            SegmentFiles = [],
            TargetRSquared = 0.95
        };

        // Act
        Result result = _validator.Validate(request);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("At least one SegmentFilesGroup must be provided in SegmentFiles.", result.Messages);
    }

    /// <summary>
    /// Verifies that a segment files group with an unknown segment type triggers a validation error.
    /// </summary>
    [Fact]
    public void Validate_WhenGroupHasUnknownSegmentType_ReturnsBadRequest()
    {
        // Arrange
        FitCurveRequest request = new()
        {
            MechanicalModelName = "Maxwell",
            SegmentFiles =
            [
                new SegmentFilesGroup(SegmentType.Unknown, [CreateMockFormFile()])
            ],
            TargetRSquared = 0.95
        };

        // Act
        Result result = _validator.Validate(request);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("SegmentFiles group at index 0 must have a valid SegmentType other than Unknown.", result.Messages);
    }

    /// <summary>
    /// Verifies that a segment files group with empty files collection triggers a validation error.
    /// </summary>
    [Fact]
    public void Validate_WhenGroupHasNoFiles_ReturnsBadRequest()
    {
        // Arrange
        FitCurveRequest request = new()
        {
            MechanicalModelName = "Maxwell",
            SegmentFiles =
            [
                new SegmentFilesGroup(SegmentType.Relaxation, [])
            ],
            TargetRSquared = 0.95
        };

        // Act
        Result result = _validator.Validate(request);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("SegmentFiles group at index 0 must contain at least one file.", result.Messages);
    }

    /// <summary>
    /// Verifies that a segment files group containing an empty or zero-length file triggers a validation error.
    /// </summary>
    [Fact]
    public void Validate_WhenGroupHasEmptyFile_ReturnsBadRequest()
    {
        // Arrange
        FitCurveRequest request = new()
        {
            MechanicalModelName = "Maxwell",
            SegmentFiles =
            [
                new SegmentFilesGroup(SegmentType.Relaxation, [CreateMockFormFile(0L)])
            ],
            TargetRSquared = 0.95
        };

        // Act
        Result result = _validator.Validate(request);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("File at index 0 in SegmentFiles group 0 cannot be null or empty.", result.Messages);
    }

    /// <summary>
    /// Verifies that invalid TargetRSquared values (less than or equal to zero, or greater than 1.0) trigger validation errors.
    /// </summary>
    [Theory]
    [InlineData(0.0)]
    [InlineData(-0.1)]
    [InlineData(1.01)]
    public void Validate_WhenTargetRSquaredIsInvalid_ReturnsBadRequest(double targetRSquared)
    {
        // Arrange
        FitCurveRequest request = new()
        {
            MechanicalModelName = "Maxwell",
            SegmentFiles =
            [
                new SegmentFilesGroup(SegmentType.Relaxation, [CreateMockFormFile()])
            ],
            TargetRSquared = targetRSquared
        };

        // Act
        Result result = _validator.Validate(request);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("TargetRSquared must be within the interval (0, 1].", result.Messages);
    }

    /// <summary>
    /// Verifies that a well-formed request passes validation.
    /// </summary>
    [Fact]
    public void Validate_WhenValid_ReturnsSuccess()
    {
        // Arrange
        FitCurveRequest request = new()
        {
            MechanicalModelName = "Maxwell",
            SegmentFiles =
            [
                new SegmentFilesGroup(SegmentType.Relaxation, [CreateMockFormFile()])
            ],
            TargetRSquared = 0.99
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
