//using MelloSilveiraTools.Core.Managers.File.Csv;
//using MelloSilveiraTools.Core.Managers.File.Models;
//using MelloSilveiraTools.Core.Managers.File.Providers;
//using MelloSilveiraTools.Core.Managers.File.Providers.Local;

//namespace UnitTests.Core.Managers.File;

///// <summary>
///// Unit tests for <see cref="CsvFileWriter"/> verifying formatted CSV generation,
///// row streaming, file storage integration, and round-trip verification with <see cref="CsvStreamReader"/>.
///// </summary>
//public sealed class CsvFileWriterTests
//{
//    /// <summary>
//    /// Verifies that writing rows using raw strings appends content correctly and updates file metadata.
//    /// </summary>
//    [Fact]
//    public async Task WriteRowAsync_WithString_IncrementsRowCountAndReturnsAccurateMetadata()
//    {
//        // Arrange
//        string baseDir = Path.Combine(Path.GetTempPath(), $"csv_writer_tests_{Guid.NewGuid():N}");
//        LocalFileStorageService fileStorageService = new(new LocalFileStorageSettings { BaseDirectory = baseDir }, TimeProvider.System);
//        string context = "UnitTests";
//        string identifier = "CsvFileWriter";
//        string fileName = $"test_csv_{Guid.NewGuid():N}.csv";
//        string header = "Time,Displacement,Force";

//        try
//        {
//            (Stream stream, string location) = await fileStorageService.CreateWriteAsync(identifier, context, fileName);

//            // Act
//            await using (stream)
//            {
//                await using CsvFileWriter writer = new(stream, header);
//                await writer.WriteRowAsync("0.0,0.0,0.0");
//                await writer.WriteRowAsync("0.1,0.05,10.2");
//                await writer.WriteRowAsync("0.2,0.10,20.5");
//            }

//            StorageEntryMetadata metadata = await fileStorageService.GetMetadataAsync(identifier, context, fileName);

//            // Assert
//            Assert.True(System.IO.File.Exists(metadata.Location));
//            Assert.Equal(fileName, metadata.FileName);
//            Assert.Equal(Path.GetFullPath(location), metadata.Location);

//            FileInfo fileInfo = new(metadata.Location);
//            Assert.Equal(fileInfo.Length, metadata.SizeBytes);
//            Assert.True(metadata.SizeBytes > 0);

//            string[] lines = await System.IO.File.ReadAllLinesAsync(metadata.Location);
//            Assert.Equal(4, lines.Length);
//            Assert.Equal(header, lines[0]);
//            Assert.Equal("0.0,0.0,0.0", lines[1]);
//        }
//        finally
//        {
//            if (Directory.Exists(baseDir))
//            {
//                Directory.Delete(baseDir, true);
//            }
//        }
//    }

//    /// <summary>
//    /// Verifies that writing rows using StringBuilder callback formats and writes correctly.
//    /// </summary>
//    [Fact]
//    public async Task WriteRowAsync_WithActionStringBuilder_IncrementsRowCount()
//    {
//        // Arrange
//        string baseDir = Path.Combine(Path.GetTempPath(), $"csv_action_tests_{Guid.NewGuid():N}");
//        LocalFileStorageService fileStorageService = new(new LocalFileStorageSettings { BaseDirectory = baseDir }, TimeProvider.System);
//        string context = "UnitTests";
//        string identifier = "CsvFileWriter";
//        string fileName = $"test_csv_action_{Guid.NewGuid():N}.csv";
//        string header = "Step,Value";

//        try
//        {
//            (Stream stream, _) = await fileStorageService.CreateWriteAsync(identifier, context, fileName);

//            // Act
//            await using (stream)
//            {
//                await using CsvFileWriter writer = new(stream, header);
//                await writer.WriteRowAsync(sb => sb.Append("1,100.5"));
//                await writer.WriteRowAsync(sb => sb.Append("2,200.7"));
//            }

//            StorageEntryMetadata metadata = await fileStorageService.GetMetadataAsync(identifier, context, fileName);

//            // Assert
//            Assert.True(metadata.SizeBytes > 0);

//            string[] lines = await System.IO.File.ReadAllLinesAsync(metadata.Location);
//            Assert.Equal(3, lines.Length);
//            Assert.Equal("Step,Value", lines[0]);
//            Assert.Equal("1,100.5", lines[1]);
//            Assert.Equal("2,200.7", lines[2]);
//        }
//        finally
//        {
//            if (Directory.Exists(baseDir))
//            {
//                Directory.Delete(baseDir, true);
//            }
//        }
//    }

//    /// <summary>
//    /// Verifies complete round-trip flow: writing CSV with CsvFileWriter and reading it back with CsvStreamReader.
//    /// </summary>
//    [Fact]
//    public async Task CsvFileWriter_WithFileStorageService_ContextAndIdentifier_WritesAndReadsCorrectly()
//    {
//        // Arrange
//        string baseDir = Path.Combine(Path.GetTempPath(), $"test_storage_{Guid.NewGuid():N}");
//        LocalFileStorageSettings settings = new() { BaseDirectory = baseDir };
//        LocalFileStorageService storageService = new(settings, TimeProvider.System);

//        string context = "ExperimentalData";
//        string identifier = "Exp_Test_001";
//        string fileName = "processed_points.csv";
//        string header = "Time,Strain,Stress";

//        try
//        {
//            // Act - Write via CsvFileWriter with storage service and resolved location
//            (Stream writeStream, _) = await storageService.CreateWriteAsync(identifier, context, fileName);
//            await using (writeStream)
//            {
//                await using CsvFileWriter writer = new(writeStream, header);
//                await writer.WriteRowAsync("0.1,0.02,15.5");
//                await writer.WriteRowAsync("0.2,0.04,30.2");
//            }

//            StorageEntryMetadata metadata = await storageService.GetMetadataAsync(identifier, context, fileName);

//            // Assert metadata
//            Assert.Equal(fileName, metadata.FileName);
//            Assert.True(metadata.SizeBytes > 0);
//            Assert.True(System.IO.File.Exists(metadata.Location));

//            // Act - Read back via CsvStreamReader consuming stream from storage service
//            await using Stream readStream = await storageService.OpenReadAsync(identifier, context, fileName);
//            await using CsvStreamReader reader = await CsvStreamReader.CreateAsync(readStream);
//            List<double[]> readRows = [];
//            await foreach (ReadOnlyMemory<double> row in reader.ReadAllRowsAsync())
//            {
//                readRows.Add(row.ToArray());
//            }

//            // Assert read data
//            Assert.Equal(2, readRows.Count);
//            Assert.Equal(3, readRows[0].Length);
//            Assert.Equal(0.1, readRows[0][0]);
//            Assert.Equal(0.02, readRows[0][1]);
//            Assert.Equal(15.5, readRows[0][2]);
//        }
//        finally
//        {
//            if (Directory.Exists(baseDir))
//            {
//                Directory.Delete(baseDir, true);
//            }
//        }
//    }
//}
