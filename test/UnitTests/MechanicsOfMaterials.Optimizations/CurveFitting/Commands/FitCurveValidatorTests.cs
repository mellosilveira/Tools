using MelloSilveiraTools.Core.Models;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Commands;
using MelloSilveiraTools.MechanicsOfMaterials.Optimizations.CurveFitting.Models;

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
            Segments =
            [
                new CurveSegment
                {
                    Type = SegmentType.Relaxation,
                    TimePoints = [0.0, 1.0],
                    ExperimentalStrain = [0.01, 0.01],
                    ExperimentalStress = [10.0, 9.0]
                }
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
    /// Verifies that omitting segments or providing an empty segments collection triggers a validation error.
    /// </summary>
    [Fact]
    public void Validate_WhenSegmentsIsNullOrEmpty_ReturnsBadRequest()
    {
        // Arrange
        FitCurveRequest request = new()
        {
            MechanicalModelName = "Maxwell",
            Segments = [],
            TargetRSquared = 0.95
        };

        // Act
        Result result = _validator.Validate(request);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("At least one CurveSegment must be provided in Segments.", result.Messages);
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
            Segments =
            [
                new CurveSegment
                {
                    Type = SegmentType.Relaxation,
                    TimePoints = [0.0, 1.0],
                    ExperimentalStrain = [0.01, 0.01],
                    ExperimentalStress = [10.0, 9.0]
                }
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
            Segments =
            [
                new CurveSegment
                {
                    Type = SegmentType.Relaxation,
                    TimePoints = [0.0, 1.0],
                    ExperimentalStrain = [0.01, 0.01],
                    ExperimentalStress = [10.0, 9.0]
                }
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
}
