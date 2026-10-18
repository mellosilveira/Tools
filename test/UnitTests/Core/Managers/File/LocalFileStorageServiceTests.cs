using MelloSilveiraTools.Core;
using MelloSilveiraTools.Core.Managers.File;
using MelloSilveiraTools.Core.ResiliencePipelines;
using Microsoft.Extensions.DependencyInjection;

namespace UnitTests.Core.Managers.File;

public sealed class LocalFileStorageServiceTests : IDisposable
{
    private readonly string _testDirectory;
    private readonly LocalFileStorageService _service;

    public LocalFileStorageServiceTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"LocalFileStorageServiceTests_{Guid.NewGuid():N}");
        FileStorageSettings settings = new() { BaseDirectory = _testDirectory };
        _service = new LocalFileStorageService(settings);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }

    [Fact]
    public void KeyedServiceRegistration_ShouldResolveLocalFileStorageService()
    {
        ServiceCollection services = new();
        services.AddCoreServices(new ResiliencePipelineSettings());
        ServiceProvider provider = services.BuildServiceProvider();

        IFileStorageService? keyedService = provider.GetKeyedService<IFileStorageService>(StorageProvider.Local);
        IFileStorageService? defaultService = provider.GetService<IFileStorageService>();

        Assert.NotNull(keyedService);
        Assert.NotNull(defaultService);
        Assert.IsType<LocalFileStorageService>(keyedService);
        Assert.IsType<LocalFileStorageService>(defaultService);
    }

    [Fact]
    public async Task Create_OpenRead_GetMetadata_Delete_ListFiles_ShouldOperateCorrectly()
    {
        string context = "ExperimentalData";
        string identifier = "Run_100";
        string fileName = "test_points.csv";
        byte[] content = "col1,col2\n1.0,2.0\n"u8.ToArray();

        // Create & Write
        (Stream writeStream, FileMetadata createdMetadata) = _service.Create(context, identifier, fileName);
        Assert.Equal(fileName, createdMetadata.FileName);
        Assert.True(Path.IsPathRooted(createdMetadata.FullPath));

        await using (writeStream)
        {
            await writeStream.WriteAsync(content);
        }

        // Metadata
        FileMetadata metadata = _service.GetMetadata(context, identifier, fileName);
        Assert.Equal(fileName, metadata.FileName);
        Assert.Equal(content.Length, metadata.SizeBytes);
        Assert.True(System.IO.File.Exists(metadata.FullPath));

        // Read
        await using (Stream readStream = _service.OpenRead(context, identifier, fileName))
        {
            byte[] readBuffer = new byte[content.Length];
            int read = await readStream.ReadAsync(readBuffer);
            Assert.Equal(content.Length, read);
            Assert.Equal(content, readBuffer);
        }

        // List files
        IReadOnlyList<FileMetadata> files = _service.ListFiles(context, identifier);
        Assert.Single(files);
        Assert.Equal(fileName, files[0].FileName);

        // Delete
        bool deleted = _service.Delete(context, identifier, fileName);
        Assert.True(deleted);
        Assert.Empty(_service.ListFiles(context, identifier));
    }

    [Fact]
    public async Task CreateTimebased_ShouldCreateTimestampedFileAndReturnMetadata()
    {
        string context = "SensitivityAnalysis";
        string identifier = "Sweep_200";
        string filePrefix = "StressSweep";
        string fileExtension = ".csv";
        byte[] content = "time,stress\n0.0,10.0\n"u8.ToArray();

        (Stream writeStream, FileMetadata createdMetadata) = _service.CreateTimebased(context, identifier, fileExtension, filePrefix);
        Assert.StartsWith(filePrefix, createdMetadata.FileName);
        Assert.EndsWith(fileExtension, createdMetadata.FileName);

        await using (writeStream)
        {
            await writeStream.WriteAsync(content);
        }

        IReadOnlyList<FileMetadata> files = _service.ListFiles(context, identifier);
        Assert.Single(files);
        Assert.StartsWith(filePrefix, files[0].FileName);
        Assert.EndsWith(fileExtension, files[0].FileName);
        Assert.Equal(content.Length, files[0].SizeBytes);
    }

    [Fact]
    public async Task CreateTimebased_WithoutPrefix_ShouldOmitPrefixInFileName()
    {
        string context = "SensitivityAnalysis";
        string identifier = "Sweep_300";
        string fileExtension = ".csv";

        (Stream writeStream, FileMetadata createdMetadata) = _service.CreateTimebased(context, identifier, fileExtension);
        await using (writeStream)
        {
            await writeStream.WriteAsync("time,stress\n"u8.ToArray());
        }

        Assert.EndsWith(fileExtension, createdMetadata.FileName);
        Assert.DoesNotContain("_", createdMetadata.FileName.Replace("-", ""));
    }
}
