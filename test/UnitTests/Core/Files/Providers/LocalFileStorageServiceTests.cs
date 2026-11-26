using MelloSilveiraTools.Core.Files.Models;
using MelloSilveiraTools.Core.Files.Providers.Local;
using System.Text;

namespace UnitTests.Core.Files.Providers;

/// <summary>
/// Unit tests for <see cref="LocalFileStorageService"/> and <see cref="MelloSilveiraTools.Core.Files.Providers.FileStorageServiceBase"/>
/// verifying local path resolution, traversal protection, stream I/O, time-based naming, metadata, listing, and deletion.
/// </summary>
public sealed class LocalFileStorageServiceTests : IDisposable
{
    private readonly string _baseDirectory = Path.Combine(Path.GetTempPath(), $"LocalStorageTests_{Guid.NewGuid():N}");
    private readonly FixedTimeProvider _timeProvider = new(new DateTimeOffset(2026, 5, 10, 14, 30, 45, 123, TimeSpan.Zero));
    private readonly LocalFileStorageService _service;

    public LocalFileStorageServiceTests()
    {
        Directory.CreateDirectory(_baseDirectory);
        _service = new LocalFileStorageService(_timeProvider, new LocalFileStorageSettings { BaseDirectory = _baseDirectory });
    }

    public void Dispose()
    {
        _service.Dispose();
        if (Directory.Exists(_baseDirectory))
        {
            try
            {
                Directory.Delete(_baseDirectory, true);
            }
            catch
            {
                // Best effort cleanup of temporary test directory
            }
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }

    /// <summary>
    /// Verifies Provider property and ResolveLocation partitioning with and without context.
    /// </summary>
    [Fact]
    public void ResolveLocation_WithValidAndEmptyContext_ResolvesInsideBaseDirectory()
    {
        // Act
        string withContext = _service.ResolveLocation("run-1", "exports", "data.csv");
        string withoutContext = _service.ResolveLocation("run-1", string.Empty, "data.csv");

        // Assert
        Assert.Equal(StorageProvider.Local, _service.Provider);
        Assert.Equal(Path.GetFullPath(Path.Combine(_baseDirectory, "run-1", "exports", "data.csv")), withContext);
        Assert.Equal(Path.GetFullPath(Path.Combine(_baseDirectory, "run-1", "data.csv")), withoutContext);
    }

    /// <summary>
    /// Verifies CreateWriteAsync, OpenReadAsync, ExistsAsync, GetMetadataAsync, and DeleteAsync round-trip by context and location.
    /// </summary>
    [Fact]
    public async Task WriteReadExistsMetadataDelete_RoundTrip_WorksCorrectly()
    {
        // Arrange
        byte[] payload = Encoding.UTF8.GetBytes("hello-biomechanics");

        // Act - Write
        (Stream writeStream, string location) = await _service.CreateWriteAsync("exp-1", "raw", "sample.txt");
        await using (writeStream)
            await writeStream.WriteAsync(payload);

        // Assert - Exists & Read & Metadata
        Assert.True(await _service.ExistsAsync("exp-1", "raw", "sample.txt"));
        Assert.True(await _service.ExistsAsync(location));

        await using (Stream readStream = await _service.OpenReadAsync("exp-1", "raw", "sample.txt"))
        using (StreamReader reader = new(readStream))
            Assert.Equal("hello-biomechanics", await reader.ReadToEndAsync());

        StorageEntryMetadata fileMeta = await _service.GetMetadataAsync("exp-1", "raw", "sample.txt");
        Assert.Equal(StorageProvider.Local, fileMeta.StorageProvider);
        Assert.Equal(location, fileMeta.Location);
        Assert.Equal("sample.txt", fileMeta.FileName);
        Assert.Equal(payload.Length, fileMeta.SizeBytes);

        // Act & Assert - Directory Metadata
        string dirPath = Path.GetDirectoryName(location)!;
        StorageEntryMetadata dirMeta = await _service.GetMetadataAsync(dirPath);
        Assert.Null(dirMeta.FileName);
        Assert.Equal(payload.Length, dirMeta.SizeBytes);

        // Act & Assert - Delete
        Assert.True(await _service.DeleteAsync("exp-1", "raw", "sample.txt"));
        Assert.False(await _service.ExistsAsync(location));
        Assert.False(await _service.DeleteAsync(location));
    }

    /// <summary>
    /// Verifies CreateWriteTimebasedAsync overloads generate expected UTC timestamped filenames.
    /// </summary>
    [Fact]
    public async Task CreateWriteTimebasedAsync_WithAndWithoutPrefix_CreatesTimestampedFiles()
    {
        // Act
        (Stream s1, string loc1) = await _service.CreateWriteTimebasedAsync("exp-2", "logs", "sweep", ".csv");
        await s1.DisposeAsync();

        (Stream s2, string loc2) = await _service.CreateWriteTimebasedAsync("exp-2", "logs", ".json");
        await s2.DisposeAsync();

        // Assert
        Assert.Equal("sweep_2026-05-10_14-30-45.123.csv", Path.GetFileName(loc1));
        Assert.Equal("2026-05-10_14-30-45.123.json", Path.GetFileName(loc2));
    }

    /// <summary>
    /// Verifies GetMetadataAsync throws FileNotFoundException for nonexistent locations.
    /// </summary>
    [Fact]
    public async Task GetMetadataAsync_WhenLocationDoesNotExist_ThrowsFileNotFoundException()
    {
        // Arrange
        string missing = _service.ResolveLocation("missing", "ctx", "none.bin");

        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(() => _service.GetMetadataAsync(missing).AsTask());
    }

    /// <summary>
    /// Verifies ListFilesAsync and ListEntriesAsync enumerate files and nested subdirectories accurately, and return empty when missing.
    /// </summary>
    [Fact]
    public async Task ListFilesAndEntries_EnumeratesFilesAndDirectoriesWithSizes()
    {
        // Arrange
        await using (Stream s = (await _service.CreateWriteAsync("exp-3", "data", "a.txt")).Stream)
            await s.WriteAsync("12345"u8.ToArray());

        await using (Stream s = (await _service.CreateWriteAsync("exp-3", "data/sub", "b.txt")).Stream)
            await s.WriteAsync("6789"u8.ToArray());

        // Act
        List<StorageEntryMetadata> filesOnly = [];
        await foreach (StorageEntryMetadata item in _service.ListFilesAsync("exp-3", "data"))
            filesOnly.Add(item);

        List<StorageEntryMetadata> entries = [];
        await foreach (StorageEntryMetadata item in _service.ListEntriesAsync("exp-3", "data"))
            entries.Add(item);

        List<StorageEntryMetadata> missingFiles = [];
        await foreach (StorageEntryMetadata item in _service.ListFilesAsync("nonexistent", "data"))
            missingFiles.Add(item);

        List<StorageEntryMetadata> missingEntries = [];
        await foreach (StorageEntryMetadata item in _service.ListEntriesAsync("nonexistent", "data"))
            missingEntries.Add(item);

        // Assert
        Assert.Single(filesOnly);
        Assert.Equal("a.txt", filesOnly[0].FileName);
        Assert.Equal(5L, filesOnly[0].SizeBytes);

        Assert.Equal(2, entries.Count);
        StorageEntryMetadata fileEntry = Assert.Single(entries, e => e.FileName is not null);
        StorageEntryMetadata dirEntry = Assert.Single(entries, e => e.FileName is null);
        Assert.Equal("a.txt", fileEntry.FileName);
        Assert.Equal(4L, dirEntry.SizeBytes);

        Assert.Empty(missingFiles);
        Assert.Empty(missingEntries);

        // Directory Delete
        string dataDir = Path.Combine(_baseDirectory, "exp-3", "data");
        Assert.True(await _service.DeleteAsync(dataDir));
        Assert.False(Directory.Exists(dataDir));
    }

    /// <summary>
    /// Verifies DisposeAsync completes cleanly.
    /// </summary>
    [Fact]
    public async Task DisposeAsync_CompletesWithoutError()
    {
        await _service.DisposeAsync();
        Assert.Equal(StorageProvider.Local, _service.Provider);
    }

    /// <summary>
    /// Verifies empty-context path resolution and relative write paths in CreateWriteStream.
    /// </summary>
    [Fact]
    public async Task EmptyContext_AndRelativeDirectoryName_BehaveCorrectly()
    {
        // Empty context hits GetDirectory false branch
        (Stream writeStream, string location) = await _service.CreateWriteAsync("exp-empty", string.Empty, "root.txt");
        await using (writeStream)
            await writeStream.WriteAsync("abc"u8.ToArray());

        Assert.True(await _service.ExistsAsync("exp-empty", string.Empty, "root.txt"));
        Assert.Equal(Path.Combine(_baseDirectory, "exp-empty", "root.txt"), location);

        // Symlink/ReparsePoint skip branch in GetDirectorySizeSecure (when OS permits symlink or directory cleanup)
        string missingDriveMeta = Path.Combine(_baseDirectory, "exp-empty");
        StorageEntryMetadata dirMeta = await _service.GetMetadataAsync(missingDriveMeta);
        Assert.Equal(3L, dirMeta.SizeBytes);

        // OpenReadAsync missing file error path
        await Assert.ThrowsAsync<FileNotFoundException>(() => _service.OpenReadAsync("exp-empty", string.Empty, "missing.txt").AsTask());
    }

    /// <summary>
    /// Verifies that <see cref="LocalFileStorageService.ParseLocation"/> inverts <see cref="LocalFileStorageService.ResolveLocation"/>
    /// for canonical 3-segment paths and rejects invalid layouts.
    /// </summary>
    [Fact]
    public void ParseLocation_InvertsResolveLocation_ForLocalPaths()
    {
        string withContext = _service.ResolveLocation("run-1", "exports", "data.csv");
        Assert.Equal(("run-1", "exports", "data.csv"), _service.ParseLocation(withContext));

        string withoutContext = _service.ResolveLocation("run-1", string.Empty, "data.csv");
        Assert.Throws<FormatException>(() => _service.ParseLocation(withoutContext));

        string nestedContext = _service.ResolveLocation("exp-9", "raw/sub", "strain.csv");
        Assert.Throws<FormatException>(() => _service.ParseLocation(nestedContext));

        Assert.Throws<FormatException>(() => _service.ParseLocation(Path.Combine(_baseDirectory, "..", "outside.csv")));
        Assert.Throws<FormatException>(() => _service.ParseLocation(Path.Combine(_baseDirectory, "only-one-segment.csv")));
    }

    /// <summary>
    /// Verifies <c>ExtractDirectory</c>, <c>MoveAsync</c>, <c>ComputeSha256Async</c>, and <c>WriteAndHashAsync</c>
    /// inherited from <c>FileStorageServiceBase</c>.
    /// </summary>
    [Fact]
    public async Task ExtractDirectory_MoveAsync_ComputeSha256Async_And_WriteAndHashAsync_WorkEndToEnd()
    {
        byte[] payload = "plugin-binary-payload"u8.ToArray();
        string expectedHash = Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(payload));

        // WriteAndHashAsync writes file, computes SHA-256, and returns normalized directory
        using MemoryStream source = new(payload);
        (string pendingLocation, string pendingDirectory, string actualHash, long sizeBytes) = await _service.WriteAndHashAsync("plugins", "pending", "Sample.v1.0.0.dll", source);

        Assert.Equal(expectedHash, actualHash);
        Assert.Equal(payload.Length, sizeBytes);
        Assert.True(await _service.ExistsAsync(pendingLocation));
        Assert.Equal(_service.ExtractDirectory(pendingLocation, "pending"), pendingDirectory);
        Assert.Equal("fallback-dir", _service.ExtractDirectory("barefile.dll", "fallback-dir"));
        Assert.Equal(string.Empty, _service.ExtractDirectory("barefile.dll"));

        // ComputeSha256Async matches both overloads
        Assert.Equal(expectedHash, await _service.ComputeSha256Async(pendingLocation));
        Assert.Equal(expectedHash, await _service.ComputeSha256Async("plugins", "pending", "Sample.v1.0.0.dll"));

        // MoveAsync (partition overload) relocates file and deletes source
        string incomingLocation = await _service.MoveAsync(pendingLocation, "plugins", "incoming", "Sample.v1.0.0.dll");
        Assert.False(await _service.ExistsAsync(pendingLocation));
        Assert.True(await _service.ExistsAsync(incomingLocation));
        Assert.Equal(expectedHash, await _service.ComputeSha256Async(incomingLocation));

        // MoveAsync to same location is a safe no-op
        await _service.MoveAsync(incomingLocation, incomingLocation);
        Assert.True(await _service.ExistsAsync(incomingLocation));

        // MoveAsync with overwrite=false throws when destination already exists
        using MemoryStream secondSource = new("other"u8.ToArray());
        (string otherLocation, _, _, _) = await _service.WriteAndHashAsync("plugins", "pending", "Other.v1.0.0.dll", secondSource);
        await Assert.ThrowsAsync<IOException>(() => _service.MoveAsync(otherLocation, incomingLocation, overwrite: false).AsTask());
        Assert.True(await _service.ExistsAsync(otherLocation));

        // MoveAsync with overwrite=true replaces destination cleanly
        await _service.MoveAsync(otherLocation, incomingLocation, overwrite: true);
        Assert.False(await _service.ExistsAsync(otherLocation));
        Assert.Equal(Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData("other"u8.ToArray())), await _service.ComputeSha256Async(incomingLocation));
    }
}
