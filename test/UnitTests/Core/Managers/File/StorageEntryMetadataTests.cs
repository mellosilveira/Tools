using MelloSilveiraTools.Core.Managers.File.Models;

namespace UnitTests.Core.Managers.File;

/// <summary>
/// Unit tests for <see cref="StorageEntryMetadata"/> verifying property resolution,
/// file vs directory discrimination, file name normalization, and non-destructive record mutations.
/// </summary>
public sealed class StorageEntryMetadataTests
{
    /// <summary>
    /// Verifies that constructing metadata for a file preserves the file name and correctly identifies it as a file.
    /// </summary>
    [Theory]
    [InlineData(@"C:\storage\data.csv", @"C:\storage", "data.csv", 1024L)]
    [InlineData("/var/storage/output.parquet", "/var/storage", "output.parquet", 2048L)]
    [InlineData("s3://bucket/folder/simulation.csv", "s3://bucket/folder", "simulation.csv", 4096L)]
    public void Constructor_ForFile_PreservesPropertiesAndFlagsAsFile(
        string fullPath,
        string directory,
        string fileName,
        long sizeBytes)
    {
        // Arrange
        DateTimeOffset now = DateTimeOffset.UtcNow;

        // Act
        StorageEntryMetadata metadata = new(
            StorageProvider.Local,
            fullPath,
            directory,
            fileName,
            sizeBytes,
            now);

        // Assert
        Assert.Equal(StorageProvider.Local, metadata.StorageProvider);
        Assert.Equal(fullPath, metadata.Location);
        Assert.Equal(directory, metadata.Directory);
        Assert.Equal(fileName, metadata.FileName);
        Assert.Equal(sizeBytes, metadata.SizeBytes);
        Assert.Equal(now, metadata.CreatedAt);
        Assert.True(metadata.IsFile);
        Assert.False(metadata.IsDirectory);
    }

    /// <summary>
    /// Verifies that constructing metadata with null or empty file name normalizes it to null and flags as directory.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Constructor_WithNullOrEmptyFileName_NormalizesToNullAndFlagsAsDirectory(string? inputFileName)
    {
        // Arrange
        DateTimeOffset now = DateTimeOffset.UtcNow;

        // Act
        StorageEntryMetadata metadata = new(
            StorageProvider.Local,
            @"C:\storage\subfolder",
            @"C:\storage",
            inputFileName,
            5120L,
            now);

        // Assert
        Assert.Null(metadata.FileName);
        Assert.False(metadata.IsFile);
        Assert.True(metadata.IsDirectory);
    }

    /// <summary>
    /// Verifies that non-destructive record mutation via 'with' preserves unchanged fields and normalizes modified file names.
    /// </summary>
    [Fact]
    public void RecordWith_AllowsNonDestructiveMutationAndNormalizesFileName()
    {
        // Arrange
        DateTimeOffset now = DateTimeOffset.UtcNow;
        StorageEntryMetadata metadata = new(
            StorageProvider.Local,
            @"C:\storage\data.csv",
            @"C:\storage",
            "data.csv",
            500L,
            now);

        // Act - size update
        StorageEntryMetadata sizeUpdated = metadata with { SizeBytes = 1024L };

        // Assert
        Assert.Equal(1024L, sizeUpdated.SizeBytes);
        Assert.Equal(metadata.Location, sizeUpdated.Location);
        Assert.Equal(metadata.FileName, sizeUpdated.FileName);
        Assert.True(sizeUpdated.IsFile);

        // Act - convert to directory via empty file name
        StorageEntryMetadata dirConverted = sizeUpdated with { FileName = string.Empty };

        // Assert
        Assert.Null(dirConverted.FileName);
        Assert.True(dirConverted.IsDirectory);
        Assert.False(dirConverted.IsFile);
    }
}
