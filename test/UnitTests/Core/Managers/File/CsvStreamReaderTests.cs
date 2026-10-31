using MelloSilveiraTools.Core.Managers.File.Csv;
using System.Text;

namespace UnitTests.Core.Managers.File;

/// <summary>
/// Contains unit tests verifying the high-performance streaming functionality of <see cref="CsvStreamReader"/>.
/// </summary>
public class CsvStreamReaderTests
{
    /// <summary>
    /// Verifies that <see cref="CsvStreamReader.ReadNextRowAsync"/> correctly parses two-column numerical rows after skipping the header.
    /// </summary>
    [Fact]
    public async Task ReadNextRowAsync_WithTwoColumns_ParsesCorrectly()
    {
        // Arrange
        string csvContent = "Time,Strain\r\n1.0, 2.5\r\n3.0, 4.5\r\n";
        byte[] bytes = Encoding.UTF8.GetBytes(csvContent);
        using MemoryStream stream = new(bytes);
        await using CsvStreamReader reader = await CsvStreamReader.CreateAsync(stream);

        // Act
        ReadOnlyMemory<double> row1 = await reader.ReadNextRowAsync();
        double[] row1Values = row1.ToArray();

        ReadOnlyMemory<double> row2 = await reader.ReadNextRowAsync();
        double[] row2Values = row2.ToArray();

        ReadOnlyMemory<double> row3 = await reader.ReadNextRowAsync();

        // Assert
        Assert.Equal([1.0, 2.5], row1Values);
        Assert.Equal([3.0, 4.5], row2Values);
        Assert.True(row3.IsEmpty);
    }

    /// <summary>
    /// Verifies that <see cref="CsvStreamReader.ReadNextRowAsync"/> parses rows containing more than two columns.
    /// </summary>
    [Fact]
    public async Task ReadNextRowAsync_WithMultipleColumns_ParsesAllColumns()
    {
        // Arrange
        string csvContent = "C1,C2,C3,C4,C5\r\n1.1, 2.2, 3.3, 4.4, 5.5\r\n10.0, 20.0, 30.0, 40.0, 50.0\r\n";
        byte[] bytes = Encoding.UTF8.GetBytes(csvContent);
        using MemoryStream stream = new(bytes);
        await using CsvStreamReader reader = await CsvStreamReader.CreateAsync(stream);

        // Act
        ReadOnlyMemory<double> row1 = await reader.ReadNextRowAsync();
        double[] row1Values = row1.ToArray();

        ReadOnlyMemory<double> row2 = await reader.ReadNextRowAsync();
        double[] row2Values = row2.ToArray();

        ReadOnlyMemory<double> row3 = await reader.ReadNextRowAsync();

        // Assert
        Assert.Equal([1.1, 2.2, 3.3, 4.4, 5.5], row1Values);
        Assert.Equal([10.0, 20.0, 30.0, 40.0, 50.0], row2Values);
        Assert.True(row3.IsEmpty);
    }

    /// <summary>
    /// Verifies that empty lines and whitespace lines are ignored while streaming data rows.
    /// </summary>
    [Fact]
    public async Task ReadNextRowAsync_WithEmptyLinesAndWhitespace_SkipsEmptyLines()
    {
        // Arrange
        string csvContent = "Col1,Col2\r\n\r\n   \r\n1.0, 2.0\r\n\r\n3.0, 4.0\r\n\r\n";
        byte[] bytes = Encoding.UTF8.GetBytes(csvContent);
        using MemoryStream stream = new(bytes);
        await using CsvStreamReader reader = await CsvStreamReader.CreateAsync(stream);

        // Act
        ReadOnlyMemory<double> row1 = await reader.ReadNextRowAsync();
        double[] row1Values = row1.ToArray();

        ReadOnlyMemory<double> row2 = await reader.ReadNextRowAsync();
        double[] row2Values = row2.ToArray();

        ReadOnlyMemory<double> row3 = await reader.ReadNextRowAsync();

        // Assert
        Assert.Equal([1.0, 2.0], row1Values);
        Assert.Equal([3.0, 4.0], row2Values);
        Assert.True(row3.IsEmpty);
    }

