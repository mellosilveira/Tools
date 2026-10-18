using MelloSilveiraTools.Core.Managers.File;

namespace UnitTests.Core.Managers.File;

public sealed class CsvFileWriterTests
{
    [Fact]
    public async Task WriteRowAsync_WithString_IncrementsRowCountAndReturnsAccurateMetadata()
    {
        // Arrange
        string context = "UnitTests";
        string identifier = "CsvFileWriter";
        string fileName = $"test_csv_{Guid.NewGuid():N}.csv";
        string header = "Time,Displacement,Force";
        IFileStorageService fileStorageService = new LocalFileStorageService();
        (Stream stream, FileMetadata createdMetadata) = fileStorageService.Create(context, identifier, fileName);
        string tempFilePath = createdMetadata.FullPath;

        try
        {
            // Act
            await using (stream)
            {
                await using CsvFileWriter writer = new(stream, header);
                await writer.WriteRowAsync("0.0,0.0,0.0");
                await writer.WriteRowAsync("0.1,0.05,10.2");
                await writer.WriteRowAsync("0.2,0.10,20.5");
            }

            FileMetadata metadata = fileStorageService.GetMetadata(context, identifier, fileName);

            // Assert
            Assert.True(System.IO.File.Exists(tempFilePath));
            Assert.Equal(fileName, metadata.FileName);
            Assert.Equal(Path.GetFullPath(tempFilePath), metadata.FullPath);
            Assert.Equal(Path.GetFullPath(tempFilePath), createdMetadata.FullPath);

            FileInfo fileInfo = new(tempFilePath);
            Assert.Equal(fileInfo.Length, metadata.SizeBytes);
            Assert.True(metadata.SizeBytes > 0);

            string[] lines = await System.IO.File.ReadAllLinesAsync(tempFilePath);
            Assert.Equal(4, lines.Length);
            Assert.Equal(header, lines[0]);
            Assert.Equal("0.0,0.0,0.0", lines[1]);
        }
        finally
        {
            if (System.IO.File.Exists(tempFilePath))
            {
                System.IO.File.Delete(tempFilePath);
            }
        }
    }

    [Fact]
    public async Task WriteRowAsync_WithActionStringBuilder_IncrementsRowCount()
    {
        // Arrange
        string context = "UnitTests";
        string identifier = "CsvFileWriter";
        string fileName = $"test_csv_action_{Guid.NewGuid():N}.csv";
        string header = "Step,Value";
        IFileStorageService fileStorageService = new LocalFileStorageService();
        (Stream stream, FileMetadata createdMetadata) = fileStorageService.Create(context, identifier, fileName);
        string tempFilePath = createdMetadata.FullPath;

        try
        {
            // Act
            await using (stream)
            {
                await using CsvFileWriter writer = new(stream, header);
                await writer.WriteRowAsync(sb => sb.Append("1,100.5"));
                await writer.WriteRowAsync(sb => sb.Append("2,200.7"));
            }

            FileMetadata metadata = fileStorageService.GetMetadata(context, identifier, fileName);

            // Assert
            Assert.True(metadata.SizeBytes > 0);

            string[] lines = await System.IO.File.ReadAllLinesAsync(tempFilePath);
            Assert.Equal(3, lines.Length);
            Assert.Equal("Step,Value", lines[0]);
            Assert.Equal("1,100.5", lines[1]);
            Assert.Equal("2,200.7", lines[2]);
        }
        finally
        {
            if (System.IO.File.Exists(tempFilePath))
            {
                System.IO.File.Delete(tempFilePath);
            }
        }
    }

    [Fact]
    public async Task CsvFileWriter_WithFileStorageService_ContextAndIdentifier_WritesAndReadsCorrectly()
    {
        // Arrange
        string baseDir = Path.Combine(Path.GetTempPath(), $"test_storage_{Guid.NewGuid():N}");
        FileStorageSettings settings = new() { BaseDirectory = baseDir };
        LocalFileStorageService storageService = new(settings);

        string context = "ExperimentalData";
        string identifier = "Exp_Test_001";
        string fileName = "processed_points.csv";
        string header = "Time,Strain,Stress";

        try
        {
            // Act - Write via CsvFileWriter with storage service and resolved location
            (Stream writeStream, FileMetadata createdMeta) = storageService.Create(context, identifier, fileName);
            await using (writeStream)
            {
                await using CsvFileWriter writer = new(writeStream, header);
                await writer.WriteRowAsync("0.1,0.02,15.5");
                await writer.WriteRowAsync("0.2,0.04,30.2");
            }

            FileMetadata metadata = storageService.GetMetadata(context, identifier, fileName);

            // Assert metadata
            Assert.Equal(fileName, metadata.FileName);
            Assert.True(metadata.SizeBytes > 0);
            Assert.True(File.Exists(metadata.FullPath));

            // Act - Read back via CsvStreamReader consuming stream from storage service
            await using Stream readStream = storageService.OpenRead(context, identifier, fileName);
            await using CsvStreamReader reader = await CsvStreamReader.CreateAsync(readStream);
            List<double[]> readRows = [];
            await foreach (ReadOnlyMemory<double> row in reader.ReadAllRowsAsync())
            {
                readRows.Add(row.ToArray());
            }

            // Assert read data
            Assert.Equal(2, readRows.Count);
            Assert.Equal(3, readRows[0].Length);
            Assert.Equal(0.1, readRows[0][0]);
            Assert.Equal(0.02, readRows[0][1]);
            Assert.Equal(15.5, readRows[0][2]);
        }
        finally
        {
            if (Directory.Exists(baseDir))
            {
                Directory.Delete(baseDir, true);
            }
        }
    }
}
