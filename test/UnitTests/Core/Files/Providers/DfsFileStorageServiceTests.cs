using MelloSilveiraTools.Core.Files.Models;
using MelloSilveiraTools.Core.Files.Providers.Dfs;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text;

namespace UnitTests.Core.Files.Providers;

/// <summary>
/// Unit tests for <see cref="DfsFileStorageService"/> verifying DFS provider identity
/// and <see cref="FileShare.ReadWrite"/> concurrent read/write stream semantics.
/// </summary>
public sealed class DfsFileStorageServiceTests : IDisposable
{
    private readonly string _baseDirectory = Path.Combine(Path.GetTempPath(), $"DfsStorageTests_{Guid.NewGuid():N}");
    private readonly DfsFileStorageService _service;

    public DfsFileStorageServiceTests()
    {
        Directory.CreateDirectory(_baseDirectory);
        _service = new DfsFileStorageService(NullLoggerFactory.Instance, TimeProvider.System, new DfsStorageSettings { BaseDirectory = _baseDirectory });
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

    /// <summary>
    /// Verifies that Provider returns StorageProvider.Dfs and write/read shares permit concurrent read while write is open.
    /// </summary>
    [Fact]
    public async Task ProviderAndStreams_AllowConcurrentReadWhileWriteStreamIsOpen()
    {
        // Arrange
        Assert.Equal(StorageProvider.Dfs, _service.Provider);
        byte[] payload = Encoding.UTF8.GetBytes("dfs-shared-payload");

        // Act
        (Stream writeStream, string location) = await _service.CreateWriteAsync("dfs-run", "ctx", "shared.txt");
        await using (writeStream)
        {
            await writeStream.WriteAsync(payload);
            await writeStream.FlushAsync();

            // Open read stream while write stream is still open (requires FileShare.ReadWrite)
            await using Stream readStream = await _service.OpenReadAsync(location);
            using StreamReader reader = new(readStream);
            string readContent = await reader.ReadToEndAsync();

            // Assert
            Assert.Equal("dfs-shared-payload", readContent);
        }

        Assert.True(await _service.ExistsAsync(location));
        Assert.True(await _service.DeleteAsync(location));
    }

    /// <summary>
    /// Verifies that <c>CheckHealthAsync</c> validates write, read, and delete on an existing DFS directory without leaving probe files behind.
    /// </summary>
    [Fact]
    public async Task CheckHealthAsync_WhenDirectoryExists_ReturnsTrueAndCleansUpProbeFile()
    {
        bool healthy = await _service.IsHealthAsync();
        Assert.True(healthy);

        string[] leftoverProbes = Directory.GetFiles(_baseDirectory, ".health-probe-*.tmp");
        Assert.Empty(leftoverProbes);
    }

    /// <summary>
    /// Verifies that <c>CheckHealthAsync</c> returns <see langword="false"/> without creating the directory when the DFS root is missing.
    /// </summary>
    [Fact]
    public async Task CheckHealthAsync_WhenDirectoryDoesNotExist_ReturnsFalseWithoutCreatingDirectory()
    {
        string missingMount = Path.Combine(_baseDirectory, "unmounted-dfs");
        using DfsFileStorageService service = new(NullLoggerFactory.Instance, TimeProvider.System, new DfsStorageSettings { BaseDirectory = missingMount });

        bool healthy = await service.IsHealthAsync();

        Assert.False(healthy);
        Assert.False(Directory.Exists(missingMount));
    }

    /// <summary>
    /// Verifies that <c>CheckHealthAsync</c> enforces optional <c>HealthMarkerFileName</c> to detect unmounted empty mount points.
    /// </summary>
    [Fact]
    public async Task CheckHealthAsync_WithHealthMarkerFileName_RequiresMarkerFileInsideBaseDirectory()
    {
        const string markerFileName = ".dfs-mounted";
        string markerPath = Path.Combine(_baseDirectory, markerFileName);
        using DfsFileStorageService serviceWithMarker = new(
            NullLoggerFactory.Instance,
            TimeProvider.System,
            new DfsStorageSettings { BaseDirectory = _baseDirectory, HealthMarkerFileName = markerFileName });

        // Missing marker -> false (unhealthy)
        Assert.False(await serviceWithMarker.IsHealthAsync());

        // Present marker -> true (healthy) and marker remains untouched
        await File.WriteAllTextAsync(markerPath, "mounted");
        Assert.True(await serviceWithMarker.IsHealthAsync());
        Assert.True(File.Exists(markerPath));
        Assert.Empty(Directory.GetFiles(_baseDirectory, ".health-probe-*.tmp"));

        // Path traversal in marker name -> false (fail closed)
        using DfsFileStorageService traversalService = new(
            NullLoggerFactory.Instance,
            TimeProvider.System,
            new DfsStorageSettings { BaseDirectory = _baseDirectory, HealthMarkerFileName = "../outside.txt" });
        Assert.False(await traversalService.IsHealthAsync());
    }
}
