using MelloSilveiraTools.Core;
using MelloSilveiraTools.Core.Managers.File;
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
        services.AddCoreServices();
        ServiceProvider provider = services.BuildServiceProvider();

        IFileStorageService? keyedService = provider.GetKeyedService<IFileStorageService>(StorageProvider.Local);
        IFileStorageService? defaultService = provider.GetService<IFileStorageService>();

        Assert.NotNull(keyedService);
        Assert.NotNull(defaultService);
        Assert.IsType<LocalFileStorageService>(keyedService);
        Assert.IsType<LocalFileStorageService>(defaultService);
    }

    [Fact]
    public void GetDirectory_And_ResolvePath_ShouldFormatCanonicalFolders()
    {
        string dir = _service.GetDirectory("ExperimentalData", "Exp_001");
        string expectedDir = Path.Combine(_testDirectory, "ExperimentalData", "Exp_001");
        Assert.Equal(expectedDir, dir);

        string resolvedPath = _service.ResolvePath("ExperimentalData", "Exp_001", "data.csv");
        string expectedPath = Path.Combine(expectedDir, "data.csv");
        Assert.Equal(expectedPath, resolvedPath);
    }

    [Fact]
    public void ResolveTimebasedPath_ShouldIncludePrefixAndExtension()
    {
        string resolved = _service.ResolveTimebasedPath("SensitivityAnalysis", "Sweep_01", "Variable_A", ".csv");

        Assert.Contains(Path.Combine("SensitivityAnalysis", "Sweep_01"), resolved);
        Assert.Contains("Variable_A", resolved);
        Assert.EndsWith(".csv", resolved);
    }

    [Fact]
    public async Task OpenWrite_OpenRead_Exists_Delete_ListFiles_ShouldOperateCorrectly()
    {
        string context = "ExperimentalData";
        string identifier = "Run_100";
        string fileName = "test_points.csv";
        byte[] content = "col1,col2\n1.0,2.0\n"u8.ToArray();

        // Write
        await using (Stream writeStream = _service.OpenWrite(context, identifier, fileName))
        {
            await writeStream.WriteAsync(content);
        }

        // Exists
        Assert.True(_service.Exists(context, identifier, fileName));

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
        Assert.False(_service.Exists(context, identifier, fileName));
    }
}
