using MelloSilveiraTools.Core.Managers.File;

namespace UnitTests.Core.Managers.File;

public sealed class FileMetadataTests
{
    [Theory]
    [InlineData(@"C:\storage\ExperimentalData\Exp_01\data.csv", @"C:\storage\ExperimentalData\Exp_01", "data.csv")]
    [InlineData("/var/storage/ExperimentalData/Exp_01/data.csv", "/var/storage/ExperimentalData/Exp_01", "data.csv")]
    [InlineData("https://account.blob.core.windows.net/container/exp/report.csv", "https://account.blob.core.windows.net/container/exp", "report.csv")]
    [InlineData("s3://bucket/folder/simulation.csv", "s3://bucket/folder", "simulation.csv")]
    [InlineData("hdfs://namenode:8020/data/sweep.csv", "hdfs://namenode:8020/data", "sweep.csv")]
    [InlineData(@"\\server\share\exp\output.csv", @"\\server\share\exp", "output.csv")]
    [InlineData("just_a_file.csv", "", "just_a_file.csv")]
    public void Constructor_ResolvesDirectoryAndFileName_ConcurrentlyInSinglePass(string fullPath, string expectedDirectory, string expectedFileName)
    {
        // Act
        FileMetadata metadata = new(fullPath, 1024L);

        // Assert
        Assert.Equal(fullPath, metadata.FullPath);
        Assert.Equal(expectedDirectory, metadata.DirectoryPath);
        Assert.Equal(expectedFileName, metadata.FileName);
        Assert.Equal(1024L, metadata.SizeBytes);
    }

    [Fact]
    public void RecordWith_AllowsNonDestructiveMutationOfSizeBytes()
    {
        // Arrange
        FileMetadata metadata = new(@"C:\storage\data.csv", 500L);

        // Act
        FileMetadata updated = metadata with { SizeBytes = 1024L };

        // Assert
        Assert.Equal(1024L, updated.SizeBytes);
        Assert.Equal(metadata.FullPath, updated.FullPath);
        Assert.Equal(metadata.FileName, updated.FileName);
        Assert.Equal(metadata.DirectoryPath, updated.DirectoryPath);
    }
}
