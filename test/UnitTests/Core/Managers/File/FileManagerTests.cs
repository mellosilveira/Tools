using MelloSilveiraTools.Core.Managers.File;

namespace UnitTests.Core.Managers.File;

public class FileManagerTests : IDisposable
{
    private readonly string _testDirectory;

    public FileManagerTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), "MelloSilveiraTools_FileManagerTests_" + Guid.NewGuid().ToString("N"));
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, true);
        }
    }

    [Fact]
    public void BuildTimebasedFullName_ShouldCombineDirectoryAndTimestampedName()
    {
        FileManager manager = new();
        string prefix = "Report";
        string extension = ".csv";

        string fullName = manager.BuildTimebasedFullName(_testDirectory, prefix, extension);

        Assert.StartsWith(_testDirectory, fullName);
        Assert.Contains(prefix, fullName);
        Assert.EndsWith(extension, fullName);
    }

    [Fact]
    public void BuildTimebasedFile_ShouldReturnFileDataWithValidFields()
    {
        FileManager manager = new();
        string prefix = "Export";
        string extension = ".json";

        FileData fileData = manager.BuildTimebasedFile(_testDirectory, prefix, extension);

        Assert.Equal(_testDirectory, fileData.Uri);
        Assert.StartsWith(prefix, fileData.Name);
        Assert.EndsWith(extension, fileData.Name);
    }

    [Fact]
    public void BuildTimebasedFileInfo_ShouldEnsureDirectoryExistsOnDisk()
    {
        FileManager manager = new();
        string subDir = Path.Combine(_testDirectory, "Nested", "Folder");

        Assert.False(Directory.Exists(subDir));

        FileInfo fileInfo = manager.BuildTimebasedFileInfo(subDir, "Log", ".txt");

        Assert.True(Directory.Exists(subDir));
        Assert.Equal(subDir, fileInfo.DirectoryName);
    }
}
