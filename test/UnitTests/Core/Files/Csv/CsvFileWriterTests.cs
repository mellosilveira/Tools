using System.Text;
using MelloSilveiraTools.Core.Files.Csv;

namespace UnitTests.Core.Files.Csv;

/// <summary>
/// Unit tests for <see cref="CsvFileWriter"/> verifying header emission, row formatting,
/// batch flushing at the 1000-row threshold, cancellation, and deterministic async disposal.
/// </summary>
public sealed class CsvFileWriterTests
{
    /// <summary>
    /// Verifies that initializing CsvFileWriter emits the header with LF line ending and WriteRowAsync appends string rows.
    /// </summary>
    [Fact]
    public async Task WriteRowAsync_StringAndAction_WritesHeaderAndRowsWithLf()
    {
        // Arrange
        using MemoryStream stream = new();
        await using (CsvFileWriter writer = new(stream, "col1,col2"))
        {
            // Act
            await writer.WriteRowAsync("a,1");
            await writer.WriteRowAsync(sb => sb.Append("b,2"));
        }

        // Assert
        string content = Encoding.UTF8.GetString(stream.ToArray());
        Assert.Equal("col1,col2\na,1\nb,2\n", content);
    }

    /// <summary>
    /// Verifies that WriteRowAsync overloads accepting ReadOnlySpan&lt;double&gt; format values with invariant culture.
    /// </summary>
    [Fact]
    public async Task WriteRowAsync_NumericSpans_FormatsInvariantCultureWithCustomAndDefaultFormat()
    {
        // Arrange
        using MemoryStream stream = new();
        await using (CsvFileWriter writer = new(stream, "x,y,z"))
        {
            // Act
            await writer.WriteRowAsync(1.5, 2.25, 3.0);
            await writer.WriteRowAsync(CancellationToken.None, 4.125, 5.5, 6.75);
            await writer.WriteRowAsync([1.23456, 7.89012], format: "F2");
        }

        // Assert
        string content = Encoding.UTF8.GetString(stream.ToArray());
        Assert.Equal("x,y,z\n1.5,2.25,3\n4.125,5.5,6.75\n1.23,7.89\n", content);
    }

    /// <summary>
    /// Verifies that writing 1000 or more rows triggers intermediate batch flushes across all WriteRowAsync overloads.
    /// </summary>
    [Fact]
    public async Task WriteRowAsync_ExceedingBatchThreshold_FlushesIntermediateBatches()
    {
        // Arrange
        using MemoryStream stream = new();
        await using (CsvFileWriter writer = new(stream, "idx"))
        {
            // Act
            for (int index = 0; index < 1000; index++)
                await writer.WriteRowAsync(index.ToString());

            for (int index = 0; index < 1000; index++)
                await writer.WriteRowAsync(sb => sb.Append(index));

            for (int index = 0; index < 1000; index++)
                await writer.WriteRowAsync([index], format: "F0");
        }

        // Assert
        string[] lines = Encoding.UTF8.GetString(stream.ToArray()).Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(3001, lines.Length);
        Assert.Equal("idx", lines[0]);
        Assert.Equal("999", lines[^1]);
    }

    /// <summary>
    /// Verifies that passing a cancelled token throws OperationCanceledException immediately and DisposeAsync is idempotent.
    /// </summary>
    [Fact]
    public async Task WriteRowAsync_WhenCancelled_ThrowsAndDisposeIsIdempotent()
    {
        // Arrange
        using MemoryStream stream = new();
        using CancellationTokenSource cts = new();
        cts.Cancel();
        CsvFileWriter writer = new(stream, "h");

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() => writer.WriteRowAsync("row", cts.Token));
        await Assert.ThrowsAsync<OperationCanceledException>(() => writer.WriteRowAsync(_ => { }, cts.Token));
        await Assert.ThrowsAsync<OperationCanceledException>(() => writer.WriteRowAsync([1.0], "G7", cts.Token));

        await writer.DisposeAsync();
        await writer.DisposeAsync();
    }
}