    /// <summary>
    /// Verifies that <see cref="CsvStreamReader"/> works properly with a non-comma delimiter (e.g. semicolon).
    /// </summary>
    [Fact]
    public async Task ReadNextRowAsync_WithCustomDelimiter_ParsesCorrectly()
    {
        // Arrange
        string csvContent = "Col1;Col2;Col3\r\n1.5;2.5;3.5\r\n4.5;5.5;6.5\r\n";
        byte[] bytes = Encoding.UTF8.GetBytes(csvContent);
        using MemoryStream stream = new(bytes);
        await using CsvStreamReader reader = await CsvStreamReader.CreateAsync(stream, delimiter: (byte)';');

        // Act
        ReadOnlyMemory<double> row1 = await reader.ReadNextRowAsync();
        double[] row1Values = row1.ToArray();

        ReadOnlyMemory<double> row2 = await reader.ReadNextRowAsync();
        double[] row2Values = row2.ToArray();

        // Assert
        Assert.Equal([1.5, 2.5, 3.5], row1Values);
        Assert.Equal([4.5, 5.5, 6.5], row2Values);
    }

    /// <summary>
    /// Verifies that <see cref="CsvStreamReader.ReadAllRowsAsync"/> streams all valid numeric rows until the end of stream.
    /// </summary>
    [Fact]
    public async Task ReadAllRowsAsync_StreamsAllRowsCorrectly()
    {
        // Arrange
        string csvContent = "H1,H2\r\n1.0, 2.0\r\n3.0, 4.0\r\n5.0, 6.0\r\n";
        byte[] bytes = Encoding.UTF8.GetBytes(csvContent);
        using MemoryStream stream = new(bytes);
        await using CsvStreamReader reader = await CsvStreamReader.CreateAsync(stream);

        // Act
        List<double[]> rows = [];
        await foreach (ReadOnlyMemory<double> row in reader.ReadAllRowsAsync())
        {
            rows.Add(row.ToArray());
        }

        // Assert
        Assert.Equal(3, rows.Count);
        Assert.Equal([1.0, 2.0], rows[0]);
        Assert.Equal([3.0, 4.0], rows[1]);
        Assert.Equal([5.0, 6.0], rows[2]);
    }

    /// <summary>
    /// Verifies that setting <c>skipInvalidLines: true</c> causes invalid rows to be ignored instead of terminating reading.
    /// </summary>
    [Fact]
    public async Task ReadNextRowAsync_WithSkipInvalidLines_SkipsCorruptedLines()
    {
        // Arrange
        string csvContent = "H1,H2\r\nInvalid,Corrupted\r\n1.0, 2.0\r\nAlsoBad\r\n3.0, 4.0\r\n";
        byte[] bytes = Encoding.UTF8.GetBytes(csvContent);
        using MemoryStream stream = new(bytes);
        await using CsvStreamReader reader = await CsvStreamReader.CreateAsync(stream, skipInvalidLines: true);

        // Act
        ReadOnlyMemory<double> row1 = await reader.ReadNextRowAsync();
        double[] row1Values = row1.ToArray();

        ReadOnlyMemory<double> row2 = await reader.ReadNextRowAsync();
        double[] row2Values = row2.ToArray();

        ReadOnlyMemory<double> row3 = await reader.ReadNextRowAsync();

        // Assert
        Assert.Equal([1.0, 2.0], row1Values);
        Assert.Equal([3.0, 4.0], row2Values);
        Assert.True(row3.IsEmpty);
    }

    /// <summary>
    /// Verifies that setting <c>skipInvalidLines: false</c> terminates streaming upon encountering the first unparseable row.
    /// </summary>
    [Fact]
    public async Task ReadNextRowAsync_WithoutSkipInvalidLines_StopsAtInvalidLine()
    {
        // Arrange
        string csvContent = "H1,H2\r\n1.0, 2.0\r\nInvalid,Row\r\n3.0, 4.0\r\n";
        byte[] bytes = Encoding.UTF8.GetBytes(csvContent);
        using MemoryStream stream = new(bytes);
        await using CsvStreamReader reader = await CsvStreamReader.CreateAsync(stream, skipInvalidLines: false);

        // Act
        ReadOnlyMemory<double> row1 = await reader.ReadNextRowAsync();
        double[] row1Values = row1.ToArray();

        ReadOnlyMemory<double> row2 = await reader.ReadNextRowAsync();

        // Assert
        Assert.Equal([1.0, 2.0], row1Values);
        Assert.True(row2.IsEmpty);
    }

    /// <summary>
    /// Verifies that <see cref="CsvStreamReader.DisposeAsync"/> executes cleanly without throwing exceptions.
    /// </summary>
    [Fact]
    public async Task DisposeAsync_CanBeCalledSafely()
    {
        // Arrange
        string csvContent = "Col1,Col2\r\n1.0, 2.0\r\n";
        byte[] bytes = Encoding.UTF8.GetBytes(csvContent);
        using MemoryStream stream = new(bytes);
        CsvStreamReader reader = await CsvStreamReader.CreateAsync(stream);

        // Act & Assert
        await reader.DisposeAsync();
        await reader.DisposeAsync();
    }
}
