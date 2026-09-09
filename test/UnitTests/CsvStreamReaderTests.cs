using MelloSilveiraTools.Core.Managers.File;
using System.Text;

namespace UnitTests;

public class CsvStreamReaderTests
{
    [Fact]
    public async Task ReadNextRowAsync_WithTwoColumns_ParsesCorrectly()
    {
        // Arrange
        string csvContent = "1.0, 2.5\r\n3.0, 4.5\r\n";
        using MemoryStream stream = new(Encoding.UTF8.GetBytes(csvContent));
        await using CsvStreamReader reader = new(stream);

        // Act
        double[]? row1 = await reader.ReadNextRowAsync();
        double[]? row2 = await reader.ReadNextRowAsync();
        double[]? row3 = await reader.ReadNextRowAsync();

        // Assert
        Assert.NotNull(row1);
        Assert.Equal(2, row1.Length);
        Assert.Equal(1.0, row1[0]);
        Assert.Equal(2.5, row1[1]);

        Assert.NotNull(row2);
        Assert.Equal(2, row2.Length);
        Assert.Equal(3.0, row2[0]);
        Assert.Equal(4.5, row2[1]);

        Assert.Null(row3);
    }

    [Fact]
    public async Task ReadNextRowAsync_WithMultipleColumns_ParsesAllColumns()
    {
        // Arrange
        string csvContent = "1.1, 2.2, 3.3, 4.4, 5.5\r\n10.0, 20.0, 30.0, 40.0, 50.0\r\n";
        using MemoryStream stream = new(Encoding.UTF8.GetBytes(csvContent));
        await using CsvStreamReader reader = new(stream);

        // Act
        double[]? row1 = await reader.ReadNextRowAsync();
        double[]? row2 = await reader.ReadNextRowAsync();
        double[]? row3 = await reader.ReadNextRowAsync();

        // Assert
        Assert.NotNull(row1);
        Assert.Equal(5, row1.Length);
        Assert.Equal([1.1, 2.2, 3.3, 4.4, 5.5], row1);

        Assert.NotNull(row2);
        Assert.Equal(5, row2.Length);
        Assert.Equal([10.0, 20.0, 30.0, 40.0, 50.0], row2);

        Assert.Null(row3);
    }

    [Fact]
    public async Task ReadNextRowAsync_WithEmptyLinesAndWhitespace_SkipsEmptyLines()
    {
        // Arrange
        string csvContent = "1.0, 2.0\r\n\r\n   \r\n3.0, 4.0\r\n\r\n";
        using MemoryStream stream = new(Encoding.UTF8.GetBytes(csvContent));
        await using CsvStreamReader reader = new(stream);

        // Act
        double[]? row1 = await reader.ReadNextRowAsync();
        double[]? row2 = await reader.ReadNextRowAsync();
        double[]? row3 = await reader.ReadNextRowAsync();

        // Assert
        Assert.NotNull(row1);
        Assert.Equal([1.0, 2.0], row1);

        Assert.NotNull(row2);
        Assert.Equal([3.0, 4.0], row2);

        Assert.Null(row3);
    }

    [Fact]
    public async Task ReadNextRowAsync_WithCustomDelimiter_ParsesCorrectly()
    {
        // Arrange
        string csvContent = "1.5;2.5;3.5\r\n4.5;5.5;6.5\r\n";
        using MemoryStream stream = new(Encoding.UTF8.GetBytes(csvContent));
        await using CsvStreamReader reader = new(stream, delimiter: (byte)';');

        // Act
        double[]? row1 = await reader.ReadNextRowAsync();
        double[]? row2 = await reader.ReadNextRowAsync();

        // Assert
        Assert.NotNull(row1);
        Assert.Equal([1.5, 2.5, 3.5], row1);

        Assert.NotNull(row2);
        Assert.Equal([4.5, 5.5, 6.5], row2);
    }

    [Fact]
    public async Task ReadAllRowsAsync_StreamsAllRowsCorrectly()
    {
        // Arrange
        string csvContent = "1.0, 2.0\r\n3.0, 4.0\r\n5.0, 6.0\r\n";
        using MemoryStream stream = new(Encoding.UTF8.GetBytes(csvContent));
        await using CsvStreamReader reader = new(stream);

        // Act
        List<double[]> rows = [];
        await foreach (double[] row in reader.ReadAllRowsAsync())
        {
            rows.Add(row);
        }

        // Assert
        Assert.Equal(3, rows.Count);
        Assert.Equal([1.0, 2.0], rows[0]);
        Assert.Equal([3.0, 4.0], rows[1]);
        Assert.Equal([5.0, 6.0], rows[2]);
    }

    [Fact]
    public async Task ReadNextRowAsync_WithSkipInvalidLines_SkipsHeaderRow()
    {
        // Arrange
        string csvContent = "Time, Value1, Value2\r\n1.0, 2.0, 3.0\r\n";
        using MemoryStream stream = new(Encoding.UTF8.GetBytes(csvContent));
        await using CsvStreamReader reader = new(stream, skipInvalidLines: true);

        // Act
        double[]? row1 = await reader.ReadNextRowAsync();
        double[]? row2 = await reader.ReadNextRowAsync();

        // Assert
        Assert.NotNull(row1);
        Assert.Equal([1.0, 2.0, 3.0], row1);
        Assert.Null(row2);
    }
}

