using MelloSilveiraTools.Mathematics.Models;
using System.Globalization;

namespace UnitTests.Mathematics.Models;

/// <summary>
/// Unit tests for <see cref="Vector3D"/> verifying culture-invariant parsing, formatting, and operations.
/// </summary>
public sealed class Vector3DTests
{
    /// <summary>
    /// Verifies that <see cref="Vector3D.Parse"/> parses decimal components using invariant culture regardless of thread culture.
    /// </summary>
    [Fact]
    public void Parse_WithInvariantCultureDecimals_ParsesCorrectlyRegardlessOfCurrentCulture()
    {
        // Arrange
        CultureInfo originalCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("pt-BR");
            string vectorString = "1.5,2.75,3.125";

            // Act
            Vector3D vector = Vector3D.Parse(vectorString);

            // Assert
            Assert.Equal(1.5, vector.X);
            Assert.Equal(2.75, vector.Y);
            Assert.Equal(3.125, vector.Z);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    /// <summary>
    /// Verifies that <see cref="Vector3D.TryParse"/> returns true and populates the vector under culture-sensitive environments.
    /// </summary>
    [Fact]
    public void TryParse_WithValidString_ReturnsTrueAndSetsVector()
    {
        // Arrange
        CultureInfo originalCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("pt-BR");
            string vectorString = "10.25,-5.5,0.0";

            // Act
            bool success = Vector3D.TryParse(vectorString, out Vector3D? vector);

            // Assert
            Assert.True(success);
            Assert.NotNull(vector);
            Assert.Equal(10.25, vector.Value.X);
            Assert.Equal(-5.5, vector.Value.Y);
            Assert.Equal(0.0, vector.Value.Z);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    /// <summary>
    /// Verifies that <see cref="Vector3D.TryParse"/> returns false and null when given an invalid string.
    /// </summary>
    [Theory]
    [InlineData("invalid")]
    [InlineData("1.0,2.0")]
    [InlineData("1.0,abc,3.0")]
    [InlineData("")]
    public void TryParse_WithInvalidString_ReturnsFalseAndNull(string invalidString)
    {
        // Act
        bool success = Vector3D.TryParse(invalidString, out Vector3D? vector);

        // Assert
        Assert.False(success);
        Assert.Null(vector);
    }

    /// <summary>
    /// Verifies that <see cref="Vector3D.SafeParse"/> returns <see cref="Vector3D.Empty"/> for null or whitespace, and parses valid strings.
    /// </summary>
    [Theory]
    [InlineData(null, 0.0, 0.0, 0.0)]
    [InlineData("", 0.0, 0.0, 0.0)]
    [InlineData("   ", 0.0, 0.0, 0.0)]
    [InlineData("1.0,2.0,3.0", 1.0, 2.0, 3.0)]
    public void SafeParse_ReturnsExpectedVector(string? input, double expectedX, double expectedY, double expectedZ)
    {
        // Act
        Vector3D vector = Vector3D.SafeParse(input!);

        // Assert
        Assert.Equal(expectedX, vector.X);
        Assert.Equal(expectedY, vector.Y);
        Assert.Equal(expectedZ, vector.Z);
    }

    /// <summary>
    /// Verifies that <see cref="Vector3D.ToString"/> formats components using invariant culture periods as decimal separator.
    /// </summary>
    [Fact]
    public void ToString_FormatsWithInvariantCulture()
    {
        // Arrange
        CultureInfo originalCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("pt-BR");
            Vector3D vector = Vector3D.Create(1.5, 2.5, 3.5);

            // Act
            string formatted = vector.ToString();

            // Assert
            Assert.Equal("1.5,2.5,3.5", formatted);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }
}
