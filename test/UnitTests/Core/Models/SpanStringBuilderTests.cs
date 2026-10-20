using MelloSilveiraTools.Core.Models;

namespace UnitTests.Core.Models;

/// <summary>
/// Unit tests for <see cref="SpanStringBuilder"/> verifying stack-based string building,
/// pooled buffer growth, indexer access, line formatting, and deterministic disposal.
/// </summary>
public sealed class SpanStringBuilderTests
{
    /// <summary>
    /// Verifies that appending characters builds the expected string.
    /// </summary>
    [Fact]
    public void Append_Characters_BuildsExpectedString()
    {
        // Arrange
        using SpanStringBuilder builder = new(8);

        // Act
        builder.Append('A').Append('B').Append('C');
        string result = builder.ToString();

        // Assert
        Assert.Equal("ABC", result);
    }

    /// <summary>
    /// Verifies that appending string spans builds the expected composite string.
    /// </summary>
    [Fact]
    public void Append_ReadOnlySpan_BuildsExpectedString()
    {
        // Arrange
        using SpanStringBuilder builder = new(16);

        // Act
        builder.Append("Hello, ".AsSpan()).Append("World!".AsSpan());
        string result = builder.ToString();

        // Assert
        Assert.Equal("Hello, World!", result);
    }

    /// <summary>
    /// Verifies that appending past initial capacity grows the buffer seamlessly without data loss.
    /// </summary>
    [Fact]
    public void Append_ExceedingInitialCapacity_GrowsBufferAutomatically()
    {
        // Arrange
        using SpanStringBuilder builder = new(4);
        string largeString = "This is a large string that exceeds the initial capacity of 4 characters.";

        // Act
        builder.Append(largeString.AsSpan());
        string result = builder.ToString();

        // Assert
        Assert.Equal(largeString, result);
    }

    /// <summary>
    /// Verifies that AppendLine appends content followed by Environment.NewLine.
    /// </summary>
    [Fact]
    public void AppendLine_AppendsEnvironmentNewLine()
    {
        // Arrange
        using SpanStringBuilder builder = new(32);

        // Act
        builder.AppendLine("Line 1".AsSpan()).AppendLine('X');
        string result = builder.ToString();

        // Assert
        string expected = $"Line 1{Environment.NewLine}X{Environment.NewLine}";
        Assert.Equal(expected, result);
    }

    /// <summary>
    /// Verifies that character indexer allows reading and mutating characters in place.
    /// </summary>
    [Fact]
    public void Indexer_AllowsInPlaceReadAndMutation()
    {
        // Arrange
        using SpanStringBuilder builder = new(16);
        builder.Append("abc".AsSpan());

        // Act
        builder[1] = 'Z';
        string result = builder.ToString();

        // Assert
        Assert.Equal("aZc", result);
        Assert.Equal('Z', builder[1]);
    }

    /// <summary>
    /// Verifies that calling Dispose multiple times is idempotent and resets the buffer.
    /// </summary>
    [Fact]
    public void Dispose_CalledMultipleTimes_IsSafeAndIdempotent()
    {
        // Arrange
        SpanStringBuilder builder = new(16);
        builder.Append("Test".AsSpan());

        // Act
        builder.Dispose();
        builder.Dispose();
        string result = builder.ToString();

        // Assert
        Assert.Equal(string.Empty, result);
    }
}
